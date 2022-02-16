using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using System.Threading.Tasks;
using DotNetReleaser.Helpers;
using DotNetReleaser.Runners;
using Microsoft.Build.Construction;
using Microsoft.Build.Framework;

namespace DotNetReleaser;


public record BuildInformation(string Version, ProjectPackageInfoCollection[] ProjectPackageInfoCollections)
{
    public List<ProjectPackageInfo> GetAllPackableProjects()
    {
        return ProjectPackageInfoCollections.SelectMany(x => x.Packages).Where(x => x.IsPackable).ToList();
    }
}

public partial class ReleaserApp 
{
    private async Task<BuildInformation> LoadProjects()
    {
        var pathComparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var allProjectPaths = new HashSet<string>(pathComparer);
        var solutionToProjects = new Dictionary<string, List<string>>(pathComparer);
        var directProjects = new List<string>();
        
        foreach (var msBuildProject in _config.MSBuild.Projects)
        {
            if (msBuildProject.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
            {
                // solution file
                try
                {
                    var solutionFile = SolutionFile.Parse(msBuildProject);
                    foreach (var subProject in solutionFile.ProjectsInOrder)
                    {
                        if (subProject.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat)
                        {
                            var fullProjectPath = Path.GetFullPath(subProject.AbsolutePath);

                            if (allProjectPaths.Add(fullProjectPath))
                            {
                                if (!solutionToProjects.TryGetValue(msBuildProject, out var listOfProjectsPerSolution))
                                {
                                    listOfProjectsPerSolution = new List<string>();
                                    solutionToProjects[msBuildProject] = listOfProjectsPerSolution;
                                }
                                listOfProjectsPerSolution.Add(fullProjectPath);
                            }
                            else
                            {
                                Error($"The project `{fullProjectPath}` is duplicated in the list of input projects.");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Error($"Error while parsing solution {msBuildProject}. Reason: {ex.Message}");
                }
            }
            else
            {
                if (allProjectPaths.Add(msBuildProject))
                {
                    directProjects.Add(msBuildProject);
                }
                else
                {
                    Error($"The project `{msBuildProject}` is duplicated in the list of input projects.");
                }
            }
        }

        var allProjectPackageInfoCollections = new List<ProjectPackageInfoCollection>();
        
        // Load direct projects
        var tempList = new List<ProjectPackageInfo>();
        foreach (var project in directProjects)
        {
            var packageInfo = await LoadPackageInfo(project);
            if (packageInfo is not null)
            {
                tempList.Add(packageInfo);
            }
        }

        if (tempList.Count > 0)
        {
            allProjectPackageInfoCollections.Add(new ProjectPackageInfoCollection(tempList.ToArray(), null));
        }

        Info("Loading projects");

        Environment.SetEnvironmentVariable("DOTNET_CLI_TELEMETRY_OPTOUT", "1");

        var results = new ConcurrentQueue<(string, ProjectPackageInfo?)>();
        var tasks = new List<Task>();
        // Load projects from solutions
        foreach (var (solution, projects) in solutionToProjects)
        {
            foreach (var project in projects)
            {
                var task = Task.Factory.StartNew(async () =>
                {
                    var result = (solution, await LoadPackageInfo(project));
                    results.Enqueue(result);
                });
                tasks.Add(task);
            }
        }

        Task.WaitAll(tasks.ToArray());

        // Collect results
        var solutionToProjectPackageInfoCollections = new Dictionary<string, List<ProjectPackageInfo>>();
        foreach (var result in results)
        {
            var (solution, packageInfo) = result;
            if (packageInfo is not null)
            {
                if (!solutionToProjectPackageInfoCollections.TryGetValue(solution, out var list))
                {
                    list = new List<ProjectPackageInfo>();
                    solutionToProjectPackageInfoCollections.Add(solution, list);
                }
                list.Add(packageInfo);
            }
        }
        allProjectPackageInfoCollections.AddRange(solutionToProjectPackageInfoCollections.Select(x => new ProjectPackageInfoCollection(x.Value.ToArray(), x.Key)));

        // Verify versions of projects and display all projects
        var version = VerifyVersionsAndDisplayAllProjects(allProjectPackageInfoCollections);

        return new BuildInformation(version, allProjectPackageInfoCollections.ToArray());
    }

    private (string, ProjectPackageInfo?) Function()
    {
        throw new NotImplementedException();
    }

    private string VerifyVersionsAndDisplayAllProjects(List<ProjectPackageInfoCollection> projectPackageInfoCollections)
    {
        var tableRenderer = new TableTextRenderer();
        tableRenderer.AddColumnHeader("Project");
        tableRenderer.AddColumnHeader("Kind");
        tableRenderer.AddColumnHeader("Version");
        tableRenderer.AddColumnHeader("License");
        tableRenderer.AddColumnHeader("IsPackable", TextAlignKind.Center);
        tableRenderer.AddColumnHeader("IsTest", TextAlignKind.Center);
        tableRenderer.AddColumnHeader("Solution");

        var row = new List<string>();
        row.AddRange(Enumerable.Repeat(string.Empty, tableRenderer.ColumnHeaders.Count));

        string? version = null;
        var invalidPackageVersions = new List<ProjectPackageInfo>();
        foreach (var projectPackageInfoCollection in projectPackageInfoCollections)
        {
            foreach (var project in projectPackageInfoCollection.Packages)
            {
                if (project.IsPackable)
                {
                    version ??= project.Version;
                }
                bool invalidVersion = project.IsPackable && version != project.Version;
                row[0] = project.AssemblyName;
                row[1] = project.OutputType.ToString().ToLowerInvariant();
                row[2] = invalidVersion ? $"{project.Version} (invalid)" : project.Version;
                row[3] = project.License;
                row[4] = project.IsPackable ? "x" : string.Empty;
                row[5] = project.IsTestProject ? "x" : string.Empty;
                row[6] = projectPackageInfoCollection.SolutionFile ?? string.Empty;
                if (invalidVersion)
                {
                    invalidPackageVersions.Add(project);
                }

                tableRenderer.AddRow(row);
            }
        }

        Info($"Packages and Projects\n{tableRenderer.Render()}");
        if (invalidPackageVersions.Count > 0)
        {
            foreach (var invalidPackageVersion in invalidPackageVersions)
            {
                Error($"Invalid version {invalidPackageVersion.Version} for package {invalidPackageVersion.AssemblyName}");
            }
        }

        if (string.IsNullOrEmpty(version))
        {
            Error("No version found from all projects");
        }

        return version ?? string.Empty;
    }

    private async Task<List<ITaskItem>?> RunMSBuild(string project, string target, IDictionary<string, object>? properties = null)
    {
        using var program = new MSBuildRunner()
        {
            Project = project,
            Configuration = _config.MSBuild.Configuration,
            CustomAfterMicrosoftCommonTargets = DotNetReleaserConfigFile,
            Targets =
            {
                target
            }
        };

        // Copy properties
        if (properties is not null)
        {
            foreach (var property in properties)
            {
                program.Properties[property.Key] = property.Value;
            }
        }
        
        var result = await program.Run(this);

        if (result.TargetOutputs.TryGetValue(target, out var outputs))
        {
            return outputs;
        }
        else if (!result.HasErrors)
        {
            return new List<ITaskItem>();
        }

        return null;
    }
}