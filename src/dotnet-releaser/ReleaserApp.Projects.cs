using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DotNetReleaser.Helpers;
using DotNetReleaser.Runners;
using Microsoft.Build.Construction;
using Microsoft.Build.Framework;
using NuGet.Frameworks;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace DotNetReleaser;

public enum PackageOutputType
{
    Exe,
    WinExe,
    AppContainerExe,
    Library,
}

public record BuildInformation(string Version, ProjectPackageInfoCollection[] ProjectPackageInfoCollections)
{
    public List<ProjectPackageInfo> GetAllPackableProjects()
    {
        return ProjectPackageInfoCollections.SelectMany(x => x.Packages).Where(x => x.IsPackable).ToList();
    }
}

public record TargetFrameworkInfo(bool IsMultiTargeting, List<string> TargetFrameworks);


public partial class ReleaserApp
{
 
    private async Task<TargetFrameworkInfo?> GetTargetFrameworks(string projectFullFilePath)
    {
        var outputs = await RunMSBuild(projectFullFilePath, ReleaserConstants.DotNetReleaserGetTargetFramework);
        if (outputs is null) return null;

        if (outputs.Count != 1)
        {
            Error($"Unexpected error. Unable to read build results for target `{ReleaserConstants.DotNetReleaserGetTargetFramework}`");
            return null;
        }

        bool isCrossBuilding = true;
        var targetFrameworksAsString = outputs[0].GetMetadata("TargetFrameworks")?.Trim();
        if (string.IsNullOrEmpty(targetFrameworksAsString))
        {
            targetFrameworksAsString = outputs[0].GetMetadata("TargetFramework") ?? string.Empty;
            isCrossBuilding = false;
        }

        var targetFrameworksAsArray = targetFrameworksAsString.Split(";", StringSplitOptions.RemoveEmptyEntries);
        if (targetFrameworksAsArray.Length == 0)
        {
            Error($"The project `{projectFullFilePath}` doesn't not have a <TargetFramework> or <TargetFrameworks> defined");
            return null;
        }

        var targetFrameworks = new List<string>();
        foreach (var targetFrameworkAsString in targetFrameworksAsArray)
        {
            try
            {
                var targetFramework = NuGetFramework.Parse(targetFrameworkAsString);
                targetFrameworks.Add(targetFrameworkAsString);
            }
            catch (Exception ex)
            {
                Error($"Error while parsing TargetFramework `{targetFrameworksAsString}`. Reason: {ex.Message}");
                return null;
            }
        }
        return new TargetFrameworkInfo(isCrossBuilding, targetFrameworks);
    }

