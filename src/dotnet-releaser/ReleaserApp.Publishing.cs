using System.Threading.Tasks;
using DotNetReleaser.Changelog;
using DotNetReleaser.Configuration;

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

            _logger.LogStartGroup($"Publishing Packages - {releaseVersion}");
            groupStarted = true;

            foreach (var (packageInfo, buildPackageInformation) in buildInformation.BuildPackages)
            {
                if (nugetApiToken is not null && buildInformation.PublishNuGet)
                {
                    await PublishNuGet(buildPackageInformation.NuGetPackages, nugetApiToken);
                }

                // Don't try to continue publishing if we had errors with NuGet publishing
                // Otherwise publish any packages that we have generated before
                if (!HasErrors && devHosting is not null && buildKind == BuildKind.Publish)
                {
                    var appPackagesToPublish = buildPackageInformation.AppPackages;

                    // In the case of a build, we still want to upload a draft release notes
                    await devHosting.UpdateChangelogAndUploadPackages(hostingConfiguration.User, hostingConfiguration.Repo, releaseVersion, changelog, appPackagesToPublish, _config.EnablePublishPackagesInDraft);

                    if (!HasErrors && _config.Brew.Publish)
                    {
                        // Log an error if we don't have an extra access for homebrew
                        devHostingExtra ??= devHosting;
                        var brewFormula = CreateFormula(devHostingExtra, packageInfo, appPackagesToPublish);

                        if (brewFormula is not null)
                        {
                            if (devHostingExtra == devHosting)
                            {
                                Warn("Warning, publishing a new Homebrew formula requires to use --github-token-extra. Using --github-token as a fallback but it might fail!");
                            }
                            await devHostingExtra.UploadHomebrewFormula(hostingConfiguration.User, _config.Brew.Home, packageInfo, brewFormula);
                        }
                    }
                    
                    if (!HasErrors && _config.Scoop.Publish)
                    {
                        // Log an error if we don't have an extra access for homebrew
                        devHostingExtra ??= devHosting;
                        var scoopManifest = CreateScoopManifest(devHostingExtra, packageInfo, appPackagesToPublish);

                        if (scoopManifest is not null)
                        {
                            if (devHostingExtra == devHosting)
                            {
                                Warn("Warning, publishing a new Scoop manifest requires to use --github-token-extra. Using --github-token as a fallback but it might fail!");
                            }
                            await devHostingExtra.UploadScoopManifest(hostingConfiguration.User, _config.Scoop.Home, packageInfo, scoopManifest);
                        }
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