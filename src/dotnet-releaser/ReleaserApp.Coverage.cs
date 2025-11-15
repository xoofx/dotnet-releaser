using System;
using System.Threading.Tasks;
using DotNetReleaser.Coverage;

namespace DotNetReleaser;

public partial class ReleaserApp
{
    private async Task PublishCoverageToGist(IDevHosting devHosting, BuildInformation buildInfo, HitCoverage lineCoverage)
    {
        if (!_config.Coverage.BadgeUploadToGist || !buildInfo.IsPush) return;

        var gistId = _config.Coverage.BadgeGistId;
        if (string.IsNullOrWhiteSpace(gistId))
        {
            Warn("The 'coverage.badge_gist_id' is not set in the configuration file. The coverage badge will not be uploaded to a gist.");
            return;
        }
        
        var rate = (int)Math.Round((double)lineCoverage.Rate * 100);

        // TODO: We could make many of these things configurable (colors, size of the badge, etc.)
        var color = rate switch
        {
            >= 95 => "#4c1",
            >= 90 => "#a3c51c",
            >= 75 => "#dfb317",
            _ => "#e05d44"
        };
        
        var svg = $"""
                  <svg xmlns="http://www.w3.org/2000/svg" width="99" height="20"><linearGradient id="a" x2="0" y2="100%"><stop offset="0" stop-color="#bbb" stop-opacity=".1"/><stop offset="1" stop-opacity=".1"/></linearGradient><rect rx="3" width="99" height="20" fill="#555"/><rect rx="3" x="63" width="36" height="20" fill="{color}"/><path fill="{color}" d="M63 0h4v20h-4z"/><rect rx="3" width="99" height="20" fill="url(#a)"/><g fill="#fff" text-anchor="middle" font-family="DejaVu Sans,Verdana,Geneva,sans-serif" font-size="11"><text x="32.5" y="15" fill="#010101" fill-opacity=".3">coverage</text><text x="32.5" y="14">coverage</text><text x="80" y="15" fill="#010101" fill-opacity=".3">{rate:##}%</text><text x="80" y="14">{rate:##}%</text></g></svg>
                  """;

        var fileName = $"dotnet-releaser-coverage-badge-{_config.GitHub.User}-{_config.GitHub.Repo}.svg";
        Info($"Updating coverage badge with {rate:##}% result to gist {gistId} and file {fileName}");
        await devHosting.CreateOrUpdateGist(gistId, fileName, svg);
    }
}