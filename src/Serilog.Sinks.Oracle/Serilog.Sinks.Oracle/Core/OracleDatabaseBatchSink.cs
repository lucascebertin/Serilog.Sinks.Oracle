using Oracle.ManagedDataAccess.Client;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Sinks.Oracle.Batch;
using Serilog.Sinks.Oracle.Columns;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Serilog.Sinks.Oracle.Core
{
    public class OracleDatabaseBatchSink : ILogEventBatchSink
    {
        private readonly string _connectionString;
        private readonly string _tableSpaceAndTableName;
        private readonly string _tableSpaceAndFunctionName;
        private readonly ColumnOptions _columnOptions;
        private readonly IFormatProvider _formatProvider;
        private readonly Properties _properties;
        private readonly IList<ColumnInfo> _columnsInfo;
        private readonly string _insertStatementForArrayBinding;
        private readonly bool _bindArrays;

        public OracleDatabaseBatchSink(
            string connectionString, 
            string tableSpaceAndTableName, 
            string tableSpaceAndFunctionName, 
            ColumnOptions columnOptions,
            HashSet<string> additionalDataColumnNames, 
            IFormatProvider formatProvider,
            bool bindArrays = false)
        {
            _connectionString = connectionString;
            _tableSpaceAndTableName = tableSpaceAndTableName;
            _tableSpaceAndFunctionName = tableSpaceAndFunctionName;
            _columnOptions = columnOptions ?? new ColumnOptions();
            _formatProvider = formatProvider;
            _properties = new Properties(_columnOptions, additionalDataColumnNames, _formatProvider);

            _bindArrays = bindArrays;
            _columnsInfo = GetColumnsInfo();
            _insertStatementForArrayBinding = CreateInsertStatementForArrayBinding();
        }

        private string CreateInsertStatementForArrayBinding()
        {
            // Create Insert statement
            var commandString = new StringBuilder();
            var cols = string.Join(", ", _columnsInfo.Select(x => $"\"{x.ColumnName}\""));
            var values = string.Join(", ", _columnsInfo.Select(x => $":v_{x.ColumnName}"));
            commandString.AppendLine($@"INSERT INTO {_tableSpaceAndTableName} ");
            commandString.AppendLine($@"       ({cols}) ");
            commandString.AppendLine($@"VALUES ({values}) ");

            return commandString.ToString();
        }

        public (string, Dictionary<string, object>) CreateInsertArrayBindData(IEnumerable<LogEvent> events)
        {
            // Fill parameter values
            var parameterDictionary = new Dictionary<string, object>();

            foreach ( var ci in _columnsInfo)
            {
                object arrColData = null;
                if (ci.StandardColumn == StandardColumn.Level)
                    if (ci.Type == typeof(string))
                        arrColData = events.Select(s => s.Level.ToString()).ToArray();
                    else
                        arrColData = events.Select(s => (byte)s.Level).ToArray();
                else if (ci.StandardColumn == StandardColumn.TimeStamp)
                    arrColData = events.Select(s => _columnOptions.TimeStamp.ConvertToUtc 
                        ? s.Timestamp.DateTime.ToUniversalTime() 
                        : s.Timestamp.DateTime).ToArray();
                else if (ci.StandardColumn == StandardColumn.LogEvent)
                    arrColData = events.Select(s => _properties.LogEventToJson(s)).ToArray();
                else if (ci.StandardColumn == StandardColumn.Exception)
                    arrColData = events.Select(s => s.Exception?.ToString()).ToArray();
                else if (ci.StandardColumn == StandardColumn.Message)
                    arrColData = events.Select(s => s.RenderMessage(_formatProvider)).ToArray();
                else if (ci.StandardColumn == StandardColumn.MessageTemplate)
                    arrColData = events.Select(s => s.MessageTemplate.ToString()).ToArray();
                else if (ci.StandardColumn == StandardColumn.Properties)
                    arrColData = events.Select(s => _properties.ConvertPropertiesToXmlStructure(s.Properties)).ToArray();

                parameterDictionary.Add($"v_{ci.ColumnName}", arrColData);
            }

            return (_insertStatementForArrayBinding, parameterDictionary);
        }


        public (string, Dictionary<string, object>) CreateInsertData(DataTable dataTable)
        {
            var parameterDictionary = new Dictionary<string, object>();
            var insertedColumns = dataTable.Columns.Cast<DataColumn>()
                .Where(x => !x.AutoIncrement).ToList();

            var commandString = new StringBuilder();
            var cols = string.Join(", ", insertedColumns.Select(x => $"\"{x.ColumnName}\""));
            commandString.Append($@"INSERT ALL {Environment.NewLine}[INTOS_HERE]");

            var selectBuilder = new StringBuilder();

            for (var i = 0; i < dataTable.Rows.Count; i++)
            {
                var eventsTableRow = dataTable.Rows[i];

                var rows = string.Join(", ", insertedColumns.Select(x =>
                    x.ColumnName == "Id" ? eventsTableRow["Id"] : $":{x.ColumnName}_{i}"));

                selectBuilder.AppendLine($"  INTO {_tableSpaceAndTableName} ({cols}) VALUES ({rows})");

                foreach (var eventsTableColumn in insertedColumns)
                    if (eventsTableColumn.ColumnName != "Id")
                        parameterDictionary[$":{eventsTableColumn.ColumnName}_{i}"] = Convert.ChangeType(
                            eventsTableRow[eventsTableColumn.ColumnName], eventsTableColumn.DataType);
            }

            commandString.AppendLine("SELECT * FROM dual");
            var insertStatement = commandString.ToString().Replace("[INTOS_HERE]", selectBuilder.ToString());

            return (insertStatement, parameterDictionary);
        }


        public void PrepareCommand(OracleCommand command, IDictionary<string, object> parameterDictionary)
        {
            foreach (var key in parameterDictionary.Keys)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = key;

                if (parameterDictionary[key] == null)
                {
                    parameter.Value = DBNull.Value;
                    command.Parameters.Add(parameter);
                }
                else
                {
                    parameter.Value = parameterDictionary[key];
                    command.Parameters.Add(parameter);
                }
            }
        }

        public async Task EmitBatchAsync(IEnumerable<LogEvent> events)
        {
            try
            {
                using (var cn = new OracleConnection(_connectionString))
                {
                    await cn.OpenAsync().ConfigureAwait(false);

                    if (_bindArrays)
                    {
                        var (stringCommand, parameterDictionary) = CreateInsertArrayBindData(events);

                        using (var command = new OracleCommand(stringCommand, cn))
                        {
                            command.ArrayBindCount = events.Count();
                            command.BindByName = true;

                            PrepareCommand(command, parameterDictionary);
                            await command.ExecuteNonQueryAsync();
                        }
                    }
                    else
                    {
                        var dataTable = FillDataTable(events);
                        var (stringCommand, parameterDictionary) = CreateInsertData(dataTable);

                        using (var command = new OracleCommand(stringCommand, cn))
                        {
                            PrepareCommand(command, parameterDictionary);
                            await command.ExecuteNonQueryAsync();
                        }
                    }
                }
            }
            catch (Exception error)
            {
                SelfLog.WriteLine(error.ToString());
            }
        }


        public void EmitBatch(IEnumerable<LogEvent> events)
        {
            try
            {
                using (var cn = new OracleConnection(_connectionString))
                {
                    cn.Open();

                    if (_bindArrays)
                    {
                        var (stringCommand, parameterDictionary) = CreateInsertArrayBindData(events);

                        using (var command = new OracleCommand(stringCommand, cn))
                        {
                            command.ArrayBindCount = events.Count();
                            command.BindByName = true;

                            PrepareCommand(command, parameterDictionary);
                            command.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        var dataTable = FillDataTable(events);
                        var (stringCommand, parameterDictionary) = CreateInsertData(dataTable);

                        using (var command = new OracleCommand(stringCommand, cn))
                        {
                            PrepareCommand(command, parameterDictionary);
                            command.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception error)
            {
                SelfLog.WriteLine(error.ToString());
            }
        }


        public class ColumnInfo
        {
            public StandardColumn? StandardColumn { get; set; }
            public bool AllowDBNull { get; set; }
            public Type Type { get; set; }
            public OracleDbType DataType { get; set; }
            public long MaxLength { get; set; }
            public string ColumnName { get; set; }
        }

        public IList<ColumnInfo> GetColumnsInfo()
        {
            IList<ColumnInfo> columnsInfo = new List<ColumnInfo>();

            foreach (var standardColumn in _columnOptions.Store)
            {
                switch (standardColumn)
                {
                    case StandardColumn.Level:
                        columnsInfo.Add(new ColumnInfo
                        {
                            StandardColumn = standardColumn,
                            DataType = _columnOptions.Level.StoreAsEnum ? OracleDbType.Byte : OracleDbType.NVarchar2,
                            Type = _columnOptions.Level.StoreAsEnum ? typeof(short) : typeof(string),
                            MaxLength = _columnOptions.Level.StoreAsEnum ? -1 : 128,
                            ColumnName = _columnOptions.Level.ColumnName ?? StandardColumn.Level.ToString(),
                        });
                        break;
                    case StandardColumn.TimeStamp:
                        columnsInfo.Add(new ColumnInfo
                        {
                            StandardColumn = standardColumn,
                            DataType = OracleDbType.TimeStamp,
                            Type = typeof(DateTimeOffset),
                            ColumnName = _columnOptions.TimeStamp.ColumnName ?? StandardColumn.TimeStamp.ToString(),
                            AllowDBNull = false
                        });
                        break;
                    case StandardColumn.LogEvent:
                        columnsInfo.Add(new ColumnInfo
                        {
                            StandardColumn = standardColumn,
                            Type = typeof(string),
                            DataType = OracleDbType.NVarchar2,
                            ColumnName = _columnOptions.LogEvent.ColumnName ?? StandardColumn.LogEvent.ToString(),
                        });
                        break;
                    case StandardColumn.Message:
                        columnsInfo.Add(new ColumnInfo
                        {
                            StandardColumn = standardColumn,
                            Type = typeof(string),
                            DataType = OracleDbType.Clob,
                            MaxLength = -1,
                            ColumnName = _columnOptions.Message.ColumnName ?? StandardColumn.Message.ToString(),
                        });
                        break;
                    case StandardColumn.MessageTemplate:
                        columnsInfo.Add(new ColumnInfo
                        {
                            StandardColumn = standardColumn,
                            Type = typeof(string),
                            DataType = OracleDbType.Clob,
                            MaxLength = -1,
                            ColumnName = _columnOptions.MessageTemplate.ColumnName ?? StandardColumn.MessageTemplate.ToString(),
                        });
                        break;
                    case StandardColumn.Exception:
                        columnsInfo.Add(new ColumnInfo
                        {
                            StandardColumn = standardColumn,
                            Type = typeof(string),
                            DataType = OracleDbType.Clob,
                            MaxLength = -1,
                            ColumnName = _columnOptions.Exception.ColumnName ?? StandardColumn.Exception.ToString(),
                        });
                        break;
                    case StandardColumn.Properties:
                        columnsInfo.Add(new ColumnInfo
                        {
                            StandardColumn = standardColumn,
                            Type = typeof(string),
                            DataType = OracleDbType.Clob,
                            MaxLength = -1,
                            ColumnName = _columnOptions.Properties.ColumnName ?? StandardColumn.Properties.ToString(),
                        });
                        break;
                    case StandardColumn.Id:
                        if (!String.IsNullOrEmpty(_tableSpaceAndFunctionName))
                        {
                            columnsInfo.Add(new ColumnInfo
                            {
                                StandardColumn = standardColumn,
                                Type = typeof(long),
                                DataType = OracleDbType.Decimal,
                                ColumnName =
                                    !string.IsNullOrWhiteSpace(_columnOptions.Id.ColumnName)
                                        ? _columnOptions.Id.ColumnName
                                        : "Id"
                            });
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (_columnOptions.AdditionalDataColumns != null)
            {
                var dataColumns = _columnOptions.AdditionalDataColumns.Select(x =>
                    new ColumnInfo {
                        StandardColumn = null,
                        ColumnName = x.ColumnName,
                        Type = typeof(string), DataType = OracleDbType.NVarchar2});

                columnsInfo = columnsInfo.Concat(dataColumns).ToList();
            }

            return columnsInfo;
        }


        public DataTable CreateDataTable()
        {
            var eventsTable = new DataTable(_tableSpaceAndTableName);

            foreach (var standardColumn in _columnOptions.Store)
            {
                switch (standardColumn)
                {
                    case StandardColumn.Level:
                        eventsTable.Columns.Add(new DataColumn
                        {
                            DataType = _columnOptions.Level.StoreAsEnum ? typeof(byte) : typeof(string),
                            MaxLength = _columnOptions.Level.StoreAsEnum ? -1 : 128,
                            ColumnName = _columnOptions.Level.ColumnName ?? StandardColumn.Level.ToString()
                        });
                        break;
                    case StandardColumn.TimeStamp:
                        eventsTable.Columns.Add(new DataColumn
                        {
                            DataType = typeof(DateTime),
                            ColumnName = _columnOptions.TimeStamp.ColumnName ?? StandardColumn.TimeStamp.ToString(),
                            AllowDBNull = false
                        });
                        break;
                    case StandardColumn.LogEvent:
                        eventsTable.Columns.Add(new DataColumn
                        {
                            DataType = typeof(string),
                            ColumnName = _columnOptions.LogEvent.ColumnName ?? StandardColumn.LogEvent.ToString()
                        });
                        break;
                    case StandardColumn.Message:
                        eventsTable.Columns.Add(new DataColumn
                        {
                            DataType = typeof(string),
                            MaxLength = -1,
                            ColumnName = _columnOptions.Message.ColumnName ?? StandardColumn.Message.ToString()
                        });
                        break;
                    case StandardColumn.MessageTemplate:
                        eventsTable.Columns.Add(new DataColumn
                        {
                            DataType = typeof(string),
                            MaxLength = -1,
                            ColumnName = _columnOptions.MessageTemplate.ColumnName ?? StandardColumn.MessageTemplate.ToString()
                        });
                        break;
                    case StandardColumn.Exception:
                        eventsTable.Columns.Add(new DataColumn
                        {
                            DataType = typeof(string),
                            MaxLength = -1,
                            ColumnName = _columnOptions.Exception.ColumnName ?? StandardColumn.Exception.ToString()
                        });
                        break;
                    case StandardColumn.Properties:
                        eventsTable.Columns.Add(new DataColumn
                        {
                            DataType = typeof(string),
                            MaxLength = -1,
                            ColumnName = _columnOptions.Properties.ColumnName ?? StandardColumn.Properties.ToString()
                        });
                        break;
                    case StandardColumn.Id:
                        if (!String.IsNullOrEmpty(_tableSpaceAndFunctionName))
                        {
                            eventsTable.Columns.Add(new DataColumn
                            {
                                DataType = typeof(string),
                                ColumnName =
                                    !string.IsNullOrWhiteSpace(_columnOptions.Id.ColumnName)
                                        ? _columnOptions.Id.ColumnName
                                        : "Id",
                            });
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (_columnOptions.AdditionalDataColumns != null)
            {
                var dataColumns = _columnOptions.AdditionalDataColumns.Select(x =>
                    new DataColumn(x.ColumnName, x.DataType));

                eventsTable.Columns.AddRange(dataColumns.ToArray());
            }

            return eventsTable;
        }

        private DataTable FillDataTable(IEnumerable<LogEvent> events)
        {
            var eventsTable = CreateDataTable();

            foreach (var logEvent in events)
            {
                var row = eventsTable.NewRow();

                foreach (var column in _columnOptions.Store)
                {
                    switch (column)
                    {
                        case StandardColumn.Id:
                            if (!String.IsNullOrEmpty(_tableSpaceAndFunctionName))
                            {
                                row[_columnOptions.Id.ColumnName ?? "Id"] = _tableSpaceAndFunctionName;
                            }
                            break;
                        case StandardColumn.Message:
                            row[_columnOptions.Message.ColumnName ?? "Message"] = logEvent.RenderMessage(_formatProvider);
                            break;
                        case StandardColumn.MessageTemplate:
                            row[_columnOptions.MessageTemplate.ColumnName ?? "MessageTemplate"] = logEvent.MessageTemplate.ToString();
                            break;
                        case StandardColumn.Level:
                            row[_columnOptions.Level.ColumnName ?? "Level"] = logEvent.Level;
                            break;
                        case StandardColumn.TimeStamp:
                            row[_columnOptions.TimeStamp.ColumnName ?? "TimeStamp"] = _columnOptions.TimeStamp.ConvertToUtc ? logEvent.Timestamp.DateTime.ToUniversalTime() : logEvent.Timestamp.DateTime;
                            break;
                        case StandardColumn.Exception:
                            row[_columnOptions.Exception.ColumnName ?? "Exception"] = logEvent.Exception?.ToString();
                            break;
                        case StandardColumn.Properties:
                            row[_columnOptions.Properties.ColumnName ?? "Properties"] = _properties.ConvertPropertiesToXmlStructure(logEvent.Properties);
                            break;
                        case StandardColumn.LogEvent:
                            row[_columnOptions.LogEvent.ColumnName ?? "LogEvent"] = _properties.LogEventToJson(logEvent);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                if (_columnOptions.AdditionalDataColumns != null)
                    _properties.ConvertPropertiesToColumn(row, logEvent.Properties);

                eventsTable.Rows.Add(row);
            }

            return eventsTable;
        }
    }
}
