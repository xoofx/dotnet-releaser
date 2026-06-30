using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace DotNetReleaser.Tests;

public class NuGetPackageTests
{
    [Test]
    public void BuildOidcTokenUrlAppendsAudienceToGitHubUrl()
    {
        var tokenUrl = NuGetTrustedPublishingClient.BuildOidcTokenUrl("https://token.actions.githubusercontent.com?id=123", "https://www.nuget.org");

        Assert.AreEqual("https://token.actions.githubusercontent.com?id=123&audience=https%3A%2F%2Fwww.nuget.org", tokenUrl);
    }

    [Test]
    public void BuildOidcTokenUrlAddsQueryWhenMissing()
    {
        var tokenUrl = NuGetTrustedPublishingClient.BuildOidcTokenUrl("https://token.actions.githubusercontent.com", "https://www.nuget.org");

        Assert.AreEqual("https://token.actions.githubusercontent.com?audience=https%3A%2F%2Fwww.nuget.org", tokenUrl);
    }

    [Test]
    public async Task CollectNuGetPackageOutputsIncludesRidSpecificToolPackages()
    {
        var packageDirectory = Path.Combine(Path.GetTempPath(), $"dotnet-releaser-{Guid.NewGuid():N}");
        Directory.CreateDirectory(packageDirectory);

        try
        {
            var packageInfo = new ProjectPackageInfo(
                Path.Combine(packageDirectory, "CodeAlta.csproj"),
                "CodeAlta",
                "alta",
                PackageOutputType.Exe,
                "0.0.0-alpha.0.996",
                "description",
                "license",
                "https://example.com",
                true,
                false,
                false,
                Array.Empty<string>(),
                new TargetFrameworkInfo(false, ["net10.0"], "net10.0"));

            var expectedFiles = new[]
            {
                "CodeAlta.0.0.0-alpha.0.996.nupkg",
                "CodeAlta.linux-arm64.0.0.0-alpha.0.996.nupkg",
                "CodeAlta.linux-x64.0.0.0-alpha.0.996.nupkg",
                "CodeAlta.win-x64.0.0.0-alpha.0.996.nupkg",
                "CodeAlta.win-x64.0.0.0-alpha.0.996.snupkg",
            };

            foreach (var expectedFile in expectedFiles)
            {
                await File.WriteAllTextAsync(Path.Combine(packageDirectory, expectedFile), string.Empty);
            }

            await File.WriteAllTextAsync(Path.Combine(packageDirectory, "CodeAlta.0.0.0-alpha.0.995.nupkg"), string.Empty);
            await File.WriteAllTextAsync(Path.Combine(packageDirectory, "Other.0.0.0-alpha.0.996.nupkg"), string.Empty);

            var minVerWorkaroundPath = Path.Combine(packageDirectory, "CodeAlta.1.0.0.nupkg");
            var actualFiles = ReleaserApp.CollectNuGetPackageOutputs([minVerWorkaroundPath], packageInfo)
                .Select(Path.GetFileName)
                .OrderBy(x => x)
                .ToArray();

            Assert.AreEqual(expectedFiles.OrderBy(x => x).ToArray(), actualFiles);
        }
        finally
        {
            Directory.Delete(packageDirectory, true);
        }
    }
}
