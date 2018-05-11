using Oracle.ManagedDataAccess.Client;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Sinks.Oracle.Columns;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Serilog.Sinks.Oracle
{
    public class Database
    {
        private readonly string _connectionString;
        private readonly string _tableSpace;
        private readonly string _tableName;
        private readonly ColumnOptions _columnOptions;
        private readonly IFormatProvider _formatProvider;
        private readonly Properties _properties;

        public Database(string connectionString, string tableSpace, string tableName, ColumnOptions columnOptions, 
            HashSet<string> additionalDataColumnNames, IFormatProvider formatProvider)
        {
            _connectionString = connectionString;
            _tableSpace = tableSpace;
            _tableName = tableName;
            _columnOptions = columnOptions;
            _formatProvider = formatProvider;
           _properties = new Properties(columnOptions, additionalDataColumnNames, _formatProvider);
        }

        public (string, Dictionary<string, object>) CreateInsertData(DataTable dataTable)
        {
            var destinationTableName = string.Format("{0}.{1}", _tableSpace, _tableName);
            var parameterDictionary = new Dictionary<string, object>();
            var insertedColumns = dataTable.Columns.Cast<DataColumn>()
                .Where(x => !x.AutoIncrement).ToList();

            var commandString = new StringBuilder();
            var cols = string.Join(", ", insertedColumns.Select(x => $"\"{x.ColumnName}\""));
            commandString.AppendLine($@"INSERT ALL [INTOS_HERE]");

            var selectBuilder = new StringBuilder();

            for (var i = 0; i < dataTable.Rows.Count; i++)
            {
                var eventsTableRow = dataTable.Rows[i];

                var rows = string.Join(", ", insertedColumns.Select(x =>
                    x.ColumnName == "Id" ? eventsTableRow["Id"] : $":{x.ColumnName}_{i}"));

                selectBuilder.AppendLine($"  INTO {destinationTableName} ({cols}) VALUES ({rows})");

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

        public async Task StoreLogAsync(IEnumerable<LogEvent> events)
        {
            try
            {
                using (var cn = new OracleConnection(_connectionString))
                {
                    await cn.OpenAsync().ConfigureAwait(false);

                    var dataTable = FillDataTable(events);
                    var (stringCommand, parameterDictionary) = CreateInsertData(dataTable);

                    using (var command = new OracleCommand(stringCommand, cn))
                    {
                        PrepareCommand(command, parameterDictionary);
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception error)
            {
                SelfLog.WriteLine(error.ToString());
            }
        }

        public void StoreLog(IEnumerable<LogEvent> events)
        {
            try
            {
                using (var cn = new OracleConnection(_connectionString))
                {
                    cn.Open();

                    var dataTable = FillDataTable(events);
                    var (stringCommand, parameterDictionary) = CreateInsertData(dataTable);

                    using (var command = new OracleCommand(stringCommand, cn))
                    {
                        PrepareCommand(command, parameterDictionary);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception error)
            {
                SelfLog.WriteLine(error.ToString());
            }
        }
        private DataTable CreateDataTable()
        {
            var eventsTable = new DataTable(_tableName);

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
                        eventsTable.Columns.Add(new DataColumn
                        {
                            DataType = typeof(string),
                            ColumnName =
                                !string.IsNullOrWhiteSpace(_columnOptions.Id.ColumnName)
                                    ? _columnOptions.Id.ColumnName
                                    : "Id",
                        });
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (_columnOptions.AdditionalDataColumns != null)
                eventsTable.Columns.AddRange(_columnOptions.AdditionalDataColumns.ToArray());

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
                            row[_columnOptions.Id.ColumnName ?? "Id"] = "get_seq";
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
                {
                    _properties.ConvertPropertiesToColumn(row, logEvent.Properties);
                }

                eventsTable.Rows.Add(row);
            }

            return eventsTable;
        }
    }
}
