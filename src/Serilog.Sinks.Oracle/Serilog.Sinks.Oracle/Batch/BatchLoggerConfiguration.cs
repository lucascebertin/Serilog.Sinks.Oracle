using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.Burst;
using Serilog.Sinks.Oracle.Columns;
using Serilog.Sinks.Oracle.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Serilog.Sinks.Oracle.Batch
{
    public class BatchLoggerConfiguration
    {
        interface IBatchConfig { }

        class PeriodicBatchConfig : IBatchConfig
        {
            public int PostingLimit { get; set; }
            public TimeSpan? Period { get; set; }
            public int QueueLimit { get; set; }
        }

        class BurstBatchConfig : IBatchConfig
        {
            public bool EnableTimer { get; set; }
            public double Interval { get; set; }
            public bool EnableBatchLimit { get; set; }
            public int BatchLimit { get; set; }
        }

        IBatchConfig _batchConfig = null;
        IList<ILogEventBatchSink> _storageSinks = new List<ILogEventBatchSink>();

        public BatchLoggerConfiguration UsePeriodicBatch(
            int batchPostingLimit = PeriodicBatchingSinkWrapper.DefaultBatchPostingLimit,
            TimeSpan? period = null,
            int queueLimit = 100)
        {
            if (_batchConfig != null)
            {
                throw new NotSupportedException("Can not use more than 1 Batch Configuration!");
            }

            _batchConfig = new PeriodicBatchConfig
            {
                PostingLimit = batchPostingLimit,
                Period = period,
                QueueLimit = queueLimit
            };

            return this;
        }

        public BatchLoggerConfiguration UseBurstBatch(
            bool enableTimer = true, 
            double interval = 5000, 
            bool enableBatchLimit = true, 
            int batchLimit = 100)
        {
            if (_batchConfig != null)
            {
                throw new NotSupportedException("Can not use more than 1 Batch Configuration!");
            }

            _batchConfig = new BurstBatchConfig
            {
                EnableTimer = enableTimer,
                Interval = interval,
                EnableBatchLimit = enableBatchLimit,
                BatchLimit = batchLimit
            };

            return this;
        }



        public BatchLoggerConfiguration UseOracle(string connectionString, 
            string tableSpaceAndTableName = "LOG",
            string tableSpaceAndFunctionName = null,
            ColumnOptions columnOptions = null,
            IFormatProvider formatProvider = null,
            bool bindArrays = true)
        {
            var receivedColumnOptions = columnOptions ?? new ColumnOptions();
            HashSet<string> additionalDataColumnNames = null;

            if (receivedColumnOptions.AdditionalDataColumns != null)
                additionalDataColumnNames = new HashSet<string>(
                    receivedColumnOptions.AdditionalDataColumns.Select(c => c.ColumnName),
                    StringComparer.OrdinalIgnoreCase
                );

            _storageSinks.Add(new OracleDatabaseBatchSink(
                                    connectionString, 
                                    tableSpaceAndTableName, 
                                    tableSpaceAndFunctionName, 
                                    columnOptions, 
                                    additionalDataColumnNames, 
                                    formatProvider, 
                                    bindArrays));

            return this;
        }

        public BatchLoggerConfiguration UseForStorage(ILogEventBatchSink storageSink)
        {
            _storageSinks.Add(storageSink);

            return this;
        }


        public ILogEventSink CreateSink()
        {
            switch (_batchConfig)
            {
                case PeriodicBatchConfig _:
                    {
                        var cfg = (PeriodicBatchConfig)_batchConfig;

                        return new PeriodicBatchingSinkWrapper(async lst =>
                                _storageSinks.ToList().ForEach(async storage =>
                                    await storage.EmitBatchAsync(lst)),
                            cfg.PostingLimit,
                            cfg.Period ?? PeriodicBatchingSinkWrapper.DefaultPeriod,
                            cfg.QueueLimit);
                    }
                case BurstBatchConfig _:
                    {
                        var cfg = (BurstBatchConfig)_batchConfig;

                        return new BurstSink((lst) => _storageSinks.ToList()
                                .ForEach((storage) => storage.EmitBatch(lst)),
                            cfg.EnableTimer, cfg.Interval, cfg.EnableBatchLimit, cfg.BatchLimit);
                    }
            }

            throw new NotSupportedException("You must select a Batch configuration (Periodic, Burst, ...)!");
        }
    }
}
