namespace DotNetReleaser.Configuration;

/// <summary>
/// Configuration for GitHub.
/// </summary>
public class GitHubDevHostingConfiguration : DevHostingConfiguration
{
    public GitHubDevHostingConfiguration()
    {
        Base = "https://github.com";
        Api = "https://api.github.com";
    }

    internal override string Provider => "GitHub";
}