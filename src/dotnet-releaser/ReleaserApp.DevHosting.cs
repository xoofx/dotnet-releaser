using System.Threading.Tasks;
using DotNetReleaser.Configuration;
using DotNetReleaser.DevHosting;

namespace DotNetReleaser;

public partial class ReleaserApp 
{
    private async Task<IDevHosting?> ConnectToDevHosting(DevHostingConfiguration hostingConfiguration, string githubApiToken)
    {
        var hosting = new GitHubDevHosting(_logger, hostingConfiguration, githubApiToken);

        if (await hosting.Connect())
        {
            return hosting;
        }

        return null;
    }
}