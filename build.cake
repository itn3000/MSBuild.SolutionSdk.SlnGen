using IO = System.IO;
using Cake.Common.Tools.MSBuild;
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Debug");

Task("Default")
    .IsDependentOn("Test")
    .Does(() =>
    {

    });

Task("Build")
    .Does(() =>
    {
        MSBuild("./src/MSBuild.SolutionSdk.SlnGen.slnproj", (setting) =>
        {
            setting.SetConfiguration(configuration)
                .SetVerbosity(Verbosity.Normal)
                .WithTarget("Restore")
                ;
        });
        MSBuild("./src/MSBuild.SolutionSdk.SlnGen.slnproj", (setting) =>
        {
            setting.SetConfiguration(configuration)
                .SetVerbosity(Verbosity.Normal)
                .WithTarget("Build")
                ;
        });
        MSBuild("./src/MSBuild.SolutionSdk.SlnGen/MSBuild.SolutionSdk.SlnGen.csproj", (setting) =>
        {
            setting.SetConfiguration(configuration)
                .SetVerbosity(Verbosity.Normal)
                .WithTarget("Pack")
                ;
        });
    });
Task("Test")
    .IsDependentOn("Build");
Task("Test.MultipleSolutions")
    .IsDependeeOf("Test")
    .Does(() =>
    {
        var projectDirectory = "./tests/MultipleSolutions";
        var projectFilePath = IO.Path.Combine(projectDirectory, "multi.slngenproj");
        MSBuild("./tests/MultipleSolutions/multi.slngenproj", (setting) =>
        {
            setting.SetConfiguration(configuration)
                .SetVerbosity(Verbosity.Normal)
                .WithTarget("SlnGen");
        });
        foreach(var slnFile in new string[]{ "sln1/sln1.sln", "sln2/sln2.sln" })
        {
            var slnFilePath = IO.Path.Combine(projectDirectory, slnFile);
            if(!IO.File.Exists(slnFilePath))
            {
                throw new InvalidOperationException(string.Format("{0} is not created", slnFile));
            }
        }
    });

RunTarget(target);
