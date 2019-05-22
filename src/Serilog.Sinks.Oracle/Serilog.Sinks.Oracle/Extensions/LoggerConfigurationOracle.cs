using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.Oracle.Batch;
using Serilog.Sinks.Oracle.Columns;
using System;

namespace Serilog
{
    public static class LoggerConfigurationOracle
    {
        public static LoggerConfiguration Oracle(
            this LoggerSinkConfiguration loggerConfiguration,
            Func<BatchLoggerConfiguration, ILogEventSink> configureFunction,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum)
        {
            if (loggerConfiguration == null)
                throw new ArgumentNullException(nameof(loggerConfiguration));

            return loggerConfiguration.Sink(
                configureFunction(new BatchLoggerConfiguration()),
                restrictedToMinimumLevel);
        }
    }
}
