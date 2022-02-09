using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DotNetReleaser.Changelog;

namespace DotNetReleaser;

public partial class ReleaserApp
{
    private async Task<ChangelogResult?> CreateChangeLog(IDevHosting hosting, string version)
    {
        var builder = new ChangelogBuilder(_config.Changelog, _logger);
        var result = await builder.Generate(hosting, version);
        return result;
    }
}