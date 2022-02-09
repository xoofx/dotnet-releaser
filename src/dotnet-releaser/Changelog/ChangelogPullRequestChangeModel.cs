namespace DotNetReleaser.Changelog;

public record ChangelogPullRequestChangeModel(int Number, string Branch, string Title, string Body, string Author, string[] Labels, string[] Files) : ChangelogChangeModel(Title, Body, Author);