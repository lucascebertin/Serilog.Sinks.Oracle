using Serilog.Configuration;
using Serilog.Events;
using Serilog.Sinks.Oracle;
using Serilog.Sinks.Oracle.Columns;
using Serilog.Sinks.PeriodicBatching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Serilog
{
    public static class LoggerConfigurationOracleExtensions
    {
        public static LoggerConfiguration Oracle(
            this LoggerSinkConfiguration loggerConfiguration,
            string connectionString,
            string tableSpaceAndTableName,
            string tableSpaceAndFunctionName,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            int batchPostingLimit = OracleSink.DefaultBatchPostingLimit,
            TimeSpan? period = null,
            int queueLimit = 100,
            IFormatProvider formatProvider = null,
            ColumnOptions columnOptions = null
        )
        {
            if (loggerConfiguration == null)
                throw new ArgumentNullException("loggerConfiguration");

            var defaultedPeriod = period ?? OracleSink.DefaultPeriod;

            return loggerConfiguration.Sink(
                new OracleSink(
                    batchPostingLimit,
                    defaultedPeriod,
                    queueLimit,
                    formatProvider,
                    columnOptions,
                    tableSpaceAndTableName,
                    tableSpaceAndFunctionName,
                    connectionString
                ),
                restrictedToMinimumLevel
            );
        }
    }

    public class OracleSink : PeriodicBatchingSink
    {
        public const int DefaultBatchPostingLimit = 50;
        public static readonly TimeSpan DefaultPeriod = TimeSpan.FromSeconds(5);
        private readonly HashSet<string> _additionalDataColumnNames;
        private readonly Database _database;


        public OracleSink(int batchSizeLimit, TimeSpan period, int queueLimit, IFormatProvider formatProvider,
            ColumnOptions columnOptions, string tableSpaceAndTableName, string tableSpaceAndFunctionName, string connectionString)
            : base(batchSizeLimit, period, queueLimit)
        {
            var receivedColumnOptions = columnOptions ?? new ColumnOptions();

            if (receivedColumnOptions.AdditionalDataColumns != null)
                _additionalDataColumnNames = new HashSet<string>(
                    receivedColumnOptions.AdditionalDataColumns.Select(c => c.ColumnName),
                    StringComparer.OrdinalIgnoreCase
                );

            _database = new Database(connectionString, tableSpaceAndTableName, tableSpaceAndFunctionName,
                receivedColumnOptions, _additionalDataColumnNames, formatProvider);
        }

        protected override async Task EmitBatchAsync(IEnumerable<LogEvent> events) =>
            await _database.StoreLogAsync(events);

        protected override void EmitBatch(IEnumerable<LogEvent> events) =>
            _database.StoreLog(events);
    }
}
