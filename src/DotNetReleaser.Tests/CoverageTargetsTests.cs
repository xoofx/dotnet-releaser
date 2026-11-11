using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DotNetReleaser.Runners;

namespace DotNetReleaser.Tests;

public class CoverageTargetsTests
{
    private const string CoveragePackage = "coverlet.collector";
    private const string CoverageVersion = "6.0.4";

    [TestCase(false, null)]
    [TestCase(false, "coverlet.collector")]
    [TestCase(false, "Coverlet.Collector")]
    [TestCase(true, null)]
    [TestCase(true, "coverlet.collector")]
    public async Task CoveragePackageIsReferencedOnce(bool centralPackageManagement, string? existingReference)
    {
        var projectFolder = Path.Combine(Path.GetTempPath(), $"dotnet-releaser-{Guid.NewGuid():N}");
        Directory.CreateDirectory(projectFolder);
        try
        {
            var projectFile = Path.Combine(projectFolder, "MyTests.csproj");
            var reference = existingReference is null
                ? string.Empty
                : centralPackageManagement
                    ? $"""<PackageReference Include="{existingReference}" />"""
                    : $"""<PackageReference Include="{existingReference}" Version="1.0.0" />""";
            await File.WriteAllTextAsync(projectFile, $"""
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                    <IsTestProject>true</IsTestProject>
                  </PropertyGroup>
                  <ItemGroup>
                    {reference}
                  </ItemGroup>
                </Project>
                """);
            if (centralPackageManagement)
            {
                await File.WriteAllTextAsync(Path.Combine(projectFolder, "Directory.Packages.props"), $"""
                    <Project>
                      <PropertyGroup>
                        <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
                      </PropertyGroup>
                      <ItemGroup>
                        <PackageVersion Include="{CoveragePackage}" Version="1.0.0" />
                      </ItemGroup>
                    </Project>
                    """);
            }

            using var runner = new MSBuildRunner(Path.Combine(projectFolder, "artifacts"))
            {
                Project = projectFile,
                WorkingDirectory = projectFolder,
                CustomAfterMicrosoftCommonTargets = Path.Combine(AppContext.BaseDirectory, "dotnet-releaser.targets"),
                Properties =
                {
                    ["DotNetReleaserCoverage"] = "true",
                    ["DotNetReleaserCoveragePackage"] = CoveragePackage,
                    ["DotNetReleaserCoverageVersion"] = CoverageVersion,
                },
                Arguments = { "-getItem:PackageReference" }
            };
            var result = await runner.Run(new MockSimpleLogger());
            Assert.AreEqual(0, result.CommandResult.ExitCode, result.Output);

            using var json = JsonDocument.Parse(result.Output[result.Output.IndexOf('{')..]);
            var coverageReferences = json.RootElement.GetProperty("Items").GetProperty("PackageReference").EnumerateArray()
                .Where(x => string.Equals(x.GetProperty("Identity").GetString(), CoveragePackage, StringComparison.OrdinalIgnoreCase))
                .ToList();
            Assert.AreEqual(1, coverageReferences.Count, result.Output);

            // An existing reference of the project is kept as it is; otherwise the configured version is used.
            var version = coverageReferences[0].TryGetProperty(centralPackageManagement ? "VersionOverride" : "Version", out var value) ? value.GetString() : null;
            Assert.AreEqual(existingReference is null ? CoverageVersion : centralPackageManagement ? null : "1.0.0", string.IsNullOrEmpty(version) ? null : version, result.Output);
        }
        finally
        {
            Directory.Delete(projectFolder, true);
        }
    }
}
