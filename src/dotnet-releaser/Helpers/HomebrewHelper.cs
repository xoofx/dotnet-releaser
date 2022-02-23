using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DotNetReleaser.Logging;

namespace DotNetReleaser.Helpers;

/// <summary>
/// Helper class to create a forumla
/// </summary>
public static class HomebrewHelper
{
    public static string? CreateFormula(IDevHosting hosting, ProjectPackageInfo projectPackageInfo, List<AppPackageInfo> entries)
    {
        var log = hosting.Logger;

        // Verify that we have generated packages for Homebrew
        var entriesForBrew = new List<(AppPackageInfo, string)>();
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
            log.Info("No entries compatible with Homebrew (osx or linux with tar.gz packages)");
            return null;
        }

        var appName = projectPackageInfo.AssemblyName;
        var formulaBuilder = new StringBuilder();

        // Make sure that the ruby class name is valid
        var className = RubyHelper.GetRubyClassNameFromAppName(appName);

        // Heading
        formulaBuilder.Append($@"# This file was generated automatically by dotnet-releaser - DO NOT EDIT
class {className} < Formula
  desc ""{EscapeRuby(projectPackageInfo.Description)}""
  homepage ""{projectPackageInfo.ProjectUrl}""
  version ""{projectPackageInfo.Version}""
  license ""{projectPackageInfo.License}""
");


        AppendPlatformEntries("macos", "osx");
        AppendPlatformEntries("linux", "linux");

        formulaBuilder.AppendLine("end");
        var stringFormula = formulaBuilder.ToString().Replace("\r\n", "\n"); // replace with \n only

        return stringFormula;
        
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
      url ""{hosting.GetDownloadReleaseUrl(projectPackageInfo.Version, Path.GetFileName(packageEntry.Path))}""
      sha256 ""{packageEntry.Sha256}""

      def install
        cp_r '.', bin
        bin.install ""{appName}""
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