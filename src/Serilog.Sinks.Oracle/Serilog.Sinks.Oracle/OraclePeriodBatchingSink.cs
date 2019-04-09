using Serilog.Events;
using Serilog.Sinks.Oracle.Columns;
using Serilog.Sinks.PeriodicBatching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Serilog.Sinks.Oracle
{
    public class OraclePeriodBatchingSink : PeriodicBatchingSink
    {
        public const int DefaultBatchPostingLimit = 50;
        public static readonly TimeSpan DefaultPeriod = TimeSpan.FromSeconds(5);
        private readonly HashSet<string> _additionalDataColumnNames;
        private readonly Database _database;
        private readonly bool _bindArrays;

        public OraclePeriodBatchingSink(int batchSizeLimit, TimeSpan period, int queueLimit, IFormatProvider formatProvider,
            ColumnOptions columnOptions, 
            string tableSpaceAndTableName, 
            string tableSpaceAndFunctionName, 
            string connectionString, 
            bool bindArrays,
            bool flushOnExit)
            : base(batchSizeLimit, period, queueLimit)
        {
            _bindArrays = bindArrays;

            var receivedColumnOptions = columnOptions ?? new ColumnOptions();

            if (receivedColumnOptions.AdditionalDataColumns != null)
                _additionalDataColumnNames = new HashSet<string>(
                    receivedColumnOptions.AdditionalDataColumns.Select(c => c.ColumnName),
                    StringComparer.OrdinalIgnoreCase
                );

            _database = new Database(connectionString, tableSpaceAndTableName, tableSpaceAndFunctionName,
                receivedColumnOptions, _additionalDataColumnNames, formatProvider);

            // Make sure Last messages in queue(s) are getting flushed when exiting current process!
            if (flushOnExit)
            {
                AppDomain.CurrentDomain.ProcessExit += new EventHandler((obj, arg) => Dispose());
            }
        }

        protected override async Task EmitBatchAsync(IEnumerable<LogEvent> events) =>
            await _database.StoreLogAsync(events, _bindArrays);

        protected override void EmitBatch(IEnumerable<LogEvent> events) =>
            _database.StoreLog(events, _bindArrays);
    }
}
