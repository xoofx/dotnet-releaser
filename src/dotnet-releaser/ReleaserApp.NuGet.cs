using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DotNetReleaser.Runners;

namespace DotNetReleaser;

public partial class ReleaserApp
{
    private async Task<bool> BuildNuGetPackage(BuildInformation buildInfo)
    {
        if (!_config.NuGet.Publish) return true;

        try
        {
            _logger.LogStartGroup($"NuGet Packaging - {buildInfo.Version}");
            foreach (var projectPackageInfo in buildInfo.GetAllPackableProjects())
            {
                var nugetPackages = await BuildNuGetPackageImpl(projectPackageInfo);
                if (nugetPackages is null)
                {
                    Error("Failed to build nuget packages or no packages were found.");
                }
                else
                {
                    var buildPackageInfo = buildInfo.GetOrCreateBuildPackageInformation(projectPackageInfo);
                    foreach (var nugetPackage in nugetPackages)
                    {
                        Info($"NuGet Package built: {nugetPackage}");
                        buildPackageInfo.NuGetPackages.Add(nugetPackage);
                    }
                }
            }
        }
        finally
        {
            _logger.LogEndGroup();
        }


        return !HasErrors;
    }

    private async Task<List<string>?> BuildNuGetPackageImpl(ProjectPackageInfo projectPackageInfo)
    {
        Info($"Building NuGet Package - {projectPackageInfo.Name}");
        var restoreResult = await RunMSBuild(projectPackageInfo.ProjectFullPath, "Restore");
        if (restoreResult is null)
        {
            return null;
        }

        // Pack executables as global tools
        var properties = new Dictionary<string, object>();
        if (projectPackageInfo.OutputType != PackageOutputType.Library)
        {
            properties["PackAsTool"] = "true";
        }

        // Set a link back to GitHub release notes
        properties["PackageReleaseNotes"] = _config.GitHub.GetReleaseNotesUrl(projectPackageInfo.Version);

        // We need to inject via props to support multi-targeting projects
        var outputs = await RunMSBuild(projectPackageInfo.ProjectFullPath, ReleaserConstants.DotNetReleaserPackAndGetNuGetPackOutput, properties, injectViaProps: true);
        if (outputs is null) return null;

        // Copy to artifacts
        var list = new List<string>();
        var files = outputs.Select(x => x.ItemSpec).ToArray();
        for (var i = 0; i < files.Length; i++)
        {
            var output = files[i];
            foreach (var nugetPackageExtension in new[] { ".nupkg", ".snupkg" })
            {
                if (output.EndsWith(nugetPackageExtension, StringComparison.OrdinalIgnoreCase))
                {
                    // Workaround for https://github.com/adamralph/minver/issues/675
                    if (!File.Exists(output))
                    {
                        var trailingVersion = $".1.0.0{nugetPackageExtension}";
                        if (output.EndsWith(trailingVersion, StringComparison.OrdinalIgnoreCase))
                        {
                            var expectedOutput = $"{output.Substring(0, output.Length - trailingVersion.Length)}.{projectPackageInfo.Version}{nugetPackageExtension}";
                            if (File.Exists(expectedOutput))
                            {
                                output = expectedOutput;
                            }
                        }
                    }

                    var dest = CopyToArtifacts(output);
                    list.Add(dest);
                }
            }
        }

        return list.Count == 0 ? null : list;
    }

    private async Task PublishNuGet(List<string> nugetPackages, string nugetSecretKey)
    {
        if (!_config.NuGet.Publish) return;

        foreach (var nugetPackage in nugetPackages)
        {
            var fileName = Path.GetFileName(nugetPackage);

            Info($"Publishing NuGet {fileName}");
            try
            {
                var program = new DotNetRunner("nuget")
                {
                    Arguments =
                    {
                        "push",
                        fileName,
                        $"-s", _config.NuGet.Source,
                        $"-k", nugetSecretKey,
                        "--skip-duplicate"
                    },
                    WorkingDirectory = _config.ArtifactsFolder
                };
                var result = await program.Run();
                if (result.HasErrors)
                {
                    Error(result.Output);
                }
                else
                {
                    Info(result.Output);
                }
            }
            catch (Exception ex)
            {
                var message = ex.Message.Replace(nugetSecretKey, "**********");
                Error($"Failing to push nuget package. Reason: {message}");
            }
        }
    }
}