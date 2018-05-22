# Serilog.Sinks.Oracle

This project is a port from [Serilog.Sinks.MSSqlServerCore].
It's a netstandard2 library to provide a clean way to send Serilog events to Oracle 11 (annd possible 12 too).

[![Linux Build][travis-image]][travis-url]
[![Windows Build][appveyor-image]][appveyor-url]

## Gettins started

### Prerequisites
On Windows, powershell (most up to date, please...)
```
PS C:\Path\To\The\Project> ./build.ps1
```

On Linux (be free!)
```
$ ./build.sh
```

### Database Scripts
```sql
CREATE TABLE YOUR_TABLE_SPACE.LOG(
  "Id"                 INT             NOT NULL ENABLE,
  "Message"            CLOB            NULL,
  "MessageTemplate"    CLOB            NULL,
  "Level"              NVARCHAR2(128)  NULL,
  "TimeStamp"          TIMESTAMP       NOT NULL,
  "Exception"          CLOB            NULL,
  "Properties"         CLOB            NULL,
  "LogEvent"           CLOB            NULL
);

CREATE SEQUENCE YOUR_TABLE_SPACE.LOG_SEQUENCE START WITH 1 INCREMENT BY 1;

CREATE FUNCTION YOUR_TABLE_SPACE.get_seq RETURN INT IS
BEGIN
  RETURN YOUR_TABLE_SPACE.LOG_SEQUENCE.NEXTVAL;
END;
```

### Using it on your app
```csharp
  //Don't forget to add the namespace, ok?

  var connectionString =
      "user id=system;password=oracle;data source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL = TCP)(HOST = localhost)(PORT = 49161)))(CONNECT_DATA=(SERVICE_NAME = xe)))";

  var logger = new LoggerConfiguration()
      .MinimumLevel.Debug()
      .WriteTo.Oracle(connectionString, "YOUR_TABLE_SPACE.YOUR_TABLE_NAME", "YOUR_TABLE_SPACE.get_seq", LogEventLevel.Debug, 10, TimeSpan.FromSeconds(2))
      .CreateLogger();

  //Be aware of the batch limit and delay time configured up here!
  logger.Debug("Yey, this message will be stored on Oracle!!");
```

## IMPORTANT NOTES!
This repository and package are in early stages, so, use it on your own and risk but feel free to contribute opening issues or sending pull-requests!

[travis-image]: https://img.shields.io/travis/lucascebertin/Serilog.Sinks.Oracle/master.svg?label=linux
[travis-url]: https://travis-ci.org/lucascebertin/Serilog.Sinks.Oracle

[appveyor-image]: https://ci.appveyor.com/api/projects/status/g7tw6rhtysx8t3w5?svg=true
[appveyor-url]: https://ci.appveyor.com/project/lcssk8board/serilog-sinks-oracle
