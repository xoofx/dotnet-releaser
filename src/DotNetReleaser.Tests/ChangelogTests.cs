using System;
using System.Threading.Tasks;
using DotNetReleaser.Changelog;
using DotNetReleaser.Configuration;
using NuGet.Versioning;
using NUnit.Framework;

namespace DotNetReleaser.Tests;

public class ChangelogTests
{
    [Test]
    public async Task TestBig()
    {
        await AssertTemplate(@"# Changes

## 🚨 Breaking Changes

- Breaking change of feature x (8ec9e7a1) by @mister_breaking

## ✨ New Features

- Maybe support for xyz (PR #1) by @mister_pr_feature
- Add support for new feature y (3608a391) by @mister_feature

## 🐛 Bug Fixes

- Fix issue with blablabla (6ec0a0ea) by @mister_bug

## 🚀 Enhancements

- Improve performance of xyz (d4563ad7) by @mister_perf

## 🧰 Maintenance

- Fix ci (d8b26fb1) by @mister_ci

## 🏭 Tests

- Add tests for abc (PR #3)
- Add tests (64774134) by @mister_test

## 🛠 Examples

- Add example for alpha (PR #2) by @mister_pr_example
- Improve example (51bfc6bc) by @mister_example

## 📚 Documentation

- Add documentation (ac9c7bcf) by @mister_doc

## 🌎 Accessibility

- Fix accessibility (d8d88f20) by @mister_accessibility

## 📦 Dependencies

- Update dependency of package xyz/0.1.2 (4c08f780) by @mister_deps

## 🧰 Misc

- Go to misc (47b1ecf3) by @mister_misc
- Go to misc but it's the owner (47b1ecf3)

**Full Changelog**: [0.1.3...1.0.0](https://github.com/xoofx/dotnet-releaser/compare/0.1.3...1.0.0)

<sub>Published with [dotnet-releaser](https://github.com/xoofx/dotnet-releaser/)</sub>
", GetBigChangesImpl);
    }

    [Test]
    public async Task TestBigWithoutCommits()
    {
        await AssertTemplate(@"# Changes

## ✨ New Features

- Maybe support for xyz (PR #1) by @mister_pr_feature

## 🏭 Tests

- Add tests for abc (PR #3)

## 🛠 Examples

- Add example for alpha (PR #2) by @mister_pr_example

**Full Changelog**: [0.1.3...1.0.0](https://github.com/xoofx/dotnet-releaser/compare/0.1.3...1.0.0)

<sub>Published with [dotnet-releaser](https://github.com/xoofx/dotnet-releaser/)</sub>
", GetBigChangesImpl, configuration =>
        {
            configuration.IncludeCommits = false;
        });
    }

    [Test]
    public async Task TestBigWithoutCommitsAndIncludeLabels()
    {
        await AssertTemplate(@"# Changes

## ✨ New Features

- Maybe support for xyz (PR #1) by @mister_pr_feature

**Full Changelog**: [0.1.3...1.0.0](https://github.com/xoofx/dotnet-releaser/compare/0.1.3...1.0.0)

<sub>Published with [dotnet-releaser](https://github.com/xoofx/dotnet-releaser/)</sub>
", GetBigChangesImpl, configuration =>
        {
            configuration.IncludeCommits = false;
            configuration.Include.Labels.Add("feature");
        });
    }

    [Test]
    public async Task TestBigWithoutCommitsAndIncludeContributors()
    {
        await AssertTemplate(@"# Changes

## 🛠 Examples

- Add example for alpha (PR #2) by @mister_pr_example

**Full Changelog**: [0.1.3...1.0.0](https://github.com/xoofx/dotnet-releaser/compare/0.1.3...1.0.0)

<sub>Published with [dotnet-releaser](https://github.com/xoofx/dotnet-releaser/)</sub>
", GetBigChangesImpl, configuration =>
        {
            configuration.IncludeCommits = false;
            configuration.Include.Contributors.Add("mister_pr_example");
        });
    }
    
    [Test]
    public async Task TestBigWithoutCommitsAndExcludeContributor()
    {
        await AssertTemplate(@"# Changes

## ✨ New Features

- Maybe support for xyz (PR #1) by @mister_pr_feature

## 🏭 Tests

- Add tests for abc (PR #3)

**Full Changelog**: [0.1.3...1.0.0](https://github.com/xoofx/dotnet-releaser/compare/0.1.3...1.0.0)

<sub>Published with [dotnet-releaser](https://github.com/xoofx/dotnet-releaser/)</sub>
", GetBigChangesImpl, configuration =>
        {
            configuration.IncludeCommits = false;
            configuration.Exclude.Contributors.Add("mister_pr_example");
        });
    }

    [Test]
    public async Task TestBigWithoutCommitsAndExcludeLabels()
    {
        await AssertTemplate(@"# Changes

## 🏭 Tests

- Add tests for abc (PR #3)

## 🛠 Examples

- Add example for alpha (PR #2) by @mister_pr_example

**Full Changelog**: [0.1.3...1.0.0](https://github.com/xoofx/dotnet-releaser/compare/0.1.3...1.0.0)

<sub>Published with [dotnet-releaser](https://github.com/xoofx/dotnet-releaser/)</sub>
", GetBigChangesImpl, configuration =>
        {
            configuration.IncludeCommits = false;
            configuration.Exclude.Labels.Add("feature");
        });
    }

    [Test]
    public async Task TestFilesDispatch()
    {
        await AssertTemplate(@"# Changes

## 📚 Documentation

- This is a change of doc 1 (PR #1) by @mister_pr_doc

## 🧰 Misc

- This is not a change of doc 2 (PR #2) by @mister_pr_misc

**Full Changelog**: [0.1.3...1.0.0](https://github.com/xoofx/dotnet-releaser/compare/0.1.3...1.0.0)

<sub>Published with [dotnet-releaser](https://github.com/xoofx/dotnet-releaser/)</sub>
", GetBranchAndFilesDispatchChangesImpl, configuration =>
        {
            configuration.Autolabelers.Insert(0, new ChangelogAutolabeler("doc").AppendFiles("/doc/*.md"));
        });
    }

    [Test]
    public async Task TestBranchDispatch()
    {
        await AssertTemplate(@"# Changes

## 🛠 Examples

- This is not a change of doc 2 (PR #2) by @mister_pr_misc

## 🧰 Misc

- This is a change of doc 1 (PR #1) by @mister_pr_doc

**Full Changelog**: [0.1.3...1.0.0](https://github.com/xoofx/dotnet-releaser/compare/0.1.3...1.0.0)

<sub>Published with [dotnet-releaser](https://github.com/xoofx/dotnet-releaser/)</sub>
", GetBranchAndFilesDispatchChangesImpl, configuration =>
        {
            configuration.Autolabelers.Insert(0, new ChangelogAutolabeler("samples").AppendBranch(@"special_branch\d+"));
        });
    }

    [Test]
    public async Task TestBodyDispatch()
    {
        await AssertTemplate(@"# Changes

## ✨ New Features

- This is not a change of doc 2 (PR #2) by @mister_pr_misc

## 🧰 Misc

- This is a change of doc 1 (PR #1) by @mister_pr_doc

**Full Changelog**: [0.1.3...1.0.0](https://github.com/xoofx/dotnet-releaser/compare/0.1.3...1.0.0)

<sub>Published with [dotnet-releaser](https://github.com/xoofx/dotnet-releaser/)</sub>
", GetBranchAndFilesDispatchChangesImpl, configuration =>
        {
            configuration.Autolabelers.Insert(0, new ChangelogAutolabeler("feature").AppendBody(@"special \d+ comment"));
        });
    }

    [Test]
    public async Task TestBodyTemplateWithProperties()
    {
        await AssertTemplate(@"# Amazing Changes 1

## ✨ New Features

- This is not a change of doc 2 (PR #2) by @mister_pr_misc

## 🧰 Misc

- This is a change of doc 1 (PR #1) by @mister_pr_doc

**Full Changelog**: [0.1.3...1.0.0](https://github.com/xoofx/dotnet-releaser/compare/0.1.3...1.0.0)

<sub>Published with [dotnet-releaser](https://github.com/xoofx/dotnet-releaser/)</sub>
", GetBranchAndFilesDispatchChangesImpl, configuration =>
        {
            configuration.BodyTemplate = @"# {{ properties.changes_pre_title }} Changes {{ properties.hello_number }}

{{ changes }}

**Full Changelog**: {{ url_full_changelog_compare_changes }}

<sub>Published with [dotnet-releaser](https://github.com/xoofx/dotnet-releaser/)</sub>
";
            configuration.TemplateProperties.Add("changes_pre_title", "Amazing");
            configuration.TemplateProperties.Add("hello_number", 1);
            configuration.Autolabelers.Insert(0, new ChangelogAutolabeler("feature").AppendBody(@"special \d+ comment"));
        });
    }

    [Test]
    public async Task TestErrorRegex()
    {
        var (log, result) = await CreateChangelog(GetBigChangesImpl, configuration =>
        {
            configuration.Autolabelers.Add(new ChangelogAutolabeler("feature").AppendBody(@"[\d+ invalid regex"));
        });
        Assert.True(log.HasErrors, $"No errors while expecting: {log.Output}");
        AssertHelper.Equals(@"error: Invalid regex `[\d+ invalid regex` for property `autolabeler.body. Invalid pattern '[\d+ invalid regex' at offset 18. Unterminated [] set.", log.Output.ToString().Trim());
    }

    [Test]
    public async Task TestErrorTemplate()
    {
        var (log, result) = await CreateChangelog(GetBigChangesImpl, configuration =>
        {
            configuration.NameTemplate = "{{ invalid + ";
        });
        Assert.True(log.HasErrors, $"No errors while expecting: {log.Output}");
        AssertHelper.Equals(@"error: The template property `changelog.name_template` has template errors:
<input>(0,0) : error : Error while parsing binary expression: Expecting an <expression> to the right of the operator instead of `<eof>` in: <expression> operator <expression>", log.Output.ToString().Trim());
    }

    [Test]
    public async Task TestErrorRunningTemplate()
    {
        var (log, result) = await CreateChangelog(GetBigChangesImpl, configuration =>
        {
            configuration.NameTemplate = "{{ invalid test }} ";
        });
        Assert.True(log.HasErrors, $"No errors while expecting: {log.Output}");
        AssertHelper.Equals(@"error: Error while rendering template name. Reason: <input>(1,4) : error : The function `invalid` was not found.", log.Output.ToString().Trim());
    }

    private async Task AssertTemplate(string expected, MockDevHosting.GetChangesDelegate getChangesImpl, Action<ChangelogConfiguration>? configure = null)
    {

        var (log, result) = await CreateChangelog(getChangesImpl, configure);
        Assert.False(log.HasErrors, $"Invalid errors: {log.Output}");
        Assert.NotNull(result);
        Assert.AreEqual("1.0.0", result!.Title);
        AssertHelper.Equals(expected, result.Body);
    }

    private ChangelogCollection? GetBranchAndFilesDispatchChangesImpl(IDevHosting hosting, string user, string repo, string tagPrefix, string version)
    {
        var changeCollection = DefaultChangelogCollection(hosting, user, repo, tagPrefix, version);

        changeCollection.AddPullRequestChange(1, "yo1", "This is a change of doc 1", "Yeah doc", "mister_pr_doc", Array.Empty<string>(), new string[] { "/doc/readme.md"});
        changeCollection.AddPullRequestChange(2, "another_repo:special_branch1", "This is not a change of doc 2", "A special 125 comment in the body", "mister_pr_misc", Array.Empty<string>(), new string[] { "/files/readme.md" });

        return changeCollection;
    }

    private ChangelogCollection? GetBigChangesImpl(IDevHosting hosting, string user, string repo, string tagPrefix, string version)
    {
        var changeCollection = DefaultChangelogCollection(hosting, user, repo, tagPrefix, version);

        // Check PR with labels
        changeCollection.AddPullRequestChange(1, "yo1", "Maybe support for xyz", "Yeah support", "mister_pr_feature", new[] { "feature" }, Array.Empty<string>());
        // PR with auto-labeler
        changeCollection.AddPullRequestChange(2, "yo2", "Add example for alpha", "Yeah example", "mister_pr_example", Array.Empty<string>(), Array.Empty<string>());
        // PR with owner
        changeCollection.AddPullRequestChange(3, "yo3", "Add tests for abc", "Yeah tests", "xoofx", Array.Empty<string>(), Array.Empty<string>());

        // Check auto defaults auto-labelers

        // Check for autolabeler: breaking-change
        changeCollection.AddCommitChange("Breaking change of feature x", "This is a breaking change", "mister_breaking", "8ec9e7a1c6874535bcc2e004b307baba");
        // Check for autolabeler: maintenance
        changeCollection.AddCommitChange("Fix ci", "This is ci change", "mister_ci", "d8b26fb18214416b8de110eb68a7403d");
        // Check for autolabeler: bugfix
        changeCollection.AddCommitChange("Fix issue with blablabla", "This is fixing", "mister_bug", "6ec0a0ea3aae47ffad7eeaad9f879fee");
        // Check for autolabeler: documentation
        changeCollection.AddCommitChange("Add documentation", "Doc entry", "mister_doc", "ac9c7bcfe9f847c79c65783e41236f32");
        // Check for autolabeler: test
        changeCollection.AddCommitChange("Add tests", "Test entry", "mister_test", "647741349dbb412cbb98106e5f4366ba");
        // Check for autolabeler: examples
        changeCollection.AddCommitChange("Improve example", "Test entry", "mister_example", "51bfc6bcf06048f1a205696248dfe6ef");
        // Check for autolabeler: accessibility
        changeCollection.AddCommitChange("Fix accessibility", "Another fix", "mister_accessibility", "d8d88f20239a45cc90710fc1b466aa96");
        // Check for autolabeler: feature
        changeCollection.AddCommitChange("Add support for new feature y", "Feature y", "mister_feature", "3608a3916b97438882cc61eac36f4a7c");
        // Check for autolabeler: enhancement
        changeCollection.AddCommitChange("Improve performance of xyz", "Perf xyz", "mister_perf", "d4563ad7e5b94327a5b37220d37d6eea");
        // Check for autolabeler: dependencies
        changeCollection.AddCommitChange("Update dependency of package xyz/0.1.2", "Update package", "mister_deps", "4c08f7801ba2468caff457725a7f3f28");
        // Check for Misc
        changeCollection.AddCommitChange("Go to misc", "This is a misc", "mister_misc", "47b1ecf3314740df856843ef4f03370a");
        // Check for Misc with owner
        changeCollection.AddCommitChange("Go to misc but it's the owner", "This is a misc", "xoofx", "47b1ecf3314740df856843ef4f03370a");

        return changeCollection;
    }

    private ChangelogCollection DefaultChangelogCollection(IDevHosting hosting, string user, string repo, string tagPrefix, string version)
    {
        var changeCollection = new ChangelogCollection();

        var fromVersion = "0.1.3";
        changeCollection.Version = new ChangelogVersionModel(tagPrefix, NuGetVersion.Parse(version), "ef88fe8e57c74d7e9da706b88bf97812");
        changeCollection.PreviousVersion = new ChangelogVersionModel(tagPrefix, NuGetVersion.Parse(fromVersion), "afb75e5b25474c9e8baea5327a03f34a");
        changeCollection.CompareUrl = hosting.GetCompareUrl(user, repo, fromVersion, version);
        return changeCollection;
    }

    private async ValueTask<(MockSimpleLogger log, ChangelogResult? result)> CreateChangelog(MockDevHosting.GetChangesDelegate getChangesImpl, Action<ChangelogConfiguration>? configure = null)
    {
        var config = new ChangelogConfiguration();
        config.Owners.Add("xoofx");
        configure?.Invoke(config);
        config.AddDefaults();

        var log = new MockSimpleLogger();
        var builder = new ChangelogBuilder(config, log);

        var devHosting = new MockDevHosting(log, new GitHubDevHostingConfiguration()
        {
            User = "xoofx",
            Repo = "dotnet-releaser"
        })
        {
            GetChangesImpl = getChangesImpl
        };

        var result = await builder.Generate(devHosting, "1.0.0");
        return (log, result);
    }
}