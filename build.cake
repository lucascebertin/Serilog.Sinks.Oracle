// Target - The task you want to start. Runs the Default task if not specified.

using System.Text.RegularExpressions;

var target = Argument("Target", "Default");
var configuration = Argument("Configuration", "Release");
var source = Argument("source", "https://api.nuget.org/v3/index.json");
var apiKey = Argument<string>("apikey", null);
var version = Argument("version", "1.0.7");

Information($"Running target {target} in configuration {configuration}");

var solutionPath = "./src/Serilog.Sinks.Oracle/Serilog.Sinks.Oracle.sln";
var output = Directory("./dist");
var build = output + Directory("build");
var nuget = output + Directory("nuget");

// Deletes the contents of the Artifacts folder if it contains anything from a previous build.
Task("Clean")
    .Does(() =>
    {
        CleanDirectory(output);
    });

// Run dotnet restore to restore all package references.
Task("Restore")
    .Does(() =>
    {
        DotNetCoreRestore(solutionPath);
    });

// Build using the build configuration specified as an argument.
 Task("Build")
    .Does(() =>
    {
		MSBuild(solutionPath, new MSBuildSettings {
				Verbosity = Verbosity.Minimal,
				ToolVersion = MSBuildToolVersion.VS2017,
				Restore = true,
				Configuration = configuration,
				PlatformTarget = PlatformTarget.MSIL
				});

		MSBuild(solutionPath, new MSBuildSettings {
				Verbosity = Verbosity.Minimal,
				ToolVersion = MSBuildToolVersion.VS2017,
				Restore = true,
				Configuration = configuration,
				PlatformTarget = PlatformTarget.x86
				});

		MSBuild(solutionPath, new MSBuildSettings {
				Verbosity = Verbosity.Minimal,
				ToolVersion = MSBuildToolVersion.VS2017,
				Restore = true,
				Configuration = configuration,
				PlatformTarget = PlatformTarget.x64
				});

		

//        DotNetCoreBuild(solutionPath,
//            new DotNetCoreBuildSettings()
//            {
//                Configuration = configuration,
//                ArgumentCustomization = args => args.Append("--no-restore"),
//            });
    });

// Look under a 'Tests' folder and run dotnet test against all of those projects.
// Then drop the XML test results file in the Artifacts folder at the root.
Task("Test")
    .Does(() =>
    {
        var projects = GetFiles("./src/Serilog.Sinks.Oracle/*StdUnitTest*/*.csproj");
        foreach(var project in projects)
        {
            Information("Testing project " + project);
            DotNetCoreTest(
                project.ToString(),
                new DotNetCoreTestSettings()
                {
                    Configuration = configuration,
                    NoBuild = true,
                    ArgumentCustomization = args => args.Append("--no-restore"),
                });
        }
    });

// Publish the app to the /dist folder
Task("Publish")
    .Does(() =>
    {
        DotNetCorePublish(
            solutionPath,
            new DotNetCorePublishSettings()
            {
                Configuration = configuration,
                OutputDirectory = build,
                ArgumentCustomization = args => args.Append("--no-restore"),
            });
    });

