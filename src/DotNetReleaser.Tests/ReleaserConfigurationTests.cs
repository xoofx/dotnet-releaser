using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DotNetReleaser.Configuration;

namespace DotNetReleaser.Tests;

public class ReleaserConfigurationTests
{
    [Test]
    public async Task From_DeserializesCompleteConfigurationGraph()
    {
        using var fixture = new ConfigurationFixture();

        var configurationText = """
profile = "custom"
artifacts_folder = "artifacts-output"
enable_publish_packages_in_draft = true

[coverage]
enable = true
package = "custom.coverlet"
version = "9.9.9"
format = ["cobertura", "opencover"]
single_hit = true
source_link = false
include_test_assembly = false
skip_auto_props = false
does_not_return_attribute = false
deterministic_report = true
exclude = ["[Tests]*"]
exclude_by_file = ["**/*.g.cs"]
include = ["[Main]*"]
include_directory = ["generated"]

[test]
enable = false
run_tests = false
run_tests_for_debug = true

[msbuild]
publish = false
project = ["TestProject.csproj", "AnotherProject.csproj"]
configuration = "Ship"
configuration_debug = "ShipDebug"
build_debug = true
[msbuild.properties]
PublishReadyToRun = false
CustomProperty = "custom-value"

[github]
publish = false
base = "https://example.test/git"
api = "https://api.example.test"
user = "octocat"
repo = "hello-world"
version_prefix = "rel-"
branches = ["release", "preview"]

[changelog]
publish = true
path = "../changelog.md"
version = "^##\\s+(?<version>.*)$"
name_template = "{{ version.tag }} release"
body_template = '''
# Release Notes

{{ changes }}
'''
include_commits = false
defaults = false
owners = ["octocat", "hubot"]
owners_commit_change_template = "owner commit: {{ commit.title }}"
owners_pull_request_change_template = "owner pr: {{ pr.title }}"
commit_change_template = "commit: {{ commit.title }}"
pull_request_change_template = "pr: {{ pr.title }}"
disable_draft_for_build = true
[changelog.include]
labels = ["feature"]
contributors = ["contributor-a"]
[changelog.exclude]
labels = ["skip-release-notes"]
contributors = ["bot-user"]
[changelog.template_properties]
changes_pre_title = "Amazing"
hello_number = 42
[[changelog.autolabeler]]
label = "documentation"
title = "^Doc"
body = ["docs", "guide"]
files = ["doc/**/*.md"]
branch = "docs/.+"
[[changelog.replacer]]
pattern = "before"
replace = "after"
[[changelog.category]]
title = "## Docs"
labels = ["documentation", "doc"]
order = 12
[changelog.category.exclude]
labels = ["skip-docs"]
contributors = ["doc-bot"]

[nuget]
publish = false
publish_draft = true
source = "https://nuget.example.test/v3/index.json"

[brew]
publish = false
home = "homebrew-tools"

[scoop]
publish = false
home = "scoop-tools"

[service]
publish = true
[service.systemd]
publish = true
arguments = "/etc/service/config.toml"
user = "service-user"
create_user = true
[service.systemd.sections.Unit]
After = "network.target"
Description = "A custom service"
[service.systemd.sections.Service]
Environment = "DOTNET_ENVIRONMENT=Production"
[service.systemd.sections.Custom]
Answer = 42

[[deb.depends]]
name = "libfoo"
[[deb.depends]]
name = ["libbar", "libbaz"]

[[rpm.depends]]
name = "rpm-foo"
[[rpm.depends]]
name = ["rpm-bar", "rpm-baz"]

[[pack]]
publish = false
rid = "win-x64"
kinds = "zip"
renamer = [{ pattern = "win-x64", replace = "windows-amd64" }]

[[pack]]
rid = ["linux-x64", "linux-arm64"]
kinds = ["tar", "deb", "rpm"]
""";

        var (logger, configuration) = await fixture.LoadAsync(configurationText);

        Assert.That(configuration, Is.Not.Null, logger.Output.ToString());
        Assert.That(logger.HasErrors, Is.False, logger.Output.ToString());

        Assert.Multiple(() =>
        {
            Assert.That(configuration!.Profile, Is.EqualTo(PackagingProfileKind.Custom));
            Assert.That(configuration.ArtifactsFolder, Is.EqualTo(Path.Combine(fixture.ConfigurationDirectory, "artifacts-output")));
            Assert.That(configuration.EnablePublishPackagesInDraft, Is.True);

            Assert.That(configuration.Coverage.Enable, Is.True);
            Assert.That(configuration.Coverage.Package, Is.EqualTo("custom.coverlet"));
            Assert.That(configuration.Coverage.Version, Is.EqualTo("9.9.9"));
            CollectionAssert.AreEquivalent(new[] { "cobertura", "opencover", "json" }, configuration.Coverage.Format);
            Assert.That(configuration.Coverage.SingleHit, Is.True);
            Assert.That(configuration.Coverage.SourceLink, Is.False);
            Assert.That(configuration.Coverage.IncludeTestAssembly, Is.False);
            Assert.That(configuration.Coverage.SkipAutoProps, Is.False);
            Assert.That(configuration.Coverage.DoesNotReturnAttribute, Is.False);
            Assert.That(configuration.Coverage.DeterministicReport, Is.True);
            CollectionAssert.AreEqual(new[] { "[Tests]*" }, configuration.Coverage.Exclude);
            CollectionAssert.AreEqual(new[] { "**/*.g.cs" }, configuration.Coverage.ExcludeByFile);
            CollectionAssert.AreEqual(new[] { "[Main]*" }, configuration.Coverage.Include);
            CollectionAssert.AreEqual(new[] { "generated" }, configuration.Coverage.IncludeDirectory);

            Assert.That(configuration.Test.Enable, Is.False);
            Assert.That(configuration.Test.RunTests, Is.False);
            Assert.That(configuration.Test.RunTestsForDebug, Is.True);

            Assert.That(configuration.MSBuild.Publish, Is.False);
            CollectionAssert.AreEqual(new[] { "TestProject.csproj", "AnotherProject.csproj" }, configuration.MSBuild.Projects);
            Assert.That(configuration.MSBuild.Configuration, Is.EqualTo("Ship"));
            Assert.That(configuration.MSBuild.ConfigurationDebug, Is.EqualTo("ShipDebug"));
            Assert.That(configuration.MSBuild.BuildDebug, Is.True);
            Assert.That(configuration.MSBuild.Properties["PublishReadyToRun"], Is.EqualTo(false));
            Assert.That(configuration.MSBuild.Properties["CustomProperty"], Is.EqualTo("custom-value"));

            Assert.That(configuration.GitHub.Publish, Is.False);
            Assert.That(configuration.GitHub.Base, Is.EqualTo("https://example.test/git"));
            Assert.That(configuration.GitHub.Api, Is.EqualTo("https://api.example.test"));
            Assert.That(configuration.GitHub.User, Is.EqualTo("octocat"));
            Assert.That(configuration.GitHub.Repo, Is.EqualTo("hello-world"));
            Assert.That(configuration.GitHub.VersionPrefix, Is.EqualTo("rel-"));
            CollectionAssert.AreEqual(new[] { "release", "preview" }, configuration.GitHub.Branches);

            Assert.That(configuration.Changelog.Publish, Is.True);
            Assert.That(configuration.Changelog.Path, Is.EqualTo(Path.Combine(fixture.RootDirectory, "changelog.md")));
            Assert.That(configuration.Changelog.Version, Is.EqualTo("^##\\s+(?<version>.*)$"));
            Assert.That(configuration.Changelog.NameTemplate, Is.EqualTo("{{ version.tag }} release"));
            Assert.That(configuration.Changelog.BodyTemplate, Does.Contain("# Release Notes"));
            Assert.That(configuration.Changelog.IncludeCommits, Is.False);
            Assert.That(configuration.Changelog.Defaults, Is.False);
            CollectionAssert.AreEqual(new[] { "octocat", "hubot" }, configuration.Changelog.Owners);
            Assert.That(configuration.Changelog.OwnersCommitChangeTemplate, Is.EqualTo("owner commit: {{ commit.title }}"));
            Assert.That(configuration.Changelog.OwnersPullRequestChangeTemplate, Is.EqualTo("owner pr: {{ pr.title }}"));
            Assert.That(configuration.Changelog.CommitChangeTemplate, Is.EqualTo("commit: {{ commit.title }}"));
            Assert.That(configuration.Changelog.PullRequestChangeTemplate, Is.EqualTo("pr: {{ pr.title }}"));
            Assert.That(configuration.Changelog.DisableDraftForBuild, Is.True);
            CollectionAssert.AreEqual(new[] { "feature" }, configuration.Changelog.Include.Labels);
            CollectionAssert.AreEqual(new[] { "contributor-a" }, configuration.Changelog.Include.Contributors);
            CollectionAssert.AreEqual(new[] { "skip-release-notes" }, configuration.Changelog.Exclude.Labels);
            CollectionAssert.AreEqual(new[] { "bot-user" }, configuration.Changelog.Exclude.Contributors);
            Assert.That(configuration.Changelog.TemplateProperties["changes_pre_title"], Is.EqualTo("Amazing"));
            Assert.That(Convert.ToInt32(configuration.Changelog.TemplateProperties["hello_number"]), Is.EqualTo(42));
            Assert.That(configuration.Changelog.Autolabelers.Count, Is.EqualTo(1));
            Assert.That(configuration.Changelog.Autolabelers[0].Label, Is.EqualTo("documentation"));
            Assert.That(configuration.Changelog.Autolabelers[0].Title, Is.EqualTo(new[] { "^Doc" }));
            Assert.That(configuration.Changelog.Autolabelers[0].Body, Is.EqualTo(new[] { "docs", "guide" }));
            Assert.That(configuration.Changelog.Autolabelers[0].Files, Is.EqualTo(new[] { "doc/**/*.md" }));
            Assert.That(configuration.Changelog.Autolabelers[0].Branch, Is.EqualTo(new[] { "docs/.+" }));
            Assert.That(configuration.Changelog.Replacers.Count, Is.EqualTo(1));
            Assert.That(configuration.Changelog.Replacers[0].Pattern, Is.EqualTo("before"));
            Assert.That(configuration.Changelog.Replacers[0].Replace, Is.EqualTo("after"));
            Assert.That(configuration.Changelog.Categories.Count, Is.EqualTo(1));
            Assert.That(configuration.Changelog.Categories[0].Title, Is.EqualTo("## Docs"));
            Assert.That(configuration.Changelog.Categories[0].Order, Is.EqualTo(12));
            Assert.That(configuration.Changelog.Categories[0].Labels, Is.EqualTo(new[] { "documentation", "doc" }));
            Assert.That(configuration.Changelog.Categories[0].Exclude.Labels, Is.EqualTo(new[] { "skip-docs" }));
            Assert.That(configuration.Changelog.Categories[0].Exclude.Contributors, Is.EqualTo(new[] { "doc-bot" }));

            Assert.That(configuration.NuGet.Publish, Is.False);
            Assert.That(configuration.NuGet.PublishDraft, Is.True);
            Assert.That(configuration.NuGet.Source, Is.EqualTo("https://nuget.example.test/v3/index.json"));

            Assert.That(configuration.Brew.Publish, Is.False);
            Assert.That(configuration.Brew.Home, Is.EqualTo("homebrew-tools"));
            Assert.That(configuration.Scoop.Publish, Is.False);
            Assert.That(configuration.Scoop.Home, Is.EqualTo("scoop-tools"));

            Assert.That(configuration.Service.Publish, Is.True);
            Assert.That(configuration.Service.Systemd.Publish, Is.True);
            Assert.That(configuration.Service.Systemd.Arguments, Is.EqualTo("/etc/service/config.toml"));
            Assert.That(configuration.Service.Systemd.User, Is.EqualTo("service-user"));
            Assert.That(configuration.Service.Systemd.CreateUser, Is.True);
            Assert.That(configuration.Service.Systemd.Sections["Unit"]["After"], Is.EqualTo("network.target"));
            Assert.That(configuration.Service.Systemd.Sections["Unit"]["Description"], Is.EqualTo("A custom service"));
            Assert.That(configuration.Service.Systemd.Sections["Service"]["Environment"], Is.EqualTo("DOTNET_ENVIRONMENT=Production"));
            Assert.That(Convert.ToInt32(configuration.Service.Systemd.Sections["Custom"]["Answer"]), Is.EqualTo(42));

            Assert.That(configuration.Debian.Depends.Count, Is.EqualTo(2));
            Assert.That(configuration.Debian.Depends[0].Names, Is.EqualTo(new[] { "libfoo" }));
            Assert.That(configuration.Debian.Depends[1].Names, Is.EqualTo(new[] { "libbar", "libbaz" }));
            Assert.That(configuration.Rpm.Depends.Count, Is.EqualTo(2));
            Assert.That(configuration.Rpm.Depends[0].Names, Is.EqualTo(new[] { "rpm-foo" }));
            Assert.That(configuration.Rpm.Depends[1].Names, Is.EqualTo(new[] { "rpm-bar", "rpm-baz" }));

            Assert.That(configuration.Packs.Count, Is.EqualTo(2));
            Assert.That(configuration.Packs[0].Publish, Is.False);
            Assert.That(configuration.Packs[0].RuntimeIdentifiers, Is.EqualTo(new[] { "win-x64" }));
            Assert.That(configuration.Packs[0].Kinds, Is.EqualTo(new[] { PackageKind.Zip }));
            Assert.That(configuration.Packs[0].Renamers.Count, Is.EqualTo(1));
            Assert.That(configuration.Packs[0].Renamers[0].Pattern, Is.EqualTo("win-x64"));
            Assert.That(configuration.Packs[0].Renamers[0].Replace, Is.EqualTo("windows-amd64"));
            Assert.That(configuration.Packs[1].RuntimeIdentifiers, Is.EqualTo(new[] { "linux-x64", "linux-arm64" }));
            Assert.That(configuration.Packs[1].Kinds, Is.EqualTo(new[] { PackageKind.Tar, PackageKind.Deb, PackageKind.Rpm }));
        });
    }