    private async Task<ProjectPackageInfo?> LoadPackageInfo(string projectFullFilePath, TargetFrameworkInfo targetFrameworkInfo)
    {
        //Info($"Try load {projectFullFilePath}");
        var properties = new Dictionary<string, object>();
        if (targetFrameworkInfo.IsMultiTargeting)
        {
            // Take the last TargetFramework declared
            properties["TargetFramework"] = targetFrameworkInfo.TargetFrameworks[^1];
        }

        var outputs = await RunMSBuild(projectFullFilePath, ReleaserConstants.DotNetReleaserGetPackageInfo, properties);
        if (outputs is null) return null;

        if (outputs.Count == 0)
        {
            Error($"Unexpected error. Unable to read build results for target `{ReleaserConstants.DotNetReleaserGetPackageInfo}`");
            return null;
        }

        try
        {
            var packageId = outputs.First(x => x.GetMetadata(ReleaserConstants.ItemSpecKind) == ReleaserConstants.PackageId).ItemSpec!;
            var assemblyName = outputs.First(x => x.GetMetadata(ReleaserConstants.ItemSpecKind) == ReleaserConstants.AssemblyName).ItemSpec!;
            var packageVersion = outputs.First(x => x.GetMetadata(ReleaserConstants.ItemSpecKind) == ReleaserConstants.PackageVersion).ItemSpec;
            var packageDescription = outputs.FirstOrDefault(x => x.GetMetadata(ReleaserConstants.ItemSpecKind) == ReleaserConstants.PackageDescription)?.ItemSpec;
            var packageLicenseExpression = outputs.FirstOrDefault(x => x.GetMetadata(ReleaserConstants.ItemSpecKind) == ReleaserConstants.PackageLicenseExpression)?.ItemSpec;
            var packageOutputType = outputs.FirstOrDefault(x => x.GetMetadata(ReleaserConstants.ItemSpecKind) == ReleaserConstants.PackageOutputType)?.ItemSpec?.Trim() ?? string.Empty;
            var packageProjectUrl = outputs.FirstOrDefault(x => x.GetMetadata(ReleaserConstants.ItemSpecKind) == ReleaserConstants.PackageProjectUrl)?.ItemSpec ?? $"{_config.GitHub.GetUrl()}";
            var isNuGetPackable = string.Equals(outputs.FirstOrDefault(x => x.GetMetadata(ReleaserConstants.ItemSpecKind) == ReleaserConstants.IsNuGetPackable)?.ItemSpec?.Trim(), "true", StringComparison.OrdinalIgnoreCase);
            var isTestProject = string.Equals(outputs.FirstOrDefault(x => x.GetMetadata(ReleaserConstants.ItemSpecKind) == ReleaserConstants.IsTestProject)?.ItemSpec?.Trim(), "true", StringComparison.OrdinalIgnoreCase);

            var builder = new StringBuilder();
            var currentProjectDirectory = Path.GetDirectoryName(projectFullFilePath)!;
            var projectReferences = new List<string>();
            foreach (var projectReference in outputs.Where(x => x.GetMetadata(ReleaserConstants.ItemSpecKind) == ReleaserConstants.ProjectReference))
            {
                var projectReferenceFullPath =
                    Path.GetFullPath(Path.Combine(currentProjectDirectory, projectReference.ItemSpec));
                projectReferences.Add(projectReferenceFullPath);
            }

            if (!Enum.TryParse<PackageOutputType>(packageOutputType, true, out var result))
            {
                Error($"Unsupported project type `{packageOutputType}` found for project `{projectFullFilePath}`");
                return null;
            }

            return new ProjectPackageInfo(projectFullFilePath, packageId, assemblyName, result, packageVersion, packageDescription ?? "No description found", packageLicenseExpression ?? "No license found", packageProjectUrl, isNuGetPackable, isTestProject, projectReferences.ToArray(), targetFrameworkInfo);
        }
        catch (Exception ex)
        {
            Error($"Unexpected error while trying to read build results for target `{ReleaserConstants.DotNetReleaserGetPackageInfo}`. Outputs: {string.Join(", ", outputs.Select(x => x.ItemSpec))}. Reason: {ex}");
            return null;
        }
    }

