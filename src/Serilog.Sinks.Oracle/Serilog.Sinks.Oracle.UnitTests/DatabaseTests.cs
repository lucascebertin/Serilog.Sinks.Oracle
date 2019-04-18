using FluentAssertions;
using Oracle.ManagedDataAccess.Client;
using Serilog.Sinks.Oracle.Columns;
using Serilog.Sinks.Oracle.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Xunit;

namespace Serilog.Sinks.Oracle.UnitTests
{
    public class DatabaseTests
    {
        //private readonly ITestOutputHelper _output;
        private const string ConnectionString = "";
        private const string TableName = "myTableName";
        private const string ColumnName = "myColumnName";
        private const string FunctionName = "myFunction";
        private readonly ColumnOptions _columnOptions = new ColumnOptions();

        [Fact(DisplayName = "Should return a default insert statement when added the default configuration")]
        public void Should_return_only_one_valid_insert_statement_when_added_only_one_row()
        {
            var dataTable = new DataTable();
            var insertStatementExpected =
                $"INSERT ALL {Environment.NewLine}  INTO {TableName} (\"{ColumnName}\") VALUES (:{ColumnName}_0){Environment.NewLine}SELECT * FROM dual{Environment.NewLine}";

            dataTable.Columns.Add(ColumnName, typeof(string));
            dataTable.Rows.Add("data");

            var database = new OracleDatabaseBatchSink(ConnectionString, TableName, FunctionName, _columnOptions, null, null);
            var (insertData, _) = database.CreateInsertData(dataTable);

            insertData.Should().BeEquivalentTo(insertStatementExpected);
        }

        [Fact(DisplayName = "Should return one key and value when added only one column and row")]
        public void Should_return_two_valid_insert_statements_when_added_two_row()
        {
            var dataTable = new DataTable();
            var insertStatementExpected =
                $"INSERT ALL {Environment.NewLine}  INTO {TableName} (\"{ColumnName}\") VALUES (:{ColumnName}_0){Environment.NewLine}  INTO {TableName} (\"{ColumnName}\") VALUES (:{ColumnName}_1){Environment.NewLine}SELECT * FROM dual{Environment.NewLine}";

            dataTable.Columns.Add(ColumnName, typeof(string));
            dataTable.Rows.Add("data");
            dataTable.Rows.Add("data");

            var database = new OracleDatabaseBatchSink(ConnectionString, TableName, FunctionName, _columnOptions, null, null);
            var (insertData, _) = database.CreateInsertData(dataTable);

            insertData.Should().BeEquivalentTo(insertStatementExpected);
        }

        [Fact(DisplayName = "Should return two valid insert statements when added two row")]
        public void Should_return_one_key_and_value_when_added_only_one_column_and_row()
        {
            var dataTable = new DataTable();
            var value = "data";
            var (keyExpected, valueExpected) = ($":{ColumnName}_0", value);

            dataTable.Columns.Add(ColumnName, typeof(string));
            dataTable.Rows.Add(value);

            var database = new OracleDatabaseBatchSink(ConnectionString, TableName, FunctionName, _columnOptions, null, null);
            var (_, parameters) = database.CreateInsertData(dataTable);

            parameters.ContainsKey(keyExpected).Should().BeTrue();
            parameters[keyExpected].Should().BeEquivalentTo(valueExpected);
        }

        [Fact(DisplayName = "Should return only one valid insert statement when added only one row")]
        public void Should_return_a_default_insert_statement_when_added_the_default_configuration()
        {
            var database = new OracleDatabaseBatchSink(ConnectionString, TableName, FunctionName, _columnOptions, null, null);
            var defaultDataTable = database.CreateDataTable();
            var (id, message, messageTemplate, level, timestamp, exception, properties) =
                ("seq", "", "", "Debug", DateTime.MaxValue, "", "<properties></properties>");

            var defaultColumns = (
                from DataColumn dataColumn
                    in defaultDataTable.Columns
                select dataColumn.ColumnName).ToList();

            var columns = string.Join(", ", defaultColumns.Select(x => $@"""{x}"""));

            defaultColumns.RemoveAt(0);
            var parameters = string.Join(", ", defaultColumns.Select(x => $":{x}_0"));

            defaultDataTable.Rows.Add(id, message, messageTemplate, level, timestamp, exception, properties);
            var (insert, _) = database.CreateInsertData(defaultDataTable);

            var insertStatementExpected =
                $"INSERT ALL {Environment.NewLine}  INTO {TableName} ({columns}) VALUES (seq, {parameters}){Environment.NewLine}SELECT * FROM dual{Environment.NewLine}";

            insert.Should().BeEquivalentTo(insertStatementExpected);
        }

