using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

        // Load projects from solutions
        foreach (var (solution, projects) in solutionToProjects)
        {
            tempList.Clear();
            foreach (var project in projects)
            {
                var packageInfo = await LoadPackageInfo(project);
                if (packageInfo is not null)
                {
                    tempList.Add(packageInfo);
                }
            }

            if (tempList.Count > 0)
            {
                allProjectPackageInfoCollections.Add(new ProjectPackageInfoCollection(tempList.ToArray(), solution));
            }
        }

        // Verify versions of projects and display all projects
        var version = VerifyVersionsAndDisplayAllProjects(allProjectPackageInfoCollections);

        return new BuildInformation(version, allProjectPackageInfoCollections.ToArray());
    }


    private string VerifyVersionsAndDisplayAllProjects(List<ProjectPackageInfoCollection> projectPackageInfoCollections)
    {
        string? version = null;
        foreach (var projectPackageInfoCollection in projectPackageInfoCollections)
        {
            string indent = string.Empty;
            if (projectPackageInfoCollection.SolutionFile is not null)
            {
                Info($"Projects from Solution {projectPackageInfoCollection.SolutionFile}");
                indent = "-> ";
            }
            foreach (var project in projectPackageInfoCollection.Packages)
            {
                if (project.IsPackable)
                {
                    version ??= project.Version;

                    if (version != project.Version)
                    {
                        Error($"{indent}Project Package: {project.AssemblyName} with the Version {project.Version} is not matching the version of other previous projects {version}. All versions must match in order to publish under the same version!");
                    }
                    else
                    {
                        Info($"{indent}Project Package: {project.AssemblyName}, Version: {project.Version}, Kind: {project.OutputType}");
                    }
                }
                else if (project.IsTestProject)
                {
                    Info($"{indent}Test Project {project.AssemblyName} (build & test only,no packaging or publish)");
                }
                else
                {
                    Info($"{indent}Project: {project.AssemblyName}, Version: {project.Version}, Kind: {project.OutputType} (build only, no packaging or publish)");
                }
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