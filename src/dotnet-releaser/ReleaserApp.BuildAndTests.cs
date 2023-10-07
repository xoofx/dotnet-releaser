using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DotNetReleaser.Coverage;
using DotNetReleaser.Logging;
using DotNetReleaser.Runners;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace DotNetReleaser;

public partial class ReleaserApp
{
    private List<AssemblyCoverage> _assemblyCoverages;

    private async Task<bool> BuildAndTest(BuildInformation buildInfo)
    {
        var coverage = _config.Coverage.Enable && _config.Test.Enable;

        // Build
        _logger.LogStartGroup("Building");
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
        _logger.LogEndGroup();

        // Test
        if (_config.Test.Enable)
        {
            bool groupLogged = false;
            foreach (var projectPackageInfoCollection in buildInfo.ProjectPackageInfoCollections)
            {
                foreach (var projectPackageInfo in projectPackageInfoCollection.Packages.Where(x => x.IsTestProject))
                {
                    if (!groupLogged)
                    {
                        _logger.LogStartGroup("Testing");
                        groupLogged = true;
                    }
                    if (!await Test(projectPackageInfo.ProjectFullPath))
                    {
                        return false;
                    }
                }
            }

            if (groupLogged)
            {
                if (_config.Coverage.Enable)
                {
                    LoadAndDisplayCoverageResults();
                }

                _logger.LogEndGroup();
            }
        }

        return !HasErrors;
    }

    private void LoadCoverageResults()
    {
        var coverageFolder = GetCoverageFolder();
        _assemblyCoverages = new List<AssemblyCoverage>();
        foreach (var file in Directory.EnumerateFiles(coverageFolder, "coverage.json", SearchOption.AllDirectories))
        {
            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read);
            
            var list  = CoverletJsonParser.Parse(stream);
            var merge = list.Count > 0;
            _assemblyCoverages.AddRange(list);
            if (merge)
            {
                _assemblyCoverages = AssemblyCoverage.Merge(_assemblyCoverages).ToList();
            }
        }
    }

    private static string FormatRate(HitCoverage rate)
    {
        return Math.Round(rate.Rate * 100, 2, MidpointRounding.AwayFromZero).ToString("##.00") + "%";
    }

    private void LoadAndDisplayCoverageResults()
    {
        LoadCoverageResults();

        var table = new Table();
        table.AddColumn(" ");
        table.AddColumn("Project");
        table.AddColumn("Line");
        table.AddColumn("Branch");
        table.AddColumn("Method");
        table.Border = _tableBorder;

        var rows = Enumerable.Repeat((object)string.Empty, table.Columns.Count).ToList();
        HitCoverage totalLineRate = default;
        HitCoverage totalBranchRate = default;
        HitCoverage totalMethodRate = default;
        foreach (var assemblyCoverage in _assemblyCoverages)
        {
            totalLineRate += assemblyCoverage.LineRate;
            totalBranchRate += assemblyCoverage.BranchRate;
            totalMethodRate += assemblyCoverage.MethodRate;
            var lineRate = FormatRate(assemblyCoverage.LineRate);
            var branchRate = FormatRate(assemblyCoverage.BranchRate);
            var methodRate = FormatRate(assemblyCoverage.MethodRate);
            rows[1] = assemblyCoverage.Name;
            rows[2] = lineRate;
            rows[3] = branchRate;
            rows[4] = methodRate;
            table.AddRow(rows.Select(x => x is IRenderable r ? r : new Text(x.ToString() ?? string.Empty)));
        }

        table.ShowFooters = true;
        table.Columns[0].Footer = new Text("Total");
        table.Columns[1].Footer = new Text(" ");
        table.Columns[2].Footer = new Text(FormatRate(totalLineRate));
        table.Columns[3].Footer = new Text(FormatRate(totalBranchRate));
        table.Columns[4].Footer = new Text(FormatRate(totalMethodRate));

        _logger.InfoMarkup("Coverage Results:", table);
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

    private string GetCoverageFolder()
    {
        return Path.Combine(_config.ArtifactsFolder, "coverage");
    }

    private string EnsureCoverageFolder()
    {
        var coverageFolder = GetCoverageFolder();
        if (!Directory.Exists(coverageFolder))
        {
            Directory.CreateDirectory(coverageFolder);
        }

        return coverageFolder;
    }

    private async Task<bool> RunTest(string projectFile, string configuration)
    {
        var runner = new DotNetRunner("test");
        runner.Arguments.Add("--no-restore"); // Because we ran it just before
        runner.Arguments.Add("--no-build"); // Because we ran it just before
        runner.Arguments.Add("--logger");
        runner.Arguments.Add("GitHubActions");
        runner.Arguments.Add("--configuration");
        runner.Arguments.Add($"{configuration}");
        runner.Arguments.Add(projectFile);

        if (_config.Coverage.Enable)
        {
            runner.Arguments.Add("--results-directory");
            var coverageFolder = EnsureCoverageFolder();
            runner.Arguments.Add(coverageFolder);
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