    [TestCaseSource(nameof(GetDocumentationTomlSnippets))]
    public async Task From_DeserializesEveryDocumentationTomlExample(string sourceName, string snippet)
    {
        using var fixture = new ConfigurationFixture();
        var configurationText = ComposeDocumentationConfiguration(snippet);

        var (logger, configuration) = await fixture.LoadAsync(configurationText);

        Assert.That(configuration, Is.Not.Null, $"{sourceName}{Environment.NewLine}{logger.Output}");
        Assert.That(logger.HasErrors, Is.False, $"{sourceName}{Environment.NewLine}{logger.Output}");
    }

    public static IEnumerable<TestCaseData> GetDocumentationTomlSnippets()
    {
        var repositoryRoot = GetRepositoryRoot();
        var documentationDirectory = Path.Combine(repositoryRoot, "doc");
        var markdownFiles = Directory.GetFiles(documentationDirectory, "*.md", SearchOption.AllDirectories);

        foreach (var file in markdownFiles)
        {
            var content = File.ReadAllText(file);
            var matches = Regex.Matches(content, @"```toml\s*(.*?)```", RegexOptions.Singleline);
            for (var i = 0; i < matches.Count; i++)
            {
                var snippet = matches[i].Groups[1].Value.Trim();
                var relativeFile = Path.GetRelativePath(repositoryRoot, file);
                var sourceName = $"{relativeFile} TOML #{i + 1}";
                yield return new TestCaseData(sourceName, snippet).SetName($"From_Deserializes_{SanitizeTestName(sourceName)}");
            }
        }
    }

