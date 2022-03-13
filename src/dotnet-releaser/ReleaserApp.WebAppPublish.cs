using System.Collections.Generic;
using System.Threading.Tasks;

namespace DotNetReleaser;

public partial class ReleaserApp
{
    private async Task<bool> BuildAndPublishWebApp(BuildInformation buildInformation)
    {
        if (!_config.WebApp.Publish || buildInformation.BuildKind != BuildKind.Publish) return true;

        var webApps = buildInformation.GetAllWebAppProjects();
        if (webApps.Count == 0) return true;
        
        try
        {
            _logger.LogStartGroup("WebApp Publishing");

            foreach (var webAppProject in buildInformation.GetAllWebAppProjects())
            {
                if (!await BuildAndPublishWebApp(webAppProject))
                {
                    return false;
                }
            }
        }
        finally
        {
            _logger.LogEndGroup();
        }

        return true;
    }

    private async Task<bool> BuildAndPublishWebApp(ProjectPackageInfo buildPackageInfo)
    {
        if (buildPackageInfo.WebAppPublishProfile is null)
        {
            Warn($"No PublishProfile for {buildPackageInfo.ProjectName}. Skipping.");
            return true;
        }

        Info($"Publishing {buildPackageInfo.ProjectName}");

        //buildPackageInfo
        var properties = new Dictionary<string, object>(_config.MSBuild.Properties);

        // We need to explicitly restore the platform RID before trying to build it
        var restoreResult = await RunMSBuild(buildPackageInfo.ProjectFullPath, "Restore", properties);
        if (restoreResult is null)
        {
            // Stop on first error
            return false;
        }

        properties["PublishProfile"] = buildPackageInfo.WebAppPublishProfile;

        // Publish
        var result = await RunMSBuild(buildPackageInfo.ProjectFullPath, "Publish", properties);

        if (result is null)
        {
            // Stop on first error
            return false;
        }

        return true;

    }
}