using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DotNetReleaser.Changelog;
using DotNetReleaser.Configuration;
using DotNetReleaser.DevHosting;
using DotNetReleaser.Logging;

namespace DotNetReleaser;

public partial class ReleaserApp
{
    private async Task<ChangelogResult?> CreateChangeLog(IDevHosting hosting, string version)
    {
        var builder = new ChangelogBuilder(_config.Changelog, _logger);
        var result = await builder.Generate(hosting, version);
        return result;
    }

    private async Task<bool> ListOrUpdateChangelog(string configurationFilePath, string githubApiToken, string version, bool update)
    {
        if (!await LoadConfiguration(configurationFilePath))
        {
            return false;
        }
        
        var devHosting = await ConnectToDevHosting(_config.GitHub, githubApiToken);
        if (devHosting is null) return false;

        return await ListOrUpdateChangelog(devHosting, version, update);
    }

    private async Task<bool> ListOrUpdateChangelog(IDevHosting devHosting, string version, bool update)
    {
        // TODO: allow to use settings from existing file
        var user = devHosting.Configuration.User;
        var repo = devHosting.Configuration.Repo;
        var tagPrefix = devHosting.Configuration.VersionPrefix;

        var changelogConfiguration = _config.Changelog;

        bool hasErrors = false;
        Info($"Updating changelog for for repository `{user}/{repo}`. Fetching tags.");
        var versions = await devHosting.GetAllReleaseTags(user, repo, tagPrefix);

        // If we specify a version, filter only this one
        if (!string.IsNullOrEmpty(version))
        {
            var tagsFound = versions.Count;
            versions = versions.Where(x => x.Version == version).ToList();
            if (versions.Count != 1)
            {
                Error($"Cannot find the version {version} from the existing list of tags ({tagsFound} tags found).");
                return false;
            }
        }

        Info($"Updating changelog for for repository `{user}/{repo}`. {versions.Count} tags found.");
        foreach (var releaseVersion in versions)
        {
            Info($"Building changelog for repository `{user}/{repo}` for version {releaseVersion.Version}");
            var builder = new ChangelogBuilder(changelogConfiguration, _logger);
            var changelogResult = await builder.Generate(devHosting, releaseVersion.Version);
            if (changelogResult is not null)
            {
                _logger.Info($"Title: {changelogResult.Title}");
                _logger.Info(changelogResult.Body);
                if (update)
                {
                    Info($"Updating changelog for repository `{user}/{repo}` for version {releaseVersion.Version}");
                    await devHosting.CreateOrUpdateRelease(user, repo, releaseVersion, changelogResult);
                }
            }
            else
            {
                hasErrors = true;
            }
        }

        return !hasErrors;
    }
}