    private static string ComposeDocumentationConfiguration(string snippet)
    {
        var builder = new StringBuilder();

        if (!HasAssignment(snippet, "profile"))
        {
            builder.AppendLine("profile = \"custom\"");
        }

        if (!HasAssignment(snippet, "artifacts_folder"))
        {
            builder.AppendLine("artifacts_folder = \"artifacts-docs\"");
        }

        if (!HasAssignment(snippet, "project"))
        {
            builder.AppendLine("[msbuild]");
            builder.AppendLine("project = \"TestProject.csproj\"");
            builder.AppendLine();
        }

        builder.AppendLine(snippet);

        if (!HasTable(snippet, "github"))
        {
            builder.AppendLine("[github]");
            builder.AppendLine("user = \"doc-user\"");
            builder.AppendLine("repo = \"doc-repo\"");
            builder.AppendLine();
        }
        return builder.ToString();
    }

    private static bool HasAssignment(string snippet, string propertyName)
    {
        return Regex.IsMatch(snippet, $@"(?m)^\s*{Regex.Escape(propertyName)}\s*=");
    }

    private static bool HasTable(string snippet, string tableName)
    {
        return Regex.IsMatch(snippet, $@"(?m)^\s*\[{Regex.Escape(tableName)}\]\s*$");
    }

