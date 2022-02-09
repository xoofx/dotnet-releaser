using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DotNetReleaser.Configuration;
using DotNetReleaser.Logging;

namespace DotNetReleaser.Changelog;

public class ChangelogBuilder
{
    private readonly ISimpleLogger _log;
    private readonly ChangelogConfiguration _config;
    
    public ChangelogBuilder(ChangelogConfiguration config, ISimpleLogger log)
    {
        _log = log;
        _config = config;
    }
    
    public async Task<ChangelogResult?> Generate(IDevHosting devHosting, string version)
    {
        if (!string.IsNullOrEmpty(_config.Path))
        {
            return await GenerateFromExistingChangelog(devHosting, version);
        }

        if (_config.Auto)
        {

            return await GenerateFromPullRequestsAndCommits(devHosting, version);
        }

        return null;
    }
    

    private async Task<ChangelogResult?> GenerateFromPullRequestsAndCommits(IDevHosting devHosting, string version)
    {
        var changelogCollection = await devHosting.GetChanges(devHosting.Configuration.User, devHosting.Configuration.Repo, devHosting.Configuration.VersionPrefix, version);
        if (changelogCollection is null) return null;
        var templatizer = new ChangelogTemplatizer(_config, _log);
        return templatizer.Generate(changelogCollection);
    }

    private async Task<ChangelogResult?> GenerateFromExistingChangelog(IDevHosting devHosting, string version)
    {
        var lines = await File.ReadAllLinesAsync(_config.Path!);

        var matcher = new Regex(_config.Version);

        var builder = new StringBuilder();

        bool versionFound = false;
        foreach (var line in lines)
        {
            var match = matcher.Match(line);
            if (match.Success && match.Groups.Count > 1 && match.Groups[1].Value == version)
            {
                versionFound = true;
            }
            else if (versionFound)
            {
                // Stop on the next changelog entry
                if (match.Success) break;
                // Otherwise append the line to the log
                var text = line.Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    builder.AppendLine(text);
                }
            }
        }

        if (!versionFound)
        {
            _log.Error($"Unable to find version {version} from changelog.md");
            return null;
        }

        var body = builder.ToString().Trim();
        return new ChangelogResult($"{devHosting.Configuration.VersionPrefix}{version}", body);
    }
}