        [Fact(DisplayName = "Should insert additional columns on insert statement when configured")]
        public void Should_insert_additional_columns_on_insert_statement_when_configured()
        {
            var newColumnName = "XColumns";
            var additionalColumns = new HashSet<string>() { newColumnName };
            var columnOptions = new ColumnOptions
            {
                AdditionalDataColumns = new List<DataColumn>
                {
                    new DataColumn(newColumnName, typeof(string))
                }
            };

            var database = new OracleDatabaseBatchSink(ConnectionString, TableName, FunctionName, columnOptions, additionalColumns, null);
            var defaultDataTable = database.CreateDataTable();
            var (id, message, messageTemplate, level, timestamp, exception, properties) =
                ("seq", "", "", "Debug", DateTime.MaxValue, "", "<properties></properties>");

            var defaultColumns = (
                from DataColumn dataColumn
                    in defaultDataTable.Columns
                select dataColumn.ColumnName).ToList();

            var columns = string.Join(", ", defaultColumns.Select(x => $@"""{x}"""));

            defaultColumns.RemoveAt(0);
            var parameters = string.Join(", ", defaultColumns.Select(x => $":{x}_0"));

            defaultDataTable.Rows.Add(id, message, messageTemplate, level, timestamp, exception, properties);
            var (insert, _) = database.CreateInsertData(defaultDataTable);

            var insertStatementExpected =
                $"INSERT ALL {Environment.NewLine}  INTO {TableName} ({columns}) VALUES (seq, {parameters}){Environment.NewLine}SELECT * FROM dual{Environment.NewLine}";

            insert.Should().BeEquivalentTo(insertStatementExpected);
        }

        [Fact(DisplayName = "Should create an oracle command with the proper parameters")]
        public void Should_create_an_oracle_command_with_the_proper_parameters()
        {
            var database = new OracleDatabaseBatchSink(ConnectionString, TableName, FunctionName, _columnOptions, null, null);
            var command = new OracleCommand();
            var parameters = new Dictionary<string, object>
            {
                { "@param", 1 },
                { "@param2", 2 }
            };

            database.PrepareCommand(command, parameters);

            var relevantParameterKeyValue = command.Parameters.Cast<OracleParameter>()
                .ToDictionary(parameter => parameter.ParameterName, parameter => parameter.Value);

            relevantParameterKeyValue.Should().BeEquivalentTo(parameters);
        }

        [Fact(DisplayName = "Should create an oracle command with the proper parameters and with DbNull when a null value is provided")]
        public void Should_create_an_oracle_command_with_the_proper_parameters_and_with_DbNull_when_a_null_value_is_provided()
        {
            var database = new OracleDatabaseBatchSink(ConnectionString, TableName, FunctionName, _columnOptions, null, null);
            var command = new OracleCommand();
            var parameters = new Dictionary<string, object>
            {
                { "@param", 1 },
                { "@param2", null }
            };

            var parametersExpected = new Dictionary<string, object>
            {
                { "@param", 1 },
                { "@param2", DBNull.Value }
            };

            database.PrepareCommand(command, parameters);

            var relevantParameterKeyValue = command.Parameters.Cast<OracleParameter>()
                .ToDictionary(parameter => parameter.ParameterName, parameter => parameter.Value);

            relevantParameterKeyValue.Should().BeEquivalentTo(parametersExpected);
        }
    }
}
