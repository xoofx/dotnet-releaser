using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DotNetReleaser.Changelog;
using DotNetReleaser.Configuration;
using DotNetReleaser.Coverage;
using DotNetReleaser.Helpers;
using DotNetReleaser.Logging;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using Spectre.Console.Rendering;

namespace DotNetReleaser;

/// <summary>
/// Main app that handles
/// - Update release from changelog
/// - Build NuGet package
/// - Build single exe file with archives and packages
/// - Update Release on GitHub with all assets
/// - Update homebrew
/// - Push the NuGet package
/// - Push all platform packages
/// </summary>
public partial class ReleaserApp
{
    private static readonly string DotNetReleaserConfigFile = Path.Combine(AppContext.BaseDirectory, ReleaserConstants.DotNetReleaserFileName);
    
    private readonly ISimpleLogger _logger;
    private ReleaserConfiguration _config;

    private ReleaserApp(ISimpleLogger logger)
    {
        _logger = logger;
        _config = new ReleaserConfiguration();
        _assemblyCoverages = new List<AssemblyCoverage>();
    }

    /// <summary>
    /// Main entry for the releaser. Parser the argument and delegate to <see cref="RunImpl"/>
    /// </summary>
    /// <param name="args">The command line arguments</param>
    /// <returns>0 if successful; 1 otherwise.</returns>
    public static async Task<int> Run(string[] args)
    {
        // Create our log
        using var factory = LoggerFactory.Create(configure =>
        {
            configure.AddProvider(new SpectreConsoleLoggerProvider(new SpectreConsoleLoggerOptions()
            {
                IndentAfterNewLine = false,
                IncludeTimestamp = true,
                IncludeNewLine = false,
                IncludeCategory = false
            }));
        });
        var exeName = "dotnet-releaser";
        var logger = SimpleLogger.CreateConsoleLogger(factory, exeName);
        var appReleaser = new ReleaserApp(logger);
        var version = typeof(ReleaserApp).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "?.?.?";

        var app = new CommandLineApplication
        {
            Name = exeName,
        };

        app.VersionOption("--version", $"{app.Name} {version} - {DateTime.Now.Year} (c) Copyright Alexandre Mutel", version);
        app.HelpOption(inherited: true);
        app.Command("publish", AddPublishOrBuildArgs);
        app.Command("build", AddPublishOrBuildArgs);

        app.Command("new", newCommand =>
            {
                newCommand.Description = "Create a dotnet-releaser TOML configuration file for a specified project.";
                var configurationFileArg = AddTomlConfigurationArgument(newCommand, false);
                var projectOption = newCommand.Option<string>("--project <project_file>", "A - relative - path to project file (csproj, vbproj, fsproj)", CommandOptionType.SingleValue).IsRequired();
                var userOption = newCommand.Option<string>("--user <GitHub_user/org>", "The GitHub user/org where the packages will be published", CommandOptionType.SingleValue);
                var repoOption = newCommand.Option<string>("--repo <GitHub_repo>", "The GitHub repo name where the packages will be published", CommandOptionType.SingleValue);
                var forceOption = newCommand.Option<bool>("--force", "Force overwriting the existing TOML configuration file.", CommandOptionType.NoValue);

                newCommand.OnExecuteAsync(async token =>
                    {
                        var result = await appReleaser.CreateConfigurationFile(configurationFileArg.ParsedValue, projectOption.ParsedValue, userOption.ParsedValue, repoOption.ParsedValue, forceOption.ParsedValue);
                        return result ? 0 : 1;
                    }
                );
            }
        );

        app.Command("changelog", changelogCommand =>
        {
            changelogCommand.Description = "Generate changelog for the specified GitHub owner/repository and optionally upload them back.";

            var configurationFileArg = AddTomlConfigurationArgument(changelogCommand, false);
            var versionArgument = changelogCommand.Argument("version", "An optional version to generate the changelog for. If it is not defined, it will fetch all existing tags and generate the logs for them.");
            var updateOption = changelogCommand.Option<bool>("--update", "Update the changelog on GitHub for the specified version or all versions if no versions are specified.", CommandOptionType.NoValue);
            var githubToken = AddGitHubToken(changelogCommand).IsRequired();

            changelogCommand.OnExecuteAsync(async (token) =>
            {
                var result = await appReleaser.ListOrUpdateChangelog(configurationFileArg.ParsedValue, githubToken.ParsedValue, versionArgument.Value ?? string.Empty, updateOption.ParsedValue);
                return result ? 0 : 1;
            });
        });

        CommandOption<string> AddGitHubToken(CommandLineApplication cmd)
        {
            return cmd.Option<string>("--github-token <token>", "GitHub Api Token. Required if publish to GitHub is true in the config file", CommandOptionType.SingleValue);
        }

        CommandArgument<string> AddTomlConfigurationArgument(CommandLineApplication cmd, bool forNew)
        {
            var arg = cmd.Argument<string>("dotnet-releaser.toml", forNew ? "TOML configuration file path to create. Default is: dotnet-releaser.toml" : "The input TOML configuration file.");
            if (!forNew) arg = arg.IsRequired();
            return arg;
        }

        void AddPublishOrBuildArgs(CommandLineApplication cmd)
        {
            CommandOption<string>? githubToken = null;
            CommandOption<string>? nugetToken = null;

            githubToken = AddGitHubToken(cmd);

            if (cmd.Name == "publish")
            {
                cmd.Description = "Build and publish the project.";
                nugetToken = cmd.Option<string>("--nuget-token <token>", "NuGet Api Token. Required if publish to NuGet is true in the config file", CommandOptionType.SingleValue);
            }
            else
            {
                cmd.Description = "Build only the project.";
            }

            var forceOption = cmd.Option<bool>("--force", "Force deleting and recreating the artifacts folder.", CommandOptionType.NoValue);
            var configurationFileArg = AddTomlConfigurationArgument(cmd, false);

            cmd.OnExecuteAsync(async (token) =>
            {
                // Check configuration file
                var configurationFilePath = configurationFileArg.ParsedValue;
                var buildKind = cmd.Name == "publish" ? BuildKind.Publish : BuildKind.Build;
                var result = await appReleaser.RunImpl(configurationFilePath, buildKind, githubToken.ParsedValue ?? string.Empty, nugetToken?.ParsedValue, forceOption.ParsedValue);
                return result ? 0 : 1;
            });
        }

        app.OnExecute(() =>
        {
            Console.WriteLine("Specify a sub-command");
            app.ShowHelp();
            return 1;
        });

        int result = 0;
        try
        {
            result = await app.ExecuteAsync(args);
        }
        catch (Exception exception)
        {
            var backColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;

            if (exception is UnrecognizedCommandParsingException unrecognizedCommandParsingException)
            {
                await Console.Out.WriteLineAsync($"{unrecognizedCommandParsingException.Message} for command {unrecognizedCommandParsingException.Command.Name}");
            }
            else
            {
                await Console.Out.WriteLineAsync($"Unexpected error {exception.Message}");
            }
            Console.ForegroundColor = backColor;
            result = 1;
        }

        return result;
    }

