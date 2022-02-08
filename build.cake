// Target - The task you want to start. Runs the Default task if not specified.
#tool "nuget:?package=xunit.runner.console"

using System.Text.RegularExpressions;

var target = Argument("Target", "Default");
var configuration = Argument("Configuration", "Release");
var source = Argument("source", "https://api.nuget.org/v3/index.json");
var apiKey = Argument<string>("apikey", null);
var version = Argument("version", "1.1.0");

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
		if ( !DirectoryExists(nuget) )
		{
			CreateDirectory(nuget);
		}

		NuGetRestore(solutionPath);
    });

// Build using the build configuration specified as an argument.
 Task("Build")
    .IsDependentOn("Update-Version")
    .Does(() =>
    {
		MSBuild(solutionPath, new MSBuildSettings {
				Verbosity = Verbosity.Minimal,
				ToolVersion = MSBuildToolVersion.VS2017,
				Restore = true,
				Configuration = configuration,
				PlatformTarget = PlatformTarget.MSIL
			});
    });

// Look under a 'Tests' folder and run dotnet test against all of those projects.
// Then drop the XML test results file in the Artifacts folder at the root.
Task("Test")
    .Does(() =>
    {
        XUnit2("./src/Serilog.Sinks.Oracle/*Tests/bin/Release/net452/*.UnitTests.dll");
        XUnit2("./src/Serilog.Sinks.Oracle/*Tests/bin/Release/net461/*.UnitTests.dll");

        var projects = GetFiles("./src/Serilog.Sinks.Oracle/*Tests*/**/*.csproj");
        foreach(var project in projects)
        {
            DotNetCoreTest(project.ToString(), new DotNetCoreTestSettings()
            {
                Configuration = configuration,
                Framework = "netcoreapp2.1",
                NoBuild = true,
                ArgumentCustomization = args => args.Append("--no-restore")
            });
        }
    });

// Publish the app to the /dist folder
Task("Publish")
    .IsDependentOn("Clean")
    .IsDependentOn("Build")
    .Does(() =>
    {
		// Note: Not publishing the UnitTest(s) projects!
		 Func<IFileSystemInfo, bool> exclude_node_modules =
									fileSystemInfo => !fileSystemInfo.Path.FullPath.Contains("Test");

		 var projects = GetFiles("./src/Serilog.Sinks.Oracle/Serilog.Sinks.Oracle/*.csproj", exclude_node_modules);
		 foreach(var project in projects)
		 {
			 Information("Publishing project: {0}", project);

			 // .NET 4.5.2
			DotNetCorePublish(
				project.FullPath,
				new DotNetCorePublishSettings()
				{
					Configuration = configuration,
					Framework = "net452",
					OutputDirectory = build.ToString() + "/lib/net452",
					ArgumentCustomization = args => args.Append("--no-restore"),
				});

			// .NET 4.6.1
			DotNetCorePublish(
				project.FullPath,
				new DotNetCorePublishSettings()
				{
					Configuration = configuration,
					Framework = "net461",
					OutputDirectory = build.ToString() + "/lib/net461",
					ArgumentCustomization = args => args.Append("--no-restore"),
				});

			// .NET Standard 2.0
			DotNetCorePublish(
				project.FullPath,
				new DotNetCorePublishSettings()
				{
					Configuration = configuration,
					Framework = "netstandard2.0",
					OutputDirectory = build.ToString() + "/lib/netstandard2.0",
					ArgumentCustomization = args => args.Append("--no-restore"),
				});

			// .NET 6.0
			DotNetCorePublish(
				project.FullPath,
				new DotNetCorePublishSettings()
				{
					Configuration = configuration,
					Framework = "net6.0",
					OutputDirectory = build.ToString() + "/lib/net6.0",
					ArgumentCustomization = args => args.Append("--no-restore"),
				});
			}
    });

