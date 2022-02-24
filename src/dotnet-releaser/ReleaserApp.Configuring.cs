using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CliWrap;
using DotNetReleaser.Helpers;
using DotNetReleaser.Logging;
using Spectre.Console;

namespace DotNetReleaser;

public partial class ReleaserApp
{
    private async Task<(BuildInformation? buildInformation, IDevHosting? devHosting)?> Configuring(string configurationFile, BuildKind buildKind, string githubApiToken, string? nugetApiToken, bool forceArtifactsFolder)
    {
        // ------------------------------------------------------------------
        // Load Configuration
        // ------------------------------------------------------------------
        if (!await LoadConfiguration(configurationFile)) return null; // return false;

        if (!EnsureArtifactsFolders(forceArtifactsFolder)) return null; // return false;

        // ------------------------------------------------------------------
        // Load Package Information from MSBuild project
        // ------------------------------------------------------------------
        var buildInformation = await LoadProjects();
        if (buildInformation is null || HasErrors) return null; // return false;

        // ------------------------------------------------------------------
        // Validate Publish parameters
        // ------------------------------------------------------------------
        var hostingConfiguration = _config.GitHub;

        IDevHosting? devHosting = null;

        // Connect to GitHub if we have a token
        if (!string.IsNullOrEmpty(githubApiToken))
        {
            devHosting = await ConnectToDevHosting(hostingConfiguration, githubApiToken);
            if (devHosting is null)
            {
                return null; // return false;
            }
        }

        // We require a branch name if we need to publish a changelog (for the draft version)
        // or we need to do an automatic run on GitHub Action.
        var requiresDraftForBuild = buildKind == BuildKind.Build && !string.IsNullOrEmpty(githubApiToken) && !_config.Changelog.DisableDraftForBuild;
        var requiringBranchName = buildKind == BuildKind.Publish && hostingConfiguration.Publish
                                  || requiresDraftForBuild
                                  || buildKind == BuildKind.Run;
        bool validateBranchName = false;

        if (requiringBranchName)
        {
            var branchName = await GetCurrentBranchName();
            if (branchName is null) return null;
            buildInformation.CurrentBranchName = branchName;
        }

        // Fetch current branch name
        if (buildKind == BuildKind.Run)
        {
            if (devHosting is null)
            {
                Error("Missing GitHub API token. The command `run` is only supported to run from a GitHub Action with a valid --github-api-token.");
                return null;
            }

            var gitHubInfo = GitHubActionHelper.GetInfo();
            if (gitHubInfo is null)
            {
                Error("Invalid usage of command `run`. This command is only supported to run from a GitHub Action");
                return null;
            }

            // Automatically convert a run into a publish if we have a release tag
            if (gitHubInfo.EventName == "push" && gitHubInfo.RefType == GitHubActionRefType.Tag)
            {
                var regexVersion = new Regex(@$"^{hostingConfiguration.VersionPrefix}\d+(\.\d+)*");
                if (regexVersion.IsMatch(gitHubInfo.RefName))
                {
                    _logger.InfoMarkup($"The tag `{Markup.Escape(gitHubInfo.RefName)}` is identified as a release tag. [green on black]Publish mode[/] selected.");
                    buildKind = BuildKind.Publish;
                }
                else
                {
                    _logger.WarnMarkup($"The tag {Markup.Escape(gitHubInfo.RefName)} is not identified as a release tag. [green on black]Build only mode[/] selected.");
                    buildKind = BuildKind.Build;
                }
            }
            else
            {
                _logger.InfoMarkup(gitHubInfo.EventName == "push"
                    ? $"The trigger event is `{Markup.Escape(gitHubInfo.EventName)}` and the branch `{Markup.Escape(gitHubInfo.RefName)}`. [green on black]Build only mode[/] selected."
                    : $"The trigger event is `{Markup.Escape(gitHubInfo.EventName)}`. [green on black]Build only mode[/] selected.");
                buildKind = BuildKind.Build;
            }
        }

        if (buildKind == BuildKind.Publish)
        {
            if (hostingConfiguration.Publish)
            {
                if (string.IsNullOrEmpty(githubApiToken))
                {
                    Error($"Publishing to {hostingConfiguration.Provider} requires to pass --github-token");
                    return null; // return false;
                }

                validateBranchName = true;
                buildInformation.AllowPublishDraft = true;
            }

            if (string.IsNullOrEmpty(nugetApiToken))
            {
                Error("Publishing to NuGet requires to pass --nuget-token");
                return null; // return false;
            }
        }

        // Verifies that the branch is a supported branch for releases
        if (validateBranchName)
        {
            var branchName = buildInformation.CurrentBranchName!;
            if (!_config.GitHub.Branches.Contains(branchName))
            {
                Error($"The current git branch `{branchName}` is not listed in the authorized release branches from the configuration `github.branches = [{string.Join(", ", _config.GitHub.Branches)}]`");
                return null;
            }
        }

        // Allow to generate a draft only when building from release branches
        if (requiresDraftForBuild)
        {
            var branchName = buildInformation.CurrentBranchName!;
            if (_config.GitHub.Branches.Contains(branchName))
            {
                buildInformation.AllowPublishDraft = true;
            }
        }

        // Store the build kind
        buildInformation.BuildKind = buildKind;

        return (buildInformation, devHosting);
    }


    private async Task<string?> GetCurrentBranchName()
    {
        var stdOutAndErrorBuffer = new StringBuilder();
        var result = await Cli.Wrap("git")
            .WithArguments(new []{ "branch","--show-current"})
            .WithWorkingDirectory(Environment.CurrentDirectory)
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutAndErrorBuffer))
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdOutAndErrorBuffer))
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync();

        if (result.ExitCode != 0)
        {
            Error($"Error while executing `git branch --show-current`. Reason: {stdOutAndErrorBuffer}");
            return null;
        }

        var branchName = stdOutAndErrorBuffer.ToString().Trim();
        if (string.IsNullOrEmpty(branchName))
        {
            Error(@"Unable retrieve the current branch with `git branch --show-current`. The current action requires it. Please make sure that:
1) The current commit is a checkout of a branch.
2) If running on GitHub Action, you are using `actions/checkout@v2` and not v1.");
            return null;
        }

        return branchName;
    }
}