    private async Task<bool> LoadConfiguration(string configurationFile)
    {
        // ------------------------------------------------------------------
        // Load Configuration
        // ------------------------------------------------------------------
        var configuration = await ReleaserConfiguration.From(configurationFile, _logger);
        if (configuration is null) return false;
        _config = configuration;

        // Don't continue if we had errors when deserializing the config file
        return !HasErrors;
    }

    /// <summary>
    /// Runs the releaser app
    /// </summary>
    private async Task<bool> RunImpl(string configurationFile, BuildKind buildKind, string githubApiToken, string? nugetApiToken, bool forceArtifactsFolder)
    {
        _logger.LogStartGroup("Initializing");
        BuildInformation? buildInformation = null;
        GitHubDevHostingConfiguration? hostingConfiguration = null;
        IDevHosting? devHosting = null;
        ChangelogResult? changelog = null;
        try
        {
            // ------------------------------------------------------------------
            // Load Configuration
            // ------------------------------------------------------------------
            if (!await LoadConfiguration(configurationFile)) return false;

            if (!EnsureArtifactsFolders(forceArtifactsFolder)) return false;

            // ------------------------------------------------------------------
            // Load Package Information from MSBuild project
            // ------------------------------------------------------------------
            buildInformation = await LoadProjects();
            if (HasErrors) return false;

            // ------------------------------------------------------------------
            // Validate Publish parameters
            // ------------------------------------------------------------------
            hostingConfiguration = _config.GitHub;

            // Connect to GitHub if we have a token
            if (!string.IsNullOrEmpty(githubApiToken))
            {
                devHosting = await ConnectToDevHosting(hostingConfiguration, githubApiToken);
                if (devHosting is null)
                {
                    return false;
                }
            }

            if (buildKind == BuildKind.Publish)
            {
                if (hostingConfiguration.Publish)
                {
                    if (string.IsNullOrEmpty(githubApiToken))
                    {
                        Error($"Publishing to {hostingConfiguration.Provider} requires to pass --github-token");
                        return false;
                    }
                }

                if (string.IsNullOrEmpty(nugetApiToken))
                {
                    Error("Publishing to NuGet requires to pass --nuget-token");
                    return false;
                }
            }

            // Update homebrew config (and log if necessary)
            foreach (var projectPackageInfo in buildInformation.GetAllPackableProjects())
            {
                UpdateHomebrewConfigurationFromPackage(projectPackageInfo);
            }

            // ------------------------------------------------------------------
            // Parse Changelog
            // ------------------------------------------------------------------
            if (_config.Changelog.Publish && devHosting is not null)
            {
                changelog = await CreateChangeLog(devHosting, buildInformation.Version);
                if (changelog is not null)
                {
                    Info($"Changelog:{Environment.NewLine}{changelog}");
                }
                else if (HasErrors)
                {
                    return false;
                }
                else
                {
                    Warn("No changelog found or configured.");
                }
            }
        }
        finally
        {
            _logger.LogEndGroup();
        }

        // ------------------------------------------------------------------
        // Build Projects (debug/release)
        // ------------------------------------------------------------------
        if (!await BuildAndTest(buildInformation)) return false;

        // ------------------------------------------------------------------
        // Build NuGet package
        // ------------------------------------------------------------------
        if (!await BuildNuGetPackage(buildInformation)) return false;

        // ------------------------------------------------------------------
        // Build executable packages (deb, zip, rpm, tar...)
        // ------------------------------------------------------------------
        foreach (var packageInfo in buildInformation.GetAllPackableProjects())
        {
            var entriesToPublish = new List<PackageEntry>();

            var builder = new StringBuilder();
            bool hasPackagesToBuild = false;
            foreach (var pack in _config.Packs)
            {
                foreach (var rid in pack.RuntimeIdentifiers)
                {
                    if (pack.Publish) hasPackagesToBuild = true;
                    builder.AppendLine($"Build configured for `{packageInfo.Name}` and {PackagingConfiguration.ToStringRidAndKinds(new() { rid }, pack.Kinds)}");
                }
            }

            // Don't log an empty line
            if (builder.Length > 0)
            {
                Info(builder.ToString());
            }

            if (hasPackagesToBuild)
            {
                Info($"Begin building platform packages for `{packageInfo.Name}`...");
                foreach (var pack in _config.Packs)
                {
                    foreach (var rid in pack.RuntimeIdentifiers)
                    {
                        var list = await PackPlatform(packageInfo, pack.Publish, rid, pack.Kinds.ToArray());
                        if (HasErrors) goto exitPackOnError; // break on first errors

                        if (list is not null && pack.Publish)
                        {
                            entriesToPublish.AddRange(list);
                        }
                    }
                }

                exitPackOnError:
                if (HasErrors)
                {
                    Error($"Error while building platform packages for `{packageInfo.Name}`.");
                }
                else
                {
                    Info($"End building platform packages successful for `{packageInfo.Name}`.");
                }
            }
            else
            {
                Info($"No platform packages to build for `{packageInfo.Name}`.");
            }

            // Exit if we have any errors.
            if (HasErrors)
            {
                return false;
            }

            // ------------------------------------------------------------------
            // Publish all packages NuGet + (deb, zip, rpm, tar...)
            // ------------------------------------------------------------------
            // Draft if we are just building and not publishing (to allow to update the changelog)
            var releaseVersion = new ReleaseVersion(buildInformation.Version, IsDraft: buildKind == BuildKind.Build, $"{hostingConfiguration.VersionPrefix}{buildInformation.Version}");

            if (buildKind == BuildKind.Publish)
            {
                if (nugetApiToken is not null)
                {
                    await PublishNuGet(packageInfo, nugetApiToken);
                }

                // Don't try to continue publishing if we had errors with NuGet publishing
                // Otherwise publish any packages that we have generated before
                if (!HasErrors && devHosting is not null)
                {
                    // In the case of a build, we still want to upload a draft release notes
                    await devHosting.UpdateChangelogAndUploadPackages(hostingConfiguration.User, hostingConfiguration.Repo, releaseVersion, changelog, entriesToPublish, _config.EnablePublishPackagesInDraft);

                    if (!HasErrors && _config.Brew.Publish)
                    {
                        var brewFormula = HomebrewHelper.CreateFormula(devHosting, packageInfo, entriesToPublish);

                        if (brewFormula is not null)
                        {
                            await devHosting.UploadHomebrewFormula(hostingConfiguration.User, _config.Brew.Home, packageInfo, brewFormula);
                        }
                    }
                }

            }
            else if (buildKind == BuildKind.Build && devHosting is not null && !_config.Changelog.DisableDraftForBuild)
            {
                await devHosting.UpdateChangelogAndUploadPackages(hostingConfiguration.User, hostingConfiguration.Repo, releaseVersion, changelog, entriesToPublish, _config.EnablePublishPackagesInDraft);
            }
        }

        return !HasErrors;
    }

    public bool HasErrors => _logger.HasErrors;

    public void Info(string message)
    {
        _logger.Info(message);
    }

    public void Info(string message, IRenderable renderable)
    {
        _logger.InfoMarkup(message, renderable);
    }

    public void Warn(string message)
    {
        _logger.Warn(message);
    }

    public void Error(string message)
    {
        _logger.Error(message);
    }

    enum BuildKind
    {
        None,
        Publish,
        Build,
    }

    private class AppException : Exception
    {
        public AppException(string message) : base(message)
        {
        }
    }
}