Task("Package-NuGet")
    .IsDependentOn("Publish")
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
                                            new NuSpecContent {Source = "**", Target = "."},
																	   },	
									Dependencies			 = new [] {
											// .NETFramework4.5.2
											new NuSpecDependency {
												Id = "Serilog", 
												TargetFramework="net452", 
												Version="2.8.0"
											},
											new NuSpecDependency {
												Id = "Serilog.Sinks.PeriodicBatching", 
												TargetFramework="net452", 
												Version="2.1.1"
											},
											new NuSpecDependency {
												Id = "FakeItEasy", 
												TargetFramework="net452", 
												Version="5.1.0"
											},
											new NuSpecDependency {
												Id = "Oracle.ManagedDataAccess", 
												TargetFramework="net452", 
												Version="18.6.0"
											},
											new NuSpecDependency {
												Id = "System.ValueTuple",
												TargetFramework="net452", 
												Version="4.5.0"
											},

											// .NETFramework4.6.1
											new NuSpecDependency {
												Id = "Serilog", 
												TargetFramework="net461", 
												Version="2.8.0"
											},
											new NuSpecDependency {
												Id = "Serilog.Sinks.PeriodicBatching", 
												TargetFramework="net461", 
												Version="2.1.1"
											},
											new NuSpecDependency {
												Id = "FakeItEasy", 
												TargetFramework="net461", 
												Version="5.1.0"
											},
											new NuSpecDependency {
												Id = "Oracle.ManagedDataAccess", 
												TargetFramework="net461", 
												Version="18.6.0"
											},
											new NuSpecDependency {
												Id = "System.ValueTuple", 
												TargetFramework="net461",
												Version="4.5.0"
											},

											// .NETStandard2.0
											new NuSpecDependency {
												Id = "Serilog", 
												TargetFramework="netstandard2.0", 
												Version="2.8.0"
											},
											new NuSpecDependency {
												Id = "Serilog.Sinks.PeriodicBatching",
												TargetFramework="netstandard2.0", 
												Version="2.1.1"
											},
											new NuSpecDependency {
												Id = "FakeItEasy", 
												TargetFramework="netstandard2.0",
												Version="5.1.0"
											},
											new NuSpecDependency {
												Id = "Oracle.ManagedDataAccess.Core", 
												TargetFramework="netstandard2.0",
												Version="2.18.6"
											},
											new NuSpecDependency {
												Id = "NETStandard.Library",
												TargetFramework="netstandard2.0", 
												Version="2.0.3"
											},

											// .NET 6.0
											new NuSpecDependency {
												Id = "Serilog", 
												TargetFramework="net6.0", 
												Version="2.10.0"
											},
											new NuSpecDependency {
												Id = "Serilog.Sinks.PeriodicBatching",
												TargetFramework="net6.0", 
												Version="2.3.1"
											},
											new NuSpecDependency {
												Id = "FakeItEasy", 
												TargetFramework="net6.0",
												Version="7.3.0"
											},
											new NuSpecDependency {
												Id = "Oracle.ManagedDataAccess.Core", 
												TargetFramework="net6.0",
												Version="3.21.50"
											},
										},
                                     BasePath                = build,
                                     OutputDirectory         = nuget
                                 };

			NuGetPack("Serilog.Sinks.Oracle.nuspec", nuGetPackSettings);

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
    .Does(() =>
    {
        if(string.IsNullOrWhiteSpace(version))
            throw new CakeException("No version specified! You need to pass in --targetversion=\"x.y.z\"");

        var file =
            MakeAbsolute(File("./src/Serilog.Sinks.Oracle/Serilog.Sinks.Oracle/Serilog.Sinks.Oracle.csproj"));

        Information(file.FullPath);

        var project =
            System.IO.File.ReadAllText(file.FullPath, Encoding.UTF8);

        var projectVersion =
            new Regex(@"<Version>.+<\/Version>");

        project =
            projectVersion.Replace(project, string.Concat("<Version>", version, "</Version>"));

        System.IO.File.WriteAllText(file.FullPath, project, Encoding.UTF8);
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
