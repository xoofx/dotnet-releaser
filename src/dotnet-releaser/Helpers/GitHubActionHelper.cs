using System;

namespace DotNetReleaser.Helpers;

public static class GitHubActionHelper
{
    public static GitHubActionInfo? GetInfo()
    {
        // https://docs.github.com/en/actions/learn-github-actions/environment-variables#default-environment-variables
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"))) return null;

        var ownerAndRepoName = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY")?.Split('/');
        if (ownerAndRepoName is null || ownerAndRepoName.Length != 2) return null;

        var owner = ownerAndRepoName[0];
        var repo = ownerAndRepoName[1];

        var eventName = Environment.GetEnvironmentVariable("GITHUB_EVENT_NAME");
        if (eventName is null) return null;

        var refName = Environment.GetEnvironmentVariable("GITHUB_REF_NAME");
        if (refName is null) return null;

        var refTypeStr = Environment.GetEnvironmentVariable("GITHUB_REF_TYPE");
        if (refTypeStr is null) return null;

        if (!Enum.TryParse<GitHubActionRefType>(refTypeStr, true, out var refType))
        {
            return null;
        }

        return new GitHubActionInfo(owner, repo, eventName, refName, refType);
    }
}


public record GitHubActionInfo(string OwnerName, string RepoName, string EventName, string RefName, GitHubActionRefType RefType);


public enum GitHubActionRefType
{
    Branch,
    Tag,
}