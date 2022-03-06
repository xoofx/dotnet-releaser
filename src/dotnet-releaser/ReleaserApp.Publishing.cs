using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetReleaser.Changelog;
using DotNetReleaser.Configuration;
using DotNetReleaser.Helpers;

namespace DotNetReleaser;

public partial class ReleaserApp
{
    private async Task PublishPackagesAndChangelog(string? nugetApiToken, BuildInformation buildInformation, GitHubDevHostingConfiguration hostingConfiguration, IDevHosting? devHosting, IDevHosting? devHostingExtra, ChangelogResult? changelog)
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
                foreach (var (packageInfo, buildPackageInformation) in buildInformation.BuildPackages)
                {
                    if (nugetApiToken is not null)
                    {
                        await PublishNuGet(buildPackageInformation.NuGetPackages, nugetApiToken);
                    }

                    // Don't try to continue publishing if we had errors with NuGet publishing
                    // Otherwise publish any packages that we have generated before
                    if (!HasErrors && devHosting is not null)
                    {
                        var appPackagesToPublish = buildPackageInformation.AppPackages;

                        // In the case of a build, we still want to upload a draft release notes
                        await devHosting.UpdateChangelogAndUploadPackages(hostingConfiguration.User, hostingConfiguration.Repo, releaseVersion, changelog, appPackagesToPublish, _config.EnablePublishPackagesInDraft);

                        if (!HasErrors && _config.Brew.Publish)
                        {
                            UpdateHomebrewConfigurationFromPackage(packageInfo);

                            // Log an error if we don't have an extra access for homebrew
                            if (devHostingExtra is null)
                            {
                                devHostingExtra = devHosting;
                                Warn("Warning, publishing a new Homebrew formula requires to use --github-token-extra. Using --github-token as a fallback but it might fail!");
                            }

                            var brewFormula = HomebrewHelper.CreateFormula(devHostingExtra, packageInfo, appPackagesToPublish);

                            if (brewFormula is not null)
                            {
                                await devHostingExtra.UploadHomebrewFormula(hostingConfiguration.User, _config.Brew.Home, packageInfo, brewFormula);
                            }
                        }
                    }
                }
            }

            // Disable publishing draft release for now as we can't list draft release anymore

            //else if (buildKind == BuildKind.Build)
            //{
            //    if (devHosting is not null && !_config.Changelog.DisableDraftForBuild && buildInformation.AllowPublishDraft)
            //    {
            //        _logger.LogStartGroup(_config.EnablePublishPackagesInDraft ? $"Publishing Draft Changelog and App Packages - {releaseVersion}" : $"Publishing Draft Changelog - {releaseVersion}");
            //        groupStarted = true;
            //        foreach (var (packageInfo, buildPackageInformation) in buildInformation.BuildPackages)
            //        {
            //            var appPackagesToPublish = buildPackageInformation.AppPackages;
            //            await devHosting.UpdateChangelogAndUploadPackages(hostingConfiguration.User, hostingConfiguration.Repo, releaseVersion, changelog, appPackagesToPublish, _config.EnablePublishPackagesInDraft);
            //        }
            //    }
            //}
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