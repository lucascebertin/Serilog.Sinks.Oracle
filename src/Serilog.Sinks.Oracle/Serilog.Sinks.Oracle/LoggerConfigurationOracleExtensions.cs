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
            string tableSpace,
            string tableName,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            int batchPostingLimit = OracleSink.DefaultBatchPostingLimit,
            TimeSpan? period = null,
            int queueLimit = 100,
            IFormatProvider formatProvider = null,
            bool autoCreateSqlTable = false,
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
                    tableSpace,
                    tableName,
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
        private readonly ColumnOptions _columnOptions;
        private readonly string _tableSpace;
        private readonly string _tableName;
        private readonly IFormatProvider _formatProvider;
        private readonly HashSet<string> _additionalDataColumnNames;
        private readonly Database _database;


        public OracleSink(int batchSizeLimit, TimeSpan period, int queueLimit, IFormatProvider formatProvider,
            ColumnOptions columnOptions, string tableSpace, string tableName, string connectionString)
            : base(batchSizeLimit, period, queueLimit)
        {
            _columnOptions = columnOptions ?? new ColumnOptions();
            _tableSpace = tableSpace;
            _tableName = tableName;
            _formatProvider = formatProvider;

            if (_columnOptions.AdditionalDataColumns != null)
                _additionalDataColumnNames = new HashSet<string>(
                    _columnOptions.AdditionalDataColumns.Select(c => c.ColumnName),
                    StringComparer.OrdinalIgnoreCase
                );

            _database = new Database(connectionString, _tableSpace, _tableName, 
                _columnOptions, _additionalDataColumnNames, _formatProvider);
        }

        protected override async Task EmitBatchAsync(IEnumerable<LogEvent> events) =>
            await _database.StoreLogAsync(events);

        protected override void EmitBatch(IEnumerable<LogEvent> events) =>
            _database.StoreLog(events);
    }
}
