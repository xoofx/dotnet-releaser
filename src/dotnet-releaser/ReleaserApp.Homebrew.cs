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
    private async Task UploadBrewFormula(GitHubClient github, PackageInfo packageInfo, List<PackageEntry> entries)
    {
        // Verify that we have generated packages for Homebrew
        var entriesForBrew = new List<(PackageEntry, string)>();
        foreach (var entry in entries)
        {
            var brewCheck = GetBrewCpuCheck(entry.RuntimeId);
            if (entry.Kind == PackageKind.Tar && brewCheck is not null)
            {
                entriesForBrew.Add((entry, brewCheck));
            }
        }

        // No entries compatible for brew
        if (entriesForBrew.Count == 0)
        {
            Info("No entries compatible with Homebrew (osx or linux with tar.gz packages)");
            return;
        }

        var appName = packageInfo.Name;
        var user = _config.GitHub.User!;
        var url = _config.GitHub.GetUrl();

        var githubVersion = $"{_config.GitHub.VersionPrefix}{packageInfo.Version}";

        var homebrew = $"homebrew-{appName}";
        var filePath = $"Formula/{appName}.rb";

        var formulaBuilder = new StringBuilder();

        // Heading
        formulaBuilder.Append($@"# This file was generated automatically by dotnet-releaser - DO NOT EDIT
class {appName} < Formula
  desc ""{EscapeRuby(packageInfo.Description)}""
  homepage ""{packageInfo.ProjectUrl}""
  version ""{packageInfo.Version}""
  license ""{packageInfo.License}""
");

        AppendPlatformEntries("macos","osx");
        AppendPlatformEntries("linux", "linux");
        
        formulaBuilder.AppendLine("end");
        var stringFormula = formulaBuilder.ToString().Replace("\r\n", "\n"); // replace with \n only
        Repository? existingRepository = null;
        try
        {
            existingRepository = await github.Repository.Get(user, homebrew);
        }
        catch (NotFoundException)
        {
            // ignore
        }

        if (existingRepository is null)
        {
            Info($"Creating Homebrew repository {user}/{homebrew}");
            var newRepository = new NewRepository(homebrew);
            newRepository.Description = $"Homebrew repository for {packageInfo.ProjectUrl}";
            newRepository.AutoInit = true;
            newRepository.LicenseTemplate = packageInfo.License;
            existingRepository = await github.Repository.Create(newRepository);
        }
        else
        {
            Info($"Homebrew repository found {user}/{homebrew}");
        }

        IReadOnlyList<RepositoryContent>? result = null;
        try
        {
            result = await github.Repository.Content.GetAllContents(user, homebrew, filePath);
        }
        catch (NotFoundException)
        {
            // ignore
        }

        var shouldCreate = result is null || result.Count == 0;

        if (shouldCreate)
        {
            Info($"Creating Homebrew Formula {user}/{homebrew}");
            await github.Repository.Content.CreateFile(user, homebrew, filePath,new CreateFileRequest($"{packageInfo.Version}", stringFormula));
        }
        else
        {
            Debug.Assert(result is not null);
            var file = result[0];
            if (file.Content != stringFormula)
            {
                Info($"Updating Homebrew Formula {user}/{homebrew}");
                await github.Repository.Content.UpdateFile(user, homebrew, filePath, new UpdateFileRequest($"{packageInfo.Version}", stringFormula, file.Sha));
            }
            else
            {
                Info($"No need to update Homebrew Formula {user}/{homebrew}. Already up-to-date.");
            }
        }

        // See for multi-targeting https://github.com/pokanop/homebrew-pokanop/blob/master/Formula/nostromo.rb
        void AppendPlatformEntries(string brewPlatform, string ridPrefix)
        {
            bool isPlatformActive = false;
            foreach (var (packageEntry, brewCpuCheck) in entriesForBrew.Where(x => x.Item1.RuntimeId.StartsWith($"{ridPrefix}-")))
            {
                if (!isPlatformActive)
                {
                    // https://rubydoc.brew.sh/OnOS.html
                    formulaBuilder.AppendLine($"  on_{brewPlatform} do");
                }

                formulaBuilder.Append($@"    if {brewCpuCheck}
      url ""{url}/releases/download/{githubVersion}/{Path.GetFileName(packageEntry.Path)}""
      sha256 ""{packageEntry.Sha256}""

      def install
        bin.install ""{appName}""
        cp Dir[""*.dylib*""], bin
      end
    end
");
                isPlatformActive = true;
            }
            if (isPlatformActive)
            {
                formulaBuilder.AppendLine("  end");
            }
        }
    }

    private static string? GetBrewCpuCheck(string rid)
    {
        // https://rubydoc.brew.sh/Hardware/CPU.html
        switch (rid)
        {
            case "osx-x64": return "Hardware::CPU.intel? && Hardware::CPU.is_64_bit?";
            case "osx-arm64": return "Hardware::CPU.arm? && Hardware::CPU.is_64_bit?";
            case "linux-x64": return "Hardware::CPU.intel? && Hardware::CPU.is_64_bit?";
            case "linux-arm": return "Hardware::CPU.arm? && Hardware::CPU.is_32_bit?";
            case "linux-arm64": return "Hardware::CPU.arm? && Hardware::CPU.is_64_bit?";
        }
        return null;
    }

    private static string EscapeRuby(string text)
    {
        return text.Replace("\"", @"\""");
    }
}