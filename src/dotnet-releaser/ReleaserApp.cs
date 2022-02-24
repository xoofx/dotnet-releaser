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
using Spectre.Console;
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
            var runningOnGitHub = GitHubActionHelper.GetInfo() != null;
            IAnsiConsoleOutput consoleOut = new AnsiConsoleOutput(Console.Out);
            if (runningOnGitHub)
            {
                consoleOut = new AnsiConsoleOutputOverride(consoleOut)
                {
                    Width = 256,
                    Height = 128,
                };
            }
            
            configure.AddProvider(new SpectreConsoleLoggerProvider(new SpectreConsoleLoggerOptions()
            {
                ConsoleSettings = runningOnGitHub ? new AnsiConsoleSettings()
                {
                    Ansi = AnsiSupport.No,
                    Out = consoleOut
                } : new AnsiConsoleSettings()
                {
                    Out = consoleOut
                },
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
        app.Command("run", AddPublishOrBuildArgs);

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

            if (cmd.Name == "publish" || cmd.Name == "run")
            {
                cmd.Description = cmd.Name == "run" ? "Automatically build and publish a project when running from a GitHub Action based on which branch is active, if there is a tag (for publish), and if the change is a `push`." : "Build and publish the project.";
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
                var buildKind = cmd.Name switch
                {
                    "run" => BuildKind.Run,
                    "publish" => BuildKind.Publish,
                    _ => BuildKind.Build
                };
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

        VerifyWhenRunningFromGitHubAction();

        // Don't continue if we had errors when deserializing the config file
        return !HasErrors;
    }

    private void VerifyWhenRunningFromGitHubAction()
    {
        var info = GitHubActionHelper.GetInfo();
        if (info is null) return;

        Info($"Running from GitHub: {info}");
        
        //if (_config.GitHub.User != info.OwnerName)
        //{
        //    Error($"Invalid GitHub user|owner defined in configuration file `{_config.GitHub.User}`. Expecting {info.OwnerName}");
        //}

        //if (_config.GitHub.Repo != info.RepoName)
        //{
        //    Error($"Invalid GitHub repository defined in configuration file `{_config.GitHub.Repo}`. Expecting {info.RepoName}");
        //}
    }

    /// <summary>
    /// Runs the releaser app
    /// </summary>
    private async Task<bool> RunImpl(string configurationFile, BuildKind buildKind, string githubApiToken, string? nugetApiToken, bool forceArtifactsFolder)
    {
        BuildInformation? buildInformation = null;
        GitHubDevHostingConfiguration? hostingConfiguration = null;
        IDevHosting? devHosting = null;
        ChangelogResult? changelog = null;
        try
        {
            _logger.LogStartGroup("Configuring");
            var result = await Configuring(configurationFile, buildKind, githubApiToken, nugetApiToken, forceArtifactsFolder);
            if (result is null) return false;
            buildInformation = result.Value.buildInformation!;
            devHosting = result.Value.devHosting;
            hostingConfiguration = _config.GitHub;
        }
        finally
        {
            _logger.LogEndGroup();
        }

        // ------------------------------------------------------------------
        // Parse Changelog
        // ------------------------------------------------------------------
        if (_config.Changelog.Publish && devHosting is not null)
        {
            try
            {
                _logger.LogStartGroup("Preparing Changelog");
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
            finally
            {
                _logger.LogEndGroup();
            }
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
        var buildPackages = await BuildAppPackages(buildInformation);
        if (buildPackages is null) return false;

        // ------------------------------------------------------------------
        // Publish all packages NuGet + (deb, zip, rpm, tar...)
        // ------------------------------------------------------------------
        // Draft if we are just building and not publishing (to allow to update the changelog)
        await PublishPackagesAndChangelog(nugetApiToken, buildInformation, hostingConfiguration, buildPackages, devHosting, changelog);

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

    private class AppException : Exception
    {
        public AppException(string message) : base(message)
        {
        }
    }
}