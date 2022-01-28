using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Octokit;

namespace DotNetReleaser;

public partial class ReleaserApp 
{
    private async Task<GitHubClient?> ConnectGitHubClient()
    {
        var client = new GitHubClient(new ProductHeaderValue(nameof(ReleaserApp)));

        var tokenAuth = new Credentials(_githubApiToken); // NOTE: not real token
        client.Credentials = tokenAuth;

        Info("Connecting to GitHub");
        try
        {
            _ = await client.User.Current();
        }
        catch (Exception ex)
        {
            Error($"Unable to connect GitHub. Reason: {ex.Message}");
            return null;
        }

        return client;
    }

    private async Task UpdateGitHub(GitHubClient github, PackageInfo packageInfo, string? changelog, List<PackageEntry> entries)
    {
        var packageVersion = packageInfo.Version;
        var userGitHub = _config.GitHub.User;
        var repoGitHub = _config.GitHub.Repo;

        var releases = await github.Repository.Release.GetAll(_config.GitHub.User, _config.GitHub.Repo);
        
        var versionOnGitHub = $"{_config.GitHub.VersionPrefix}{packageVersion}";

        Release? release = null;
        if (releases is not null)
        {
            release = releases.FirstOrDefault(releaseCheck => releaseCheck.TagName == versionOnGitHub);
        }

        // Create a release
        release ??= await github.Repository.Release.Create(userGitHub, repoGitHub, new NewRelease(versionOnGitHub));
        
        Info($"Loading release tag {release.TagName}");

        ReleaseUpdate? releaseUpdate = null;
        if (changelog is not null && release.Body != changelog)
        {
            releaseUpdate = release.ToUpdate();
            releaseUpdate.Body = changelog;
        }

        // Update the body if necessary
        if (releaseUpdate != null)
        {
            Info($"Updating release {release.TagName} with new changelog");
            release = await github.Repository.Release.Edit(userGitHub, repoGitHub, release.Id, releaseUpdate);
        }

        var assets = await github.Repository.Release.GetAllAssets(userGitHub, repoGitHub, release.Id, ApiOptions.None);

        foreach (var entry in entries)
        {
            if (!entry.Publish)
            {
                Info($"Skipping {entry.Path} as publishing is disabled.");
                break;
            }

            var filename = Path.GetFileName(entry.Path);
            if (assets.Any(x => x.Name == filename))
            {
                Info($"No need to update {entry.Path} on GitHub. Already uploaded.");
                continue;
            }

            const int maxHttpRetry = 10;
            for (int i = 0; i < maxHttpRetry; i++)
            {
                try
                {
                    Info($"{(i > 0 ? $"Retry ({{i}}/{maxHttpRetry - 1}) " : "")}Uploading {filename} to GitHub Release: {versionOnGitHub} (Size: {new FileInfo(entry.Path).Length / (1024 * 1024)}MB)");
                    // Upload assets
                    using var stream = File.OpenRead(entry.Path);

                    await github.Repository.Release.UploadAsset(release, new ReleaseAssetUpload(filename, entry.Mime, stream, new TimeSpan(0, 5, 0)));
                    break;
                }
                catch (Exception ex)
                {
                    // In case of a failure to upload try to delete the asset
                    try
                    {
                        assets = await github.Repository.Release.GetAllAssets(userGitHub, repoGitHub, release.Id, ApiOptions.None);
                        var assetToDelete = assets.FirstOrDefault(x => x.Name == filename);
                        await github.Repository.Release.DeleteAsset(userGitHub, repoGitHub, assetToDelete.Id);

                        // Refresh the list of the assets
                        assets = await github.Repository.Release.GetAllAssets(userGitHub, repoGitHub, release.Id, ApiOptions.None);
                    }
                    catch
                    {
                        Error($"Failure to delete the remote asset: {filename}");
                        // ignored
                    }

                    if (i + 1 == maxHttpRetry)
                    {
                        Error($"Upload failed: {ex} after {maxHttpRetry} retries.");
                    }
                    else
                    {
                        const int millisecondsDelay = 100;
                        Warn($"Upload failed: {ex.Message}. Retrying after {millisecondsDelay}ms...");
                        await Task.Delay(millisecondsDelay);
                    }
                }
            }
        }
    }
}