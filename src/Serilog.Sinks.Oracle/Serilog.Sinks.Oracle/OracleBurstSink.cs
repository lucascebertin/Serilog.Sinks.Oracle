using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.Burst;
using Serilog.Sinks.Oracle.Columns;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Serilog.Sinks.Oracle
{
    public class OracleBurstSink : ILogEventSink, IDisposable
    {
        public const int DefaultBatchPostingLimit = 50;
        public static readonly TimeSpan DefaultPeriod = TimeSpan.FromSeconds(5);
        private readonly HashSet<string> _additionalDataColumnNames;
        private readonly Database _database;
        private readonly bool _bindArrays;

        private BurstSink _burstSink;

        public OracleBurstSink(int batchSizeLimit, TimeSpan period, int queueLimit, IFormatProvider formatProvider,
            ColumnOptions columnOptions,
            string tableSpaceAndTableName,
            string tableSpaceAndFunctionName,
            string connectionString,
            bool bindArrays,
            bool flushOnExit)
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

            _burstSink = new BurstSink((lst) => _database.StoreLog(lst, _bindArrays), batchLimit: batchSizeLimit);

            //// Make sure Last messages in queue(s) are getting flushed when exiting current process!
            //if (flushOnExit)
            //{
            //    AppDomain.CurrentDomain.ProcessExit += new EventHandler((obj, arg) => Dispose());
            //}
        }


        public void Emit(LogEvent logEvent) => _burstSink.Emit(logEvent);


        public void Dispose()
        {
            ((IDisposable)_burstSink).Dispose();
        }
    }
}
