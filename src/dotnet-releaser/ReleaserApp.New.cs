using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LibGit2Sharp;

namespace DotNetReleaser;

public partial class ReleaserApp
{
    //                 (1)          (2)
    // git@github.com:xoofx/dotnet-releaser.git
    private static readonly Regex GitUrlRegex = new Regex(@":(.+)/(.+)\.git");

    private async Task<bool> CreateConfigurationFile(string? destinationFilePath, string? projectFile, string? user, string? repo, bool force)
    {
        destinationFilePath ??= Path.Combine(Environment.CurrentDirectory, "dotnet-releaser.toml");
        destinationFilePath = Path.Combine(Environment.CurrentDirectory, destinationFilePath);
        if (!force && File.Exists(destinationFilePath))
        {
            Error($"Cannot create a new configuration file at `{destinationFilePath}`. The configuration file already exists. Use option --force to overwrite it.");
            return false;
        }

        var folder = Path.GetDirectoryName(destinationFilePath)!;
        if (projectFile is null)
        {
            projectFile = Directory.GetFiles(folder).FirstOrDefault(x => x.EndsWith(".sln"));
            if (projectFile is null)
            {
                Error($"Unable to find a solution file in the folder {folder}");
                return false;
            }

            projectFile = Path.GetFileName(projectFile);
        }

        var repositoryPath = Repository.Discover(Path.GetDirectoryName(destinationFilePath));
        if (repositoryPath is not null && user is null && repo is null)
        {
            var repository = new Repository(repositoryPath);
            foreach (var remote in repository.Network.Remotes)
            {
                var url = remote.Url;
                if (url.StartsWith("git"))
                {
                    var match = GitUrlRegex.Match(url);
                    if (match.Success)
                    {
                        user = match.Groups[1].Value;
                        repo = match.Groups[2].Value;
                        break;
                    }
                }
                else if (url.StartsWith("http"))
                {
                    // https://github.com/xoofx/dotnet-releaser.git
                    var uri = new Uri(url);
                    var path = uri.PathAndQuery;
                    user = Path.GetFileName(Path.GetDirectoryName(path));
                    repo = Path.GetFileNameWithoutExtension(path);
                    break;
                }
            }
        }

        user ??= "github_user_or_org_here";
        repo ??= "github_repo_here";
        

        var configAsText = $@"# configuration file for dotnet-releaser
[msbuild]
project = ""{projectFile.Replace('\\', '/')}""
[github]
user = ""{user}""
repo = ""{repo}""
";

        // Normalize the output
        configAsText = configAsText.Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
        await File.WriteAllTextAsync(destinationFilePath, configAsText, Encoding.Default);
        Info($"New configuration file `{destinationFilePath}` created successfully.");

        return true;
    }
}