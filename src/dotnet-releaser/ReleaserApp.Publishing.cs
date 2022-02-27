using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetReleaser.Changelog;
using DotNetReleaser.Configuration;
using DotNetReleaser.Helpers;

namespace DotNetReleaser;

public partial class ReleaserApp
{
    private async Task PublishPackagesAndChangelog(string? nugetApiToken, BuildInformation buildInformation, GitHubDevHostingConfiguration hostingConfiguration, List<(ProjectPackageInfo, List<AppPackageInfo>)> buildPackages, IDevHosting? devHosting, IDevHosting? devHostingExtra, ChangelogResult? changelog)
    {
        bool groupStarted = false;
        try
        {
            var buildKind = buildInformation.BuildKind;
            var branchName = buildInformation.GitInformation?.BranchName;
            var releaseVersion = new ReleaseVersion(buildInformation.Version, IsDraft: buildKind == BuildKind.Build, $"{hostingConfiguration.VersionPrefix}{buildInformation.Version}", branchName is not null ? $"draft-{branchName}" : "draft");
            if (buildKind == BuildKind.Publish)
            {
                _logger.LogStartGroup($"Publishing Packages - {releaseVersion}");
                groupStarted = true;
                foreach (var (packageInfo, entriesToPublish) in buildPackages)
                {
                    if (nugetApiToken is not null)
                    {
                        await PublishNuGet(packageInfo, nugetApiToken);
                    }

                    // Don't try to continue publishing if we had errors with NuGet publishing
                    // Otherwise publish any packages that we have generated before
                    if (!HasErrors && devHosting is not null)
                    {
                        // In the case of a build, we still want to upload a draft release notes
                        await devHosting.UpdateChangelogAndUploadPackages(hostingConfiguration.User, hostingConfiguration.Repo, releaseVersion, changelog, entriesToPublish, _config.EnablePublishPackagesInDraft);

                        if (!HasErrors && _config.Brew.Publish)
                        {
                            UpdateHomebrewConfigurationFromPackage(packageInfo);

                            // Log an error if we don't have an extra access for homebrew
                            if (devHostingExtra is not null)
                            {
                                devHostingExtra = devHosting;
                                Warn("Warning, publishing a new Homebrew formula requires to use --github-token-extra. Using --github-token as a fallback but it might fail!");
                            }

                            var brewFormula = HomebrewHelper.CreateFormula(devHosting, packageInfo, entriesToPublish);

                            if (brewFormula is not null)
                            {
                                await devHosting.UploadHomebrewFormula(hostingConfiguration.User, _config.Brew.Home, packageInfo, brewFormula);
                            }
                        }
                    }
                }
            }
            else if (buildKind == BuildKind.Build)
            {
                if (devHosting is not null && !_config.Changelog.DisableDraftForBuild && buildInformation.AllowPublishDraft)
                {
                    _logger.LogStartGroup(_config.EnablePublishPackagesInDraft ? $"Publishing Draft Changelog and App Packages- {releaseVersion}" : $"Publishing Draft Changelog - {releaseVersion}");
                    groupStarted = true;
                    foreach (var (packageInfo, entriesToPublish) in buildPackages)
                    {
                        await devHosting.UpdateChangelogAndUploadPackages(hostingConfiguration.User, hostingConfiguration.Repo, releaseVersion, changelog, entriesToPublish, _config.EnablePublishPackagesInDraft);
                    }
                }
            }
        }
        finally
        {
            if (groupStarted)
            {
                _logger.LogEndGroup();
            }
        }
    }
}