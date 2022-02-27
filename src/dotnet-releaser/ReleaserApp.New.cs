using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

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

        var configAsText = $@"# configuration file for dotnet-releaser
[msbuild]
project = ""{projectFile.Replace('\\', '/')}""
[github]
user = ""{user ?? "github_user_or_org_here"}""
repo = ""{repo ?? "github_repo_here"}""
";

        // Normalize the output
        configAsText = configAsText.Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
        await File.WriteAllTextAsync(destinationFilePath, configAsText, Encoding.Default);
        Info($"New configuration file `{destinationFilePath}` created successfully.");

        return true;
    }
}