    private async Task<BuildInformation?> LoadProjects()
    {
        var pathComparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var allProjectPaths = new HashSet<string>(pathComparer);
        var solutionToProjects = new Dictionary<string, List<string>>(pathComparer);
        
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
                    if (!solutionToProjects.TryGetValue(string.Empty, out var listOfProjectsPerSolution))
                    {
                        listOfProjectsPerSolution = new List<string>();
                        solutionToProjects[string.Empty] = listOfProjectsPerSolution;
                    }
                    listOfProjectsPerSolution.Add(msBuildProject);
                }
                else
                {
                    Error($"The project `{msBuildProject}` is duplicated in the list of input projects.");
                }
            }
        }

        if (HasErrors) return null;

        var allProjectPackageInfoCollections = new List<ProjectPackageInfoCollection>();

        Info($"Loading {solutionToProjects.Select(x => x.Value.Count).Sum()} projects");

        // Load TargetFrameworks
        var projectToTargetFrameworkInfo = new Dictionary<string, TargetFrameworkInfo>(pathComparer);
        var tasks = new List<Task>();
        foreach (var (solution, projects) in solutionToProjects)
        {
            foreach (var project in projects)
            {
                var task = Task.Run(async () =>
                {
                    var targetFrameworkInfo = await GetTargetFrameworks(project);
                    if (targetFrameworkInfo is not null)
                    {
                        lock (projectToTargetFrameworkInfo)
                        {
                            projectToTargetFrameworkInfo.Add(project, targetFrameworkInfo);
                        }
                    }
                }); 
                tasks.Add(task);
            }
        }
        await Task.WhenAll(tasks);

        if (HasErrors) return null;


        var results = new ConcurrentQueue<(string, ProjectPackageInfo?)>();
        // Load projects from solutions
        tasks.Clear();
        foreach (var (solution, projects) in solutionToProjects)
        {
            foreach (var project in projects)
            {
                Debug.Assert(projectToTargetFrameworkInfo.ContainsKey(project));
                var task = Task.Run(async () =>
                {
                    var result = (solution, await LoadPackageInfo(project, projectToTargetFrameworkInfo[project]));
                    results.Enqueue(result);
                });
                tasks.Add(task);
            }
        }

        await Task.WhenAll(tasks);

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

        // --------------------------------------------------------------------------
        // Sort by dependencies (topological order) - only for projects in a solution
        // --------------------------------------------------------------------------
        allProjectPackageInfoCollections.AddRange(solutionToProjectPackageInfoCollections.Select(x => new ProjectPackageInfoCollection(x.Value.OrderBy(x => x.ProjectFullPath).ToArray(), x.Key)));
        for (var index = 0; index < allProjectPackageInfoCollections.Count; index++)
        {
            var projectPackageInfoCollection = allProjectPackageInfoCollections[index];
            var packages = projectPackageInfoCollection.Packages.ToList();
            var processed = new HashSet<string>(pathComparer);

            var orderedList = new List<ProjectPackageInfo>();

            while (packages.Count > 0)
            {
                var packageReady = packages.FirstOrDefault(x => x.ProjectReferences.Where(refPath1 => allProjectPaths.Contains(refPath1)).All(referencePath => processed.Contains(referencePath)));

                if (packageReady is null)
                {
                    Error($"Cannot resolve dependencies of remaining projects [{string.Join(", ", packages.Select(x => x.AssemblyName))}]. Aborting.");
                    return null;
                }

                // Add the package has being processed
                processed.Add(packageReady.ProjectFullPath);
                orderedList.Add(packageReady);
                packages.Remove(packageReady);
            }

            allProjectPackageInfoCollections[index] = new ProjectPackageInfoCollection(orderedList.ToArray(), projectPackageInfoCollection.SolutionFile);
        }
        
        // --------------------------------------------------------------------------
        // Verify versions of projects and display all projects
        // --------------------------------------------------------------------------
        var version = VerifyVersionsAndDisplayAllProjects(allProjectPackageInfoCollections);

        return new BuildInformation(version, allProjectPackageInfoCollections.ToArray());
    }

    private string VerifyVersionsAndDisplayAllProjects(List<ProjectPackageInfoCollection> projectPackageInfoCollections)
    {
        var table = new Table();
        table.AddColumn("Project");
        table.AddColumn("Kind");
        table.AddColumn("Version");
        table.AddColumn("License");
        table.AddColumn(new TableColumn("Packable?").Centered());
        table.AddColumn(new TableColumn("Test?").Centered());

        var row = new List<object>();
        row.AddRange(Enumerable.Repeat(string.Empty, table.Columns.Count));
        
        string? version = null;
        var invalidPackageVersions = new List<ProjectPackageInfo>();
        string? previousSolution = null;
        foreach (var projectPackageInfoCollection in projectPackageInfoCollections)
        {
            if (!string.IsNullOrEmpty(projectPackageInfoCollection.SolutionFile) || previousSolution != projectPackageInfoCollection.SolutionFile)
            {
                if (projectPackageInfoCollection.SolutionFile is null)
                {
                    // We don't need to add a separator if we only have projects without solutions
                    if (projectPackageInfoCollections.Any(x => x.SolutionFile is not null))
                    {
                        table.AddRow(new Text("Direct Projects"));
                    }
                }
                else
                {
                    var subTable = new Table();
                    subTable.AddColumn(projectPackageInfoCollection.SolutionFile);
                    table.AddRow(subTable);
                }
            }
            previousSolution = projectPackageInfoCollection.SolutionFile;

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
                if (project.IsPackable)
                {
                    row[3] = LicenseHelper.IsKnownLicense(project.License) ? new Text(project.License, new Style(Color.Green, Color.Black)) :
                        LicenseHelper.IsLicenseDefined(project.License) ? new Text(project.License, new Style(Color.Black, Color.Red)) :
                        new Text(project.License, new Style(Color.Yellow, Color.Black));
                }
                else
                {
                    row[3] = project.License;
                }

                row[4] = project.IsPackable ? "x" : string.Empty;
                row[5] = project.IsTestProject ? "x" : string.Empty;
                if (invalidVersion)
                {
                    invalidPackageVersions.Add(project);
                }

                //table.AddRow(row.Select(Markup.Escape).ToArray());
                table.AddRow(row.Select(x => x is IRenderable renderable ? renderable : new Text(x.ToString() ?? string.Empty)).ToArray());
            }
        }

        Info($"Packages and Projects", table);

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

    private async Task<List<ITaskItem>?> RunMSBuild(string project, string target, IDictionary<string, object>? properties = null, bool buildDebug = false)
    {
        using var program = new MSBuildRunner()
        {
            Project = project,
            Configuration = buildDebug ? _config.MSBuild.Configuration : _config.MSBuild.ConfigurationDebug,
            CustomBeforeMicrosoftCommonProps = DotNetReleaserConfigFile,
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

        var result = await program.Run(_logger);

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