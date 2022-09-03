using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DotNetReleaser.Helpers;

namespace DotNetReleaser;

public partial class ReleaserApp
{
    private void UpdateScoopConfigurationFromPackage(ProjectPackageInfo projectPackageInfo)
    {
        if (!_config.Scoop.Publish) return;

        _config.Scoop.Home = string.IsNullOrEmpty(_config.Scoop.Home) ? $"scoop-{projectPackageInfo.AssemblyName}" : _config.Scoop.Home;

        Info($"The configured scoop destination repository is `{_config.GitHub.User}/{_config.Scoop.Home}`.");
    }

    private string? CreateScoopManifest(IDevHosting hosting, ProjectPackageInfo projectPackageInfo, List<AppPackageInfo> appPackagesToPublish)
    {
        var entriesForScoop = new List<(AppPackageInfo, string)>();
        foreach (var packageInfo in appPackagesToPublish)
        {
            var architecture = GetScoopArchitecture(packageInfo.RuntimeId);
            if (packageInfo.Kind == PackageKind.Zip && architecture is not null)
            {
                entriesForScoop.Add((packageInfo, architecture));
            }
        }

        if (entriesForScoop.Count == 0)
        {
            return null;
        }

        UpdateScoopConfigurationFromPackage(projectPackageInfo);

        var appName = projectPackageInfo.AssemblyName;
        var manifestBuilder = new StringBuilder();

        manifestBuilder.AppendLine($@"{{
    ""homepage"": ""{projectPackageInfo.ProjectUrl}"",
    ""license"": ""{projectPackageInfo.License}"",
    ""description"": ""{projectPackageInfo.Description}"",
    ""version"": ""{projectPackageInfo.Version}"",
    ""architecture"": {{");

        var entries = entriesForScoop.Where(x => x.Item1.RuntimeId.StartsWith("win-")).ToArray();
        for (var i = 0; i < entries.Length; i++)
        {
            var (packageEntry, arch) = entries[i];
            manifestBuilder.Append($@"        ""{arch}"": {{
            ""url"": ""{hosting.GetDownloadReleaseUrl(projectPackageInfo.Version, Path.GetFileName(packageEntry.Path))}"",
            ""hash"": ""{packageEntry.Sha256}""
        }}");

            if (i < entries.Length - 1)
            {
                manifestBuilder.AppendLine(",");
            }
            else
            {
                manifestBuilder.AppendLine();
            }
        }

        manifestBuilder.AppendLine($@"    }},
    ""bin"": ""{appName}.exe""
}}").AppendLine();

        return manifestBuilder.ToString().Replace("\r\n", "\n");
    }

    private static string? GetScoopArchitecture(string rid) => rid switch
    {
        "win-x64" => "64bit",
        "win-x86" => "32bit",
        _ => null
    };
}