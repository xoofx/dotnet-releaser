using System;
using System.Collections.Generic;

namespace DotNetReleaser.Changelog;

public class ChangelogCollection
{
    public ChangelogCollection()
    {
        Version = ChangelogVersionModel.Empty;
        PreviousVersion = ChangelogVersionModel.Empty;
        CompareUrl = string.Empty;
        CommitChanges = new List<ChangelogCommitChangeModel>();
        PullRequestChanges = new List<ChangelogPullRequestChangeModel>();
    }

    public ChangelogVersionModel Version { get; set; }

    public ChangelogVersionModel PreviousVersion { get; set; }

    public string CompareUrl { get; set; }
    
    public List<ChangelogCommitChangeModel> CommitChanges { get; }
    
    public List<ChangelogPullRequestChangeModel> PullRequestChanges { get; }
    
    public void AddCommitChange(string title, string body, string author, string sha)
    {
        CommitChanges.Add(new ChangelogCommitChangeModel(title, body, author, sha));
    }

    public void AddPullRequestChange(int prNumber, string branch, string title, string body, string author, string[] labels, string[] files)
    {
        PullRequestChanges.Add(new ChangelogPullRequestChangeModel(prNumber, branch, title, body, author, labels, files));
    }
}