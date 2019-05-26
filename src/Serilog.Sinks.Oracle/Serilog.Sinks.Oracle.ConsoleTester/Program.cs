﻿using Serilog.Sinks.Oracle.Columns;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;

namespace Serilog.Sinks.OracleConsoleTester
{
    class Program
    {
        private const long AmountOfLogs = 100000;

        static void Main()
        {
            Debugging.SelfLog.Enable(Console.Error);

            var logConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["OracleLogDB"].ConnectionString;

            // Config Logging for commandline
            //Log.Logger = new LoggerConfiguration()
            //    .MinimumLevel.Verbose()
            //    .WriteTo.Oracle(logConnectionString, "LOG", null, batchPostingLimit: 1000, queueLimit: (int)AmountOfLogs)
            //    .CreateLogger();

            const string column = "ADDITIONALDATACOLUMN";
            var columnOptions = new ColumnOptions
            {
                AdditionalDataColumns = new List<DataColumn>
                {
                    new DataColumn(column , typeof(string))
                }
            };

            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .Enrich.WithProperty(column, "TEST")
                .MinimumLevel.Verbose()
                .WriteTo.Oracle(cfg =>
                    cfg.WithSettings(logConnectionString, columnOptions: columnOptions)
                    .UseBurstBatch()
                    .CreateSink())
                .CreateLogger();

            var myLogger = Log.Logger.ForContext<Program>();

            var sw = new Stopwatch();
            sw.Start();


            for (var i = 0; i < AmountOfLogs / 8; i++)
            {
                myLogger.Error("Simple Error Message");
                myLogger.Warning("Simple Warning Message");
                myLogger.Information("Simple Information Message");
                myLogger.Debug("Simple Debug message!");
                myLogger.Verbose("Simple Verbose message!");
                myLogger.Information("Log message {i} and sleep for {slp} (ms)!", i, 0);
                myLogger.Debug("Log Debug message {i} and sleep for {slp} (ms)!", i, 0);
                myLogger.Verbose("Log Verbose message {i} and sleep for {slp} (ms)!", i, 0);
            }

            Log.CloseAndFlush();

            sw.Stop();

            Console.WriteLine("Storing {0} log events in: {1} (ms) to Oracle.", AmountOfLogs, sw.ElapsedMilliseconds);
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
