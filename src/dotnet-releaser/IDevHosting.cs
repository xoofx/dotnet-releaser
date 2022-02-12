using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetReleaser.Changelog;
using DotNetReleaser.Configuration;
using DotNetReleaser.Helpers;
using DotNetReleaser.Logging;

namespace DotNetReleaser;

/// <summary>
/// High level interface to interact with Development Hosting Platforms (GitHub...).
/// </summary>
/// <remarks>
/// The methods are meant to cover the high level use cases for dotnet-releaser,
/// not to cover all the fine grained APIs that these platforms are supporting (e.g Octokit)
/// </remarks>
public interface IDevHosting
{
    public ISimpleLogger Logger { get; }

    DevHostingConfiguration Configuration { get; }
    
    Task<bool> Connect();

    Task<ChangelogCollection?> GetChanges(string user, string repo, string versionPrefix, string version);

    Task UpdateChangelogAndUploadPackages(string user, string repo, ReleaseVersion version, ChangelogResult? changelog, List<PackageEntry> entries, bool enablePublishPackagesInDraft);

    Task UploadHomebrewFormula(string user, string repo, PackageInfo packageInfo, string brewFormula);

    string GetCompareUrl(string user, string repo, string fromRef, string toRef);

    string GetDownloadReleaseUrl(string version, string fileEntry);
}