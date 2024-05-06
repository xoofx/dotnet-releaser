using System.Threading.Tasks;
using DotNetReleaser.Configuration;
using DotNetReleaser.DevHosting;

namespace DotNetReleaser;

public partial class ReleaserApp 
{
    private async Task<IDevHosting?> ConnectToDevHosting(DevHostingConfiguration hostingConfiguration, string githubApiToken, string apiTokenUsage, string? githubApiTokenGist = null)
    {
        var hosting = new GitHubDevHosting(_logger, hostingConfiguration, githubApiToken, apiTokenUsage, githubApiTokenGist);

        if (await hosting.Connect())
        {
            return hosting;
        }

        return null;
    }
}