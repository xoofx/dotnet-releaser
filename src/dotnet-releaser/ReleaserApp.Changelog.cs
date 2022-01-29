using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DotNetReleaser;

public partial class ReleaserApp
{
    private async Task<string?> LoadChangeLog(PackageInfo info)
    {
        if (string.IsNullOrEmpty(_config.Changelog.Path)) return null;

        var lines = await File.ReadAllLinesAsync(_config.Changelog.Path);

        var matcher = new Regex(_config.Changelog.Version);

        var builder = new StringBuilder();

        bool versionFound = false;
        foreach (var line in lines)
        {
            var match = matcher.Match(line);
            if (match.Success && match.Groups.Count > 1 && match.Groups[1].Value == info.Version)
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
            throw new AppException($"Unable to find version {info.Version} from changelog.md");
        }

        return builder.ToString().Trim();
    }
}