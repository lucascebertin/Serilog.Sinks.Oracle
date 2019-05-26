# Serilog.Sinks.Oracle

This project is a port from [Serilog.Sinks.MSSqlServerCore][serilog-mssql-url].
It's a netstandard2 library to provide a clean way to send Serilog events to Oracle 11 (and possible 12 too).

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

-- YOU CAN CHOOSE TO USE FUNCTION OR CREATE A TRIGGER... I PREFER THE TRIGGER WAY :) 
-- TRIGGER
CREATE TRIGGER 
	YOUR_TABLE_SPACE.LOG_TRIGGER 
BEFORE INSERT ON 
	YOUR_TABLE_SPACE.LOG 
REFERENCING 
	NEW AS NEW 
	OLD AS old 
FOR EACH ROW 
BEGIN 
	IF :new."Id" IS NULL THEN 
		SELECT 
			YOUR_TABLE_SPACE.LOG_SEQUENCE.NEXTVAL 
		INTO 
			:new."Id" 
		FROM dual; 
	END IF; 
END;


-- OR FUNCTION
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

  // If you choose to use the trigger just pass string.Empty in function name argument (tableSpaceAndFunctionName)
  var logger = new LoggerConfiguration()
	  .MinimumLevel.Verbose()
	  .WriteTo.Oracle(cfg => 
		  cfg.WithSettings(connectionString)
		  .UseBurstBatch() // or if you want to use PeriodicBatch, call .UsePeriodicBatch()
		  .CreateSink())
	  .CreateLogger();

  const string column = "ADDITIONALDATACOLUMN";
  var columnOptions = new ColumnOptions
  {
      AdditionalDataColumns = new List<DataColumn>
      {
          new DataColumn(column , typeof(string))
      }
  };

  Log.Logger = new LoggerConfiguration()
      //.Enrich.FromLogContext() /* uncomment this line if you want to store dynamic values and passing them by LogContext.PushProperty(name, value)... remember, this PushProperty is Disposable*/
      //.Enrich.WithProperty("column", "constant value, lika machine's hostname")  /* uncomment this line if you want to store a "constant value" */
      .MinimumLevel.Verbose()
      .WriteTo.Oracle(cfg =>
          cfg.WithSettings(logConnectionString, columnOptions: columnOptions)
          .UseBurstBatch()
          .CreateSink())
      .CreateLogger();


  //Be aware of the batch limit and delay time configured up here!
  logger.Debug("Yey, this message will be stored on Oracle!!");
```
## Travis useful notes
At the root you will find a Docker file named `Dockerfile.travis`.
To simulate, run this way:
```
docker build -t travis-ci-oracle -f Dockerfile.travis .
docker run -it travis-ci-oracle
```

## IMPORTANT NOTES!
This repository and package are in early stages, so, use it on your own and risk but feel free to contribute opening issues or sending pull-requests!

[travis-image]: https://img.shields.io/travis/lucascebertin/Serilog.Sinks.Oracle/master.svg?label=linux
[travis-url]: https://travis-ci.org/lucascebertin/Serilog.Sinks.Oracle

[appveyor-image]: https://ci.appveyor.com/api/projects/status/g7tw6rhtysx8t3w5?svg=true
[appveyor-url]: https://ci.appveyor.com/project/lcssk8board/serilog-sinks-oracle

[serilog-mssql-url]: https://github.com/serilog/serilog-sinks-mssqlserver

