using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.Oracle;
using Serilog.Sinks.Oracle.Batch;
using Serilog.Sinks.Oracle.Columns;
using Serilog.Sinks.PeriodicBatching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Serilog
{
    public static class LoggerConfigurationOracle
    {
        public static LoggerConfiguration Oracle(
            this LoggerSinkConfiguration loggerConfiguration,
            string connectionString,
            string tableSpaceAndTableName,
            string tableSpaceAndFunctionName,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            int batchPostingLimit = PeriodicBatchingSinkWrapper.DefaultBatchPostingLimit,
            TimeSpan? period = null,
            int queueLimit = 100,
            IFormatProvider formatProvider = null,
            ColumnOptions columnOptions = null,
            bool bindArrays = false,
            bool flushOnExit = false
        )
        {
            if (loggerConfiguration == null)
                throw new ArgumentNullException(nameof(loggerConfiguration));

            var defaultedPeriod = period ?? PeriodicBatchingSinkWrapper.DefaultPeriod;

            return loggerConfiguration.Batch((cfg) =>
                                            cfg.UsePeriodicBatch(batchPostingLimit, period, queueLimit)
                                                .UseOracle(connectionString,
                                                            tableSpaceAndTableName,
                                                            tableSpaceAndFunctionName,
                                                            columnOptions,
                                                            formatProvider,
                                                            bindArrays)
                                                .CreateSink());
        }


        public static LoggerConfiguration Batch(
            this LoggerSinkConfiguration loggerConfiguration,
            Func<BatchLoggerConfiguration, ILogEventSink> cfgFunc,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum)
        {
            if (loggerConfiguration == null)
                throw new ArgumentNullException(nameof(loggerConfiguration));

            return loggerConfiguration.Sink(
                        cfgFunc(new BatchLoggerConfiguration()),
                        restrictedToMinimumLevel);
        }

    }


}
