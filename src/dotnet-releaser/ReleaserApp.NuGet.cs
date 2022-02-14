using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetReleaser.Runners;

namespace DotNetReleaser;

public partial class ReleaserApp
{
    private async Task<bool> BuildNuGetPackage(BuildInformation buildInfo)
    {
        if (!_config.NuGet.Publish) return true;

        foreach (var projectPackageInfo in buildInfo.GetAllPackableProjects())
        {
            var nugetPackages = await BuildNuGetPackageImpl(projectPackageInfo);
            if (nugetPackages is null)
            {
                Error("Failed to build nuget packages or no packages were found.");
            }
            else
            {
                foreach (var nugetPackage in nugetPackages)
                {
                    Info($"NuGet Package built: {nugetPackage}");
                }
            }
        }

        return !HasErrors;
    }

    private async Task<List<string>?> BuildNuGetPackageImpl(ProjectPackageInfo projectPackageInfo)
    {
        Info($"Building NuGet Package");
        var restoreResult = await RunMSBuild(projectPackageInfo.ProjectFullPath, "Restore");
        if (restoreResult is null)
        {
            return null;
        }

        var outputs = await RunMSBuild(projectPackageInfo.ProjectFullPath, ReleaserConstants.DotNetReleaserPackAndGetNuGetPackOutput);
        if (outputs is null) return null;

        // Copy to artifacts
        var list = new List<string>();
        foreach (var output in outputs.Where(x => x.ItemSpec.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase)).Select(x => x.ItemSpec))
        {
            var dest = CopyToArtifacts(output);
            list.Add(dest);
        }
        
        return list.Count == 0 ? null : list;
    }

    private async Task PublishNuGet(ProjectPackageInfo projectPackageInfo, string nugetSecretKey)
    {
        Info($"Publishing NuGet {projectPackageInfo.Version}");
        try
        {
            var program = new DotNetRunner("nuget")
            {
                Arguments =
                {
                    "push",
                    "*.nupkg",
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