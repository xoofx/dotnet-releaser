using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetReleaser.Logging;
using Octokit;

namespace DotNetReleaser;

public partial class ReleaserApp
{

    private async Task<bool> CreateConfigurationFile(string? destinationFilePath, string projectFile, string? user, string? repo, bool force)
    {
        destinationFilePath ??= Path.Combine(Environment.CurrentDirectory, "dotnet-releaser.toml");
        destinationFilePath = Path.Combine(Environment.CurrentDirectory, destinationFilePath);
        if (!force && File.Exists(destinationFilePath))
        {
            Error($"Cannot create a new configuration file at `{destinationFilePath}`. The configuration file already exists. Use option --force to overwrite it.");
            return false;
        }

        var finalProjectFilePath = Path.Combine(Environment.CurrentDirectory, projectFile);
        string? changeLogPath = null;
        if (!File.Exists(finalProjectFilePath))
        {
            Warn($"The project file path `{finalProjectFilePath}` was not found.");
        }
        else
        {
            var file = new FileInfo(finalProjectFilePath);
            var originalDir = file.Directory!;
            var dir = originalDir;
            var dirPath = "";
            while (dir is not null)
            {
                changeLogPath = Path.Combine(dir.FullName, "changelog.md");
                if (File.Exists(changeLogPath))
                {
                    changeLogPath = $"{dirPath}{Path.GetFileName(changeLogPath)}";
                    break;
                }

                changeLogPath = null;
                dir = dir.Parent;
                dirPath += "../";
            }
        }

        var configAsText = $@"# configuration file for dotnet-releaser
[msbuild]
project = ""{projectFile.Replace('\\', '/')}""
[github]
user = ""{user ?? "github_user_or_org_here"}""
repo = ""{user ?? "github_repo_here"}""
";
        if (changeLogPath is not null)
        {
            configAsText += $@"[changelog]
path = ""{changeLogPath.Replace('\\', '/')}""
";
        }


        // Normalize the output
        configAsText = configAsText.Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
        await File.WriteAllTextAsync(destinationFilePath, configAsText, Encoding.Default);
        Info($"New configuration file `{destinationFilePath}` created successfully.");

        return true;
    }
}