namespace DotNetReleaser.Changelog;

public record ChangelogChangeModel(string Title, string Body, string Author);

public record ChangelogCommitChangeModel(string Title, string Body, string Author, string Sha) : ChangelogChangeModel(Title, Body, Author);