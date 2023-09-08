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

    string ApiToken { get; }

    string ApiTokenUsage { get; }
    
    Task<bool> Connect();

    Task<List<ReleaseVersion>> GetAllReleaseTags(string user, string repo, string tagPrefix);

    Task<ChangelogCollection?> GetChanges(string user, string repo, string tagPrefix, string version);

    Task CreateOrUpdateRelease(string user, string repo, ReleaseVersion version, ChangelogResult? changelog);

    Task UpdateChangelogAndUploadPackages(string user, string repo, ReleaseVersion version, ChangelogResult? changelog, List<AppPackageInfo> entries, bool enablePublishPackagesInDraft, bool forceUpload);

    Task UploadHomebrewFormula(string user, string repo, ProjectPackageInfo projectPackageInfo, string brewFormula);
    
    Task UploadScoopManifest(string user, string repo, ProjectPackageInfo projectPackageInfo, string scoopManifest);

    string GetCompareUrl(string user, string repo, string fromRef, string toRef);

    string GetDownloadReleaseUrl(string version, string fileEntry);

    Task<List<string>> GetBranchNamesForCommit(string user, string repo, string sha);
}