Task("Package-NuGet")
    .IsDependentOn("Update-Version")
    .Description("Generates NuGet packages for each project")
    .Does(() =>
    {
		 
		 if ( !DirectoryExists(nuget) )
		 {
			CreateDirectory(nuget);
		 }

	     var nuGetPackSettings   = new NuGetPackSettings {
                                     Id                      = "Serilog.Sinks.Oracle",
                                     Version                 =  version,
                                     RequireLicenseAcceptance= false,
                                     Symbols                 = false,
                                     NoPackageAnalysis       = true,
                                     Files                   = new [] {
                                                                          new NuSpecContent {Source = "src/Serilog.Sinks.Oracle/Serilog.Sinks.OracleNet452/bin/x86/Release/Serilog.Sinks.Oracle.*", Target = "runtimes/win10-x86/lib/net452"},
                                                                          new NuSpecContent {Source = "src/Serilog.Sinks.Oracle/Serilog.Sinks.OracleNet452/bin/x64/Release/Serilog.Sinks.Oracle.*", Target = "runtimes/win10-x64/lib/net452"},

                                                                          new NuSpecContent {Source = "src/Serilog.Sinks.Oracle/Serilog.Sinks.OracleNet461/bin/x86/Release/Serilog.Sinks.Oracle.*", Target = "runtimes/win10-x86/lib/net461"},
                                                                          new NuSpecContent {Source = "src/Serilog.Sinks.Oracle/Serilog.Sinks.OracleNet461/bin/x64/Release/Serilog.Sinks.Oracle.*", Target = "runtimes/win10-x64/lib/net461"},

                                                                          new NuSpecContent {Source = "src/Serilog.Sinks.Oracle/Serilog.Sinks.OracleNetStandard20/bin/x86/Release/netstandard2.0/Serilog.Sinks.Oracle.*", Target = "runtimes/win10-x86/lib/netstandard2.0"},
                                                                          new NuSpecContent {Source = "src/Serilog.Sinks.Oracle/Serilog.Sinks.OracleNetStandard20/bin/x64/Release/netstandard2.0/Serilog.Sinks.Oracle.*", Target = "runtimes/win10-x64/lib/netstandard2.0"},
                                                                       
                                                                          new NuSpecContent {Source = "src/Serilog.Sinks.Oracle/Serilog.Sinks.OracleNetCore21/bin/x86/Release/netcoreapp2.1/Serilog.Sinks.Oracle.*", Target = "runtimes/win10-x86/lib/netcore2.1"},
                                                                          new NuSpecContent {Source = "src/Serilog.Sinks.Oracle/Serilog.Sinks.OracleNetCore21/bin/x64/Release/netcoreapp2.1/Serilog.Sinks.Oracle.*", Target = "runtimes/win10-x64/lib/netcore2.1"},
																	   },
									 Dependencies			 = new [] {
																		  new NuSpecDependency {Id = "Serilog", TargetFramework=".NETFramework4.5.2", Version="2.8.0"},
																		  new NuSpecDependency {Id = "Serilog.Sinks.PeriodicBatching", TargetFramework=".NETFramework4.5.2", Version="2.1.1"},
																		  new NuSpecDependency {Id = "FakeItEasy", TargetFramework=".NETFramework4.5.2", Version="5.1.0"},
																		  new NuSpecDependency {Id = "Oracle.ManagedDataAccess", TargetFramework=".NETFramework4.5.2", Version="18.6.0"},
																		  new NuSpecDependency {Id = "System.ValueTuple", TargetFramework=".NETFramework4.5.2", Version="4.5.0"},
																		  
																		  
																		  new NuSpecDependency {Id = "Serilog", TargetFramework=".NETFramework4.6.1", Version="2.8.0"},
																		  new NuSpecDependency {Id = "Serilog.Sinks.PeriodicBatching", TargetFramework=".NETFramework4.6.1", Version="2.1.1"},
																		  new NuSpecDependency {Id = "FakeItEasy", TargetFramework=".NETFramework4.6.1", Version="5.1.0"},
																		  new NuSpecDependency {Id = "Oracle.ManagedDataAccess", TargetFramework=".NETFramework4.6.1", Version="18.6.0"},
																		  new NuSpecDependency {Id = "System.ValueTuple", TargetFramework=".NETFramework4.6.1", Version="4.5.0"},

																		  new NuSpecDependency {Id = "Serilog", TargetFramework=".NETStandard2.0", Version="2.8.0"},
																		  new NuSpecDependency {Id = "Serilog.Sinks.PeriodicBatching", TargetFramework=".NETStandard2.0", Version="2.1.1"},
																		  new NuSpecDependency {Id = "FakeItEasy", TargetFramework=".NETStandard2.0", Version="5.1.0"},
																		  new NuSpecDependency {Id = "Oracle.ManagedDataAccess.Core", TargetFramework=".NETStandard2.0", Version="2.18.6"},
																		  new NuSpecDependency {Id = "NETStandard.Library", TargetFramework=".NETStandard2.0", Version="2.0.3"},

																		  new NuSpecDependency {Id = "Serilog", TargetFramework=".NETCore2.1", Version="2.8.0"},
																		  new NuSpecDependency {Id = "Serilog.Sinks.PeriodicBatching", TargetFramework=".NETCore2.1", Version="2.1.1"},
																		  new NuSpecDependency {Id = "FakeItEasy", TargetFramework=".NETCore2.1", Version="5.1.0"},
																		  new NuSpecDependency {Id = "Oracle.ManagedDataAccess.Core", TargetFramework=".NETCore2.1", Version="2.18.6"},
																		  new NuSpecDependency {Id = "Microsoft.NETCore.App", TargetFramework=".NETCore2.1", Version="2.1.0"}
																	  },	
                                     BasePath                = ".",
                                     OutputDirectory         = nuget
                                 };

			NuGetPack("Serilog.Sinks.Oracle.nuspec", nuGetPackSettings);


//        foreach(var project in GetFiles("./src/Serilog.Sinks.Oracle/Serilog.Sinks.Oracle*/*.csproj"))
//        {
//            Information("Packaging " + project.GetFilename().FullPath);

//            var content =
//                System.IO.File.ReadAllText(project.FullPath, Encoding.UTF8);

//			DotNetCorePack(project.GetDirectory().FullPath, new DotNetCorePackSettings {
//                Configuration = configuration,
//                OutputDirectory = nuget
//            });
//        }
    });

