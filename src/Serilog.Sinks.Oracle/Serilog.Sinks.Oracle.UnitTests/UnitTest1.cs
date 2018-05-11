using Serilog.Events;
using System;
using System.Threading;
using Xunit;

namespace Serilog.Sinks.Oracle.UnitTests
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            var connectionString =
                "user id=system;password=oracle;data source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL = TCP)(HOST = localhost)(PORT = 49161)))(CONNECT_DATA=(SERVICE_NAME = xe)))";

            var logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(LogEventLevel.Debug)
                .WriteTo.Oracle(connectionString, "YOUR_TABLE_SPACE", "Log", LogEventLevel.Debug, 10, TimeSpan.FromSeconds(2))
                .CreateLogger();

            for (var i = 0; i < 5000; i++)
            {
                Thread.Sleep(200);
                logger.Debug(Guid.NewGuid().ToString());
            }
        }
    }
}