    private static string SanitizeTestName(string sourceName)
    {
        var builder = new StringBuilder(sourceName.Length);
        foreach (var character in sourceName)
        {
            builder.Append(char.IsLetterOrDigit(character) ? character : '_');
        }

        return builder.ToString();
    }

    private static string GetRepositoryRoot()
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    }

    private sealed class ConfigurationFixture : IDisposable
    {
        public ConfigurationFixture()
        {
            RootDirectory = Path.Combine(Path.GetTempPath(), "dotnet-releaser-config-tests", Guid.NewGuid().ToString("N"));
            ConfigurationDirectory = Path.Combine(RootDirectory, "config");
            ConfigurationPath = Path.Combine(ConfigurationDirectory, "dotnet-releaser.toml");

            Directory.CreateDirectory(ConfigurationDirectory);
            Directory.CreateDirectory(Path.Combine(RootDirectory, "Path", "To", "My"));

            File.WriteAllText(Path.Combine(ConfigurationDirectory, "TestProject.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
            File.WriteAllText(Path.Combine(ConfigurationDirectory, "AnotherProject.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
            File.WriteAllText(Path.Combine(ConfigurationDirectory, "HelloWorld.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
            File.WriteAllText(Path.Combine(RootDirectory, "Path", "To", "My", "Project.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");
            File.WriteAllText(Path.Combine(RootDirectory, "changelog.md"), "# Changelog");
        }

        public string RootDirectory { get; }

        public string ConfigurationDirectory { get; }

        public string ConfigurationPath { get; }

        public async Task<(MockSimpleLogger Logger, ReleaserConfiguration? Configuration)> LoadAsync(string content)
        {
            await File.WriteAllTextAsync(ConfigurationPath, content);
            var logger = new MockSimpleLogger();
            var configuration = await ReleaserConfiguration.From(ConfigurationPath, logger);
            return (logger, configuration);
        }

        public void Dispose()
        {
            if (Directory.Exists(RootDirectory))
            {
                Directory.Delete(RootDirectory, true);
            }
        }
    }
}