Task("Publish-NuGet")
    .IsDependentOn("Package-Nuget")
    .Description("Pushes the nuget packages in the nuget folder to a NuGet source. Also publishes the packages into the feeds.")
    .Does(() =>
    {
        if(string.IsNullOrWhiteSpace(apiKey))
            throw new CakeException("No NuGet API key provided. You need to pass in --apikey=\"xyz\"");

        var packages =
            GetFiles(nuget.Path.FullPath + "/*.nupkg") -
            GetFiles(nuget.Path.FullPath + "/*.symbols.nupkg");

        foreach(var package in packages)
        {
            NuGetPush(package, new NuGetPushSettings {
                Source = source,
                ApiKey = apiKey
            });
        }
    });

Task("Update-Version")
    .IsDependentOn("Build")
    .Does(() =>
    {
        Information("U should set the version of all projects to " + version);
		
		/*
        if(string.IsNullOrWhiteSpace(version))
            throw new CakeException("No version specified! You need to pass in --targetversion=\"x.y.z\"");

        var file =
            MakeAbsolute(File("./src/Serilog.Sinks.Oracle/Serilog.Sinks.OracleStd/Serilog.Sinks.OracleStd.csproj"));

        Information(file.FullPath);

        var project =
            System.IO.File.ReadAllText(file.FullPath, Encoding.UTF8);

        var projectVersion =
            new Regex(@"<Version>.+<\/Version>");

        project =
            projectVersion.Replace(project, string.Concat("<Version>", version, "</Version>"));

        System.IO.File.WriteAllText(file.FullPath, project, Encoding.UTF8);
		*/
    });

// A meta-task that runs all the steps to Build and Test the app
Task("BuildAndTest")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
    .IsDependentOn("Build")
    .IsDependentOn("Test");

// The default task to run if none is explicitly specified. In this case, we want
// to run everything starting from Clean, all the way up to Publish.
Task("Default")
    .IsDependentOn("BuildAndTest")
    .IsDependentOn("Publish");

// Executes the task specified in the target argument.
RunTarget(target);