using Serilog.Events;
using Serilog.Sinks.Oracle;
using Serilog.Sinks.Oracle.Batch;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Serilog.Sinks.OracleConsoleTester
{
    class Program
    {
        private const long AmountOfLogs = 100000;

        static void Main(string[] args)
        {
            Serilog.Debugging.SelfLog.Enable(Console.Error);

            var logConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["OracleLogDB"].ConnectionString;

            // Config Logging for commandline
            //Log.Logger = new LoggerConfiguration()
            //    .MinimumLevel.Verbose()
            //    .WriteTo.Oracle(logConnectionString, "LOG", null, batchPostingLimit: 1000, queueLimit: (int)AmountOfLogs)
            //    .CreateLogger();

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Oracle(cfg => 
                    cfg.WithSettings(logConnectionString)
                    .UseBurstBatch()
                    .CreateSink())
                .CreateLogger();

            var MyLogger = Log.Logger.ForContext<Serilog.Sinks.OracleConsoleTester.Program>();

            Stopwatch sw = new Stopwatch();
            sw.Start();

            for (int i = 0; i < (AmountOfLogs / 8); i++)
            {
                MyLogger.Error("Simple Error Message");
                MyLogger.Warning("Simple Warning Message");
                MyLogger.Information("Simple Information Message");
                MyLogger.Debug("Simple Debug message!");
                MyLogger.Verbose("Simple Verbose message!");

                MyLogger.Information("Log message {i} and sleep for {slp} (ms)!", i, 0);

                MyLogger.Debug("Log Debug message {i} and sleep for {slp} (ms)!", i, 0);

                MyLogger.Verbose("Log Verbose message {i} and sleep for {slp} (ms)!", i, 0);
            }


            Log.CloseAndFlush();

            sw.Stop();

            Console.WriteLine("Storing {0} log events in: {1} (ms) to Oracle.", AmountOfLogs, sw.ElapsedMilliseconds);
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
