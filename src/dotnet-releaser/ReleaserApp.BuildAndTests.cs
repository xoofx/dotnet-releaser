using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetReleaser.Runners;

namespace DotNetReleaser;

public partial class ReleaserApp
{
    private async Task<bool> BuildAndTest(BuildInformation buildInfo)
    {
        var coverage = _config.Coverage.Enable && _config.Test.Enable;

        // Build
        foreach (var projectPackageInfoCollection in buildInfo.ProjectPackageInfoCollections)
        {
            if (!string.IsNullOrEmpty(projectPackageInfoCollection.SolutionFile))
            {
                if (!await Build(projectPackageInfoCollection.SolutionFile, false))
                {
                    return false;
                }

                if (coverage)
                {
                    foreach (var projectPackageInfo in
                             projectPackageInfoCollection.Packages.Where(x => x.IsTestProject))
                    {
                        if (!await Build(projectPackageInfo.ProjectFullPath, isTestProject: true))
                        {
                            return false;
                        }

                    }
                }
            }
            else
            {
                foreach (var projectPackageInfo in projectPackageInfoCollection.Packages)
                {
                    if (!await Build(projectPackageInfo.ProjectFullPath, isTestProject: projectPackageInfo.IsTestProject))
                    {
                        return false;
                    }
                }

            }
        }

        // Test
        if (_config.Test.Enable)
        {
            foreach (var projectPackageInfoCollection in buildInfo.ProjectPackageInfoCollections)
            {
                if (!string.IsNullOrEmpty(projectPackageInfoCollection.SolutionFile))
                {
                    if (projectPackageInfoCollection.Packages.Any(x => x.IsTestProject))
                    {
                        if (!await Test(projectPackageInfoCollection.SolutionFile))
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    foreach (var projectPackageInfo in projectPackageInfoCollection.Packages.Where(x => x.IsTestProject))
                    {
                        if (!await Test(projectPackageInfo.ProjectFullPath))
                        {
                            return false;
                        }
                    }
                }
            }
        }

        return !HasErrors;
    }

    private async Task<bool> Build(string projectFile, bool isTestProject)
    {
        var properties = new Dictionary<string, object>();

        var context = string.Empty;
        if (_config.Coverage.Enable && _config.Test.Enable && isTestProject)
        {
            properties["DotNetReleaserCoverage"] = "true";
            properties["DotNetReleaserCoveragePackage"] = _config.Coverage.Package;
            properties["DotNetReleaserCoverageVersion"] = _config.Coverage.Version;
            properties["IsTestProject"] = "true";
            context = " tests with coverage";
        }
        
        Info($"Restoring{context} `{projectFile}`");
        var results = await RunMSBuild(projectFile, "Restore", properties);
        if (results is null) return false;

        if (_config.MSBuild.BuildDebug)
        {
            Info($"Building{context} `{projectFile}` - Configuration = {_config.MSBuild.ConfigurationDebug}");
            results = await RunMSBuild(projectFile, "Build", properties, buildDebug: true);
            if (results is null) return false;
        }

        Info($"Building{context} `{projectFile}` - Configuration = {_config.MSBuild.Configuration}");
        results = await RunMSBuild(projectFile, "Build", properties);
        if (results is null) return false;
        
        return true;
    }

    private async Task<bool> Test(string projectFile)
    {
        if (_config.MSBuild.BuildDebug && _config.Test.RunTestsForDebug)
        {
            Info($"Running Tests for `{projectFile}` - Configuration = {_config.MSBuild.ConfigurationDebug}");
            if (!await RunTest(projectFile, _config.MSBuild.ConfigurationDebug)) return false;
        }

        if (_config.Test.RunTests)
        {
            Info($"Running Tests for `{projectFile}` - Configuration = {_config.MSBuild.Configuration}");
            if (!await RunTest(projectFile, _config.MSBuild.Configuration)) return false;
        }

        return true;
    }

    private async Task<bool> RunTest(string projectFile, string configuration)
    {
        var runner = new DotNetRunner("test");
        runner.Arguments.Add("--no-restore"); // Because we ran it just before
        runner.Arguments.Add("--no-build"); // Because we ran it just before
        runner.Arguments.Add("--configuration");
        runner.Arguments.Add($"{configuration}");

        if (_config.Coverage.Enable)
        {
            runner.Arguments.Add("--results-directory");
            runner.Arguments.Add($"{_config.ArtifactsFolder}");
            runner.Arguments.Add("--collect");
            runner.Arguments.Add($"XPlat Code Coverage");
            // TBD doesn't work
            //runner.Arguments.Add("--test-adapter-path");
            //runner.Arguments.Add(@"C:\Users\alexa\.nuget\packages\coverlet.collector\3.1.2\build\netstandard1.0");

            runner.Arguments.Add("--");
            AddCoverageSetting(runner.Arguments, "Format", _config.Coverage.Format);
            AddCoverageSetting(runner.Arguments, "Include", _config.Coverage.Include);
            AddCoverageSetting(runner.Arguments, "IncludeDirectory", _config.Coverage.IncludeDirectory);
            AddCoverageSetting(runner.Arguments, "Exclude", _config.Coverage.Exclude);
            AddCoverageSetting(runner.Arguments, "ExcludeByFile", _config.Coverage.ExcludeByFile);
            AddCoverageSetting(runner.Arguments, "DeterministicReport", _config.Coverage.DeterministicReport);
            AddCoverageSetting(runner.Arguments, "SingleHit", _config.Coverage.SingleHit);
            AddCoverageSetting(runner.Arguments, "DoesNotReturnAttribute", _config.Coverage.DoesNotReturnAttribute);
            AddCoverageSetting(runner.Arguments, "IncludeTestAssembly", _config.Coverage.IncludeTestAssembly);
            AddCoverageSetting(runner.Arguments, "SkipAutoProps", _config.Coverage.SkipAutoProps);
            AddCoverageSetting(runner.Arguments, "SourceLink", _config.Coverage.SourceLink);
        }

        runner.Arguments.Add(projectFile);
        runner.LogStandardOutput = Info;
        runner.LogStandardError = Error;
        
        var result = await runner.Run();
        return !result.HasErrors;
    }

    const string CoveragePropertyPrefix = "DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.";

    private void AddCoverageSetting(List<string> arguments, string name, bool value)
    {
        arguments.Add($"{CoveragePropertyPrefix}{name}={(value?"true":"false")}");
    }
    private void AddCoverageSetting(List<string> arguments, string name, List<string> value)
    {
        if (value.Count > 0)
        {
            arguments.Add($"{CoveragePropertyPrefix}{name}={string.Join(",", value)}");
        }
    }
}