using System;

namespace DotNetReleaser.Changelog;

public record ChangelogChangeModel(string Title, string Body, string Author);

public record ChangelogCommitChangeModel(string Title, string Body, string Author, string FullSha) : ChangelogChangeModel(Title, Body, Author)
{
    public string Sha => FullSha.Substring(0, Math.Min(FullSha.Length, 8));
}