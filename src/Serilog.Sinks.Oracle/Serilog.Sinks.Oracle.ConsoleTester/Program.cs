using Serilog.Events;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Serilog.Sinks.OracleConsoleTester
{
    class Program
    {
        private const long AmountOfLogs = 10000;

        static void Main(string[] args)
        {
            Serilog.Debugging.SelfLog.Enable(Console.Error);

            var logConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["OracleLogDB"].ConnectionString;

            // Config Logging for commandline
            Log.Logger = new LoggerConfiguration()
                            .MinimumLevel.Verbose()
                            .WriteTo.Oracle(logConnectionString, "LOG", null, LogEventLevel.Verbose, batchPostingLimit:1000, bindArrays: true, queueLimit: (int)AmountOfLogs)
                            .CreateLogger();

            var MyLogger = Log.Logger.ForContext<Serilog.Sinks.OracleConsoleTester.Program>();

            var oraSinkAsm = Assembly.GetAssembly(typeof(Serilog.Sinks.Oracle.OracleSink));
            Console.WriteLine("Using {0} from {1}", oraSinkAsm.FullName, oraSinkAsm.CodeBase);
            Console.WriteLine("Location: {0}", oraSinkAsm.Location);

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

            Stopwatch sw = new Stopwatch();
            sw.Start();

            Log.CloseAndFlush();

            sw.Stop();

            Console.WriteLine("Storing {0} log events in: {1} (ms) to Oracle.", AmountOfLogs, sw.ElapsedMilliseconds);
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
