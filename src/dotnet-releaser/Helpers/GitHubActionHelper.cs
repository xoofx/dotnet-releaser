using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DotNetReleaser.Helpers;

public static class GitHubActionHelper
{
    public static readonly bool IsRunningOnGitHubAction = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
    
    public static GitHubActionInfo? GetInfo()
    {
        // https://docs.github.com/en/actions/learn-github-actions/environment-variables#default-environment-variables
        if (!IsRunningOnGitHubAction) return null;

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

        var eventJsonPath = Environment.GetEnvironmentVariable("GITHUB_EVENT_PATH");
        var eventJson = new Dictionary<string, object?>();
        if (File.Exists(eventJsonPath))
        {
            try
            {
                eventJson = JsonHelper.FromFile(eventJsonPath) as Dictionary<string, object?> ?? eventJson;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while reading GITHUB_EVENT_PATH {eventJsonPath}. Reason: {ex.Message}");
            }
        }

        return new GitHubActionInfo(owner, repo, eventName, refName, refType, eventJson);
    }
}


public record GitHubActionInfo(string OwnerName, string RepoName, string EventName, string RefName, GitHubActionRefType RefType, Dictionary<string, object?> Event)
{
    public override string ToString()
    {
        return $"user = {OwnerName}, repo = {RepoName}, event = {EventName}, ref_name = {RefName}, ref_type = {RefType.ToString().ToLowerInvariant()}";
    }
}


public enum GitHubActionRefType
{
    Branch,
    Tag,
}