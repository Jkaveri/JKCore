var target          = Argument("target", "Default");
var configuration   = Argument<string>("configuration", "Release");
var nugetSource     = Argument<string>("nugetSource");
var nugetApiKey     = Argument<string>("nugetApiKey");

///////////////////////////////////////////////////////////////////////////////
// GLOBAL VARIABLES
///////////////////////////////////////////////////////////////////////////////
var isLocalBuild        = !AppVeyor.IsRunningOnAppVeyor;
var packPath            = Directory("./src/JKCore");
var sourcePath          = Directory("./src");
var testsPath           = Directory("test");
var buildArtifacts      = Directory("./artifacts/packages");


Information(nugetSource);
Information(nugetApiKey);

Task("Build")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
    .Does(() =>
{
	var projects = GetFiles("./**/project.json");

	foreach(var project in projects)
	{
        var settings = new DotNetCoreBuildSettings 
        {
            Configuration = configuration
        };

        DotNetCoreBuild(project.GetDirectory().FullPath, settings); 
    }
});

Task("RunTests")
    .IsDependentOn("Restore")
    .IsDependentOn("Clean")
    .Does(() =>
{
    var projects = GetFiles("./test/**/project.json");

    foreach(var project in projects)
	{
        var settings = new DotNetCoreTestSettings
        {
            Configuration = configuration
        };

        if (!IsRunningOnWindows())
        {
            Information("Not running on Windows - skipping tests for full .NET Framework");
            settings.Framework = "netcoreapp1.1";
        }

        DotNetCoreTest(project.GetDirectory().FullPath, settings);
    }
});

Task("Pack")
    .IsDependentOn("Restore")
    .IsDependentOn("Clean")
    .Does(() =>
{
    var projects = GetFiles("./src/**/project.json");

    Information("ALL PROJECTS");
    foreach (var project in projects) {
        var settings = new DotNetCorePackSettings
        {
            Configuration = configuration,
            OutputDirectory = buildArtifacts,
        };

        // add build suffix for CI builds
        if(!isLocalBuild)
        {
            settings.VersionSuffix = "build" + AppVeyor.Environment.Build.Number.ToString().PadLeft(5,'0');
        }
        DotNetCorePack(project.GetDirectory().FullPath, settings);
    }   
});

Task("Clean")
    .Does(() =>
{
    CleanDirectories(new DirectoryPath[] { buildArtifacts });
});

Task("Restore")
    .Does(() =>
{
    var settings = new DotNetCoreRestoreSettings
    {
        Sources = new [] { "https://api.nuget.org/v3/index.json" }
    };

    DotNetCoreRestore(sourcePath, settings);
    DotNetCoreRestore(testsPath, settings);
});

Task("Default")
  .IsDependentOn("Build")
  .IsDependentOn("RunTests")
  .IsDependentOn("Pack");

RunTarget(target);