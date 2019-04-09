using Serilog.Configuration;
using Serilog.Core;
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
            int batchPostingLimit = OraclePeriodBatchingSink.DefaultBatchPostingLimit,
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

            var defaultedPeriod = period ?? OraclePeriodBatchingSink.DefaultPeriod;

            return loggerConfiguration.Sink(
                new OraclePeriodBatchingSink(
                    batchPostingLimit,
                    defaultedPeriod,
                    queueLimit,
                    formatProvider,
                    columnOptions,
                    tableSpaceAndTableName,
                    tableSpaceAndFunctionName,
                    connectionString,
                    bindArrays,
                    flushOnExit
                ),
                restrictedToMinimumLevel
            );
        }
    }


}
