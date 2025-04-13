using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DotNetReleaser.Changelog;
using DotNetReleaser.Configuration;
using DotNetReleaser.Coverage;
using DotNetReleaser.Helpers;
using DotNetReleaser.Logging;
using Lunet.Extensions.Logging.SpectreConsole;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Build.Locator;
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
    private TableBorder _tableBorder;
    private bool _skipAppPackagesForBuildOnly;

    private ReleaserApp(ISimpleLogger logger)
    {
        ExeName = "dotnet-releaser";
        _logger = logger;
        _config = new ReleaserConfiguration();
        _assemblyCoverages = new List<AssemblyCoverage>();
        _tableBorder = TableBorder.Square;
        Version = typeof(ReleaserApp).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "?.?.?";
    }

    public string ExeName { get; }

    public string Version { get; }

    /// <summary>
    /// Main entry for the releaser. Parser the argument and delegate to <see cref="RunImpl"/>
    /// </summary>
    /// <param name="args">The command line arguments</param>
    /// <returns>0 if successful; 1 otherwise.</returns>
    public static async Task<int> Run(string[] args)
    {
        MSBuildLocator.RegisterDefaults();

        Console.OutputEncoding = Encoding.UTF8;
        // Create our log
        var runningOnGitHubAction = GitHubActionHelper.IsRunningOnGitHubAction;
        using var factory = LoggerFactory.Create(configure =>
        {
            IAnsiConsoleOutput consoleOut = new AnsiConsoleOutput(Console.Out);
            if (runningOnGitHubAction)
            {
                consoleOut = new AnsiConsoleOutputOverride(consoleOut)
                {
                    Width = 256,
                    Height = 128,
                };
            }
            
            configure.AddSpectreConsole(new SpectreConsoleLoggerOptions()
            {
                ConsoleSettings = runningOnGitHubAction ? new AnsiConsoleSettings()
                {
                    Ansi = AnsiSupport.No,
                    Out = consoleOut
                } : new AnsiConsoleSettings()
                {
                    Out = consoleOut
                },
                IndentAfterNewLine = false,
                IncludeTimestamp = true,
                IncludeNewLineBeforeMessage = false,
                IncludeCategory = false
            });
        });
        var exeName = "dotnet-releaser";
        var logger = SimpleLogger.CreateConsoleLogger(factory, exeName);
        var appReleaser = new ReleaserApp(logger);

        // -----------------------------------------------------------------
        // Workaround with a PowerShell limitation that is stripping empty arguments "" from passing them to dotnet-releaser
        // See issue https://github.com/PowerShell/PowerShell/issues/1995
        // In order to protect against such error, we emit a more detailed error for the obvious cases with some guidance.
        // -----------------------------------------------------------------
        var previousArg = string.Empty;
        var protectedArgs = new[] { "--github-token", "--nuget-token", "--github-token-extra" };
        foreach (var arg in args)
        {
            if (protectedArgs.Contains(previousArg) && (protectedArgs.Contains(arg) || arg.EndsWith(".toml", StringComparison.OrdinalIgnoreCase)))
            {
                appReleaser.Error($"Invalid argument passed `{previousArg} {arg}` (All arguments: {string.Join(" ", args)}). Check that you are not passing an empty string to the argument `{previousArg}`. If you are using PowerShell running on GitHub Action, please use bash instead to avoid such limitation.");
                break;
            }
            previousArg = arg;
        }

        // Early exit
        if (appReleaser.HasErrors)
        {
            return 1;
        }

        // -----------------------------------------------------------------
        // Declare command line arguments
        // -----------------------------------------------------------------
        var app = new CommandLineApplication
        {
            Name = exeName,
        };

        app.VersionOption("--version", $"{app.Name} {appReleaser.Version} - {DateTime.Now.Year} (c) Copyright Alexandre Mutel", appReleaser.Version);
        app.HelpOption(inherited: true);
        app.Command("publish", AddPublishOrBuildArgs);
        app.Command("build", AddPublishOrBuildArgs);
        app.Command("run", AddPublishOrBuildArgs);

        app.Command("new", newCommand =>
            {
                newCommand.Description = "Create a dotnet-releaser TOML configuration file for a specified project.";
                var configurationFileArg = AddTomlConfigurationArgument(newCommand, true);
                var projectOption = newCommand.Option<string>("--project <project_file>", "A - relative - path to a solution file (.sln) or project file (.csproj, .fsproj, .vbproj). By default, it will try to find a solution file where this command is run or where the output dotnet-releaser.toml file is specified.", CommandOptionType.SingleValue);
                var userOption = newCommand.Option<string>("--user <GitHub_user/org>", "The GitHub user/org where the packages will be published. If not specified, it will try to detect automatically if there is a git repository configured from the folder (and parents) of the TOML configuration file, and extract any git remote that could give this information.", CommandOptionType.SingleValue);
                var repoOption = newCommand.Option<string>("--repo <GitHub_repo>", "The GitHub repo name where the packages will be published. If not specified, it will try to detect automatically if there is a git repository configured from the folder (and parents) of the TOML configuration file, and extract any git remote that could give this information.", CommandOptionType.SingleValue);
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

        CommandOption<string> AddGitHubTokenExtra(CommandLineApplication cmd)
        {
            return cmd.Option<string>("--github-token-extra <token>", "GitHub Api Token. Required if publish homebrew to GitHub is true in the config file. In that case dotnet-releaser needs a personal access GitHub token which can create the homebrew repository. This token has usually more access than the --github-token that is only used for the current repository. ", CommandOptionType.SingleValue);
        }

        CommandOption<string> AddGitHubTokenGist(CommandLineApplication cmd)
        {
            return cmd.Option<string>("--github-token-gist <token>", "GitHub Api Token. Required if publishing to a gist used for e.g coverage.", CommandOptionType.SingleValue);
        }

        CommandArgument<string> AddTomlConfigurationArgument(CommandLineApplication cmd, bool forNew)
        {
            var arg = cmd.Argument<string>("dotnet-releaser.toml", forNew ? "TOML configuration file path to create. Default is: dotnet-releaser.toml" : "The input TOML configuration file.");
            if (!forNew) arg = arg.IsRequired();
            return arg;
        }

        void AddPublishOrBuildArgs(CommandLineApplication cmd)
        {
            CommandOption<string>? nugetToken = null;
            CommandOption<string>? gitHubTokenExtra = null;
            CommandOption<string>? gitHubTokenGist = null;
            CommandOption<bool>? skipAppPackagesOption = null;

            var githubToken = AddGitHubToken(cmd);

            if (cmd.Name == "publish" || cmd.Name == "run")
            {
                cmd.Description = cmd.Name == "run" ? "Automatically build and publish a project when running from a GitHub Action based on which branch is active, if there is a tag (for publish), and if the change is a `push`." : "Build and publish the project.";
                nugetToken = cmd.Option<string>("--nuget-token <token>", "NuGet Api Token. Required if publish to NuGet is true in the config file", CommandOptionType.SingleValue);

                gitHubTokenExtra = AddGitHubTokenExtra(cmd);
                gitHubTokenGist = AddGitHubTokenGist(cmd);
            }
            else
            {
                cmd.Description = "Build only the project.";
            }

            if (cmd.Name == "run" || cmd.Name == "build")
            {
                skipAppPackagesOption = cmd.Option<bool>("--skip-app-packages-for-build-only",
                    "Skip building application packages (e.g tar) when building only (but not publishing). This is useful when running on a CI and you want to build app packages only when publishing.", CommandOptionType.NoValue);
            }

            var tableKindOption = cmd.Option<TableBorderKind>("--table", "Specifies the rendering of the tables. Default is square.", CommandOptionType.SingleValue);
            tableKindOption.DefaultValue = TableBorderKind.Square;

            var forceOption = cmd.Option<bool>("--force", "Force deleting and recreating the artifacts folder.", CommandOptionType.NoValue);

            CommandOption<bool>? forceUploadOption = null;
            CommandOption<string>? publishVersion = null;
            if (cmd.Name == "publish")
            {
                publishVersion = cmd.Option<string>("--version <version>", "Tag version used when publishing the changelog and creating the release tag.", CommandOptionType.SingleValue);
                forceUploadOption = cmd.Option<bool>("--force-upload", "Force uploading the release assets.", CommandOptionType.NoValue);
            }

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
                appReleaser._skipAppPackagesForBuildOnly = skipAppPackagesOption?.ParsedValue ?? false;
                if (tableKindOption.HasValue())
                {
                    appReleaser._tableBorder = GetTableBorderFromKind(tableKindOption.ParsedValue);
                }
                var result = await appReleaser.RunImpl(configurationFilePath, buildKind, githubToken.ParsedValue, gitHubTokenExtra?.ParsedValue, gitHubTokenGist?.ParsedValue, nugetToken?.ParsedValue, forceOption.ParsedValue, forceUploadOption?.ParsedValue ?? false, publishVersion?.ParsedValue);
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

        // Try to mitigate issue with structured logs
        if (runningOnGitHubAction)
        {
            // Wait for a small amount of time to make sure that output is completely flushed
            await Task.Delay(16);
            await Console.Out.FlushAsync();
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
    private async Task<bool> RunImpl(string configurationFile, BuildKind buildKind, string githubApiToken, string? githubApiTokenExtra, string? gitHubTokenGist, string? nugetApiToken, bool forceArtifactsFolder, bool forceUpload,
        string? publishVersion)
    {
        BuildInformation? buildInformation = null;
        GitHubDevHostingConfiguration? hostingConfiguration = null;
        IDevHosting? devHosting = null;
        IDevHosting? devHostingExtra = null;
        ChangelogResult? changelog = null;
        try
        {
            _logger.Info($"dotnet-releaser {Version} - {buildKind.ToString().ToLowerInvariant()}");
            _logger.LogStartGroup($"Configuring");
            var result = await Configuring(configurationFile, buildKind, githubApiToken, githubApiTokenExtra, gitHubTokenGist, nugetApiToken, forceArtifactsFolder, publishVersion);
            if (result is null) return false;
            buildInformation = result.Value.buildInformation!;
            devHosting = result.Value.devHosting;
            devHostingExtra = result.Value.devHostingExtra;
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
                _logger.LogStartGroup($"Preparing Changelog - {buildInformation.Version}");
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
        if (!await BuildAndTest(devHosting, buildInformation)) return false;

        // ------------------------------------------------------------------
        // Build NuGet package
        // ------------------------------------------------------------------
        if (!await BuildNuGetPackage(buildInformation)) return false;

        // ------------------------------------------------------------------
        // Build executable packages (deb, zip, rpm, tar...)
        // ------------------------------------------------------------------
        if (!await BuildAppPackages(buildInformation)) return false;

        // ------------------------------------------------------------------
        // Publish all packages NuGet + (deb, zip, rpm, tar...)
        // ------------------------------------------------------------------
        if (buildInformation.BuildKind == BuildKind.Publish || buildInformation.PublishNuGet)
        {
            await PublishPackages(nugetApiToken, buildInformation, hostingConfiguration, devHosting, devHostingExtra);
        }

        // ------------------------------------------------------------------
        // Publish changelog
        // ------------------------------------------------------------------
        // Draft if we are just building and not publishing (to allow to update the changelog)
        if (buildInformation.BuildKind == BuildKind.Publish || buildInformation.PublishNuGet)
        {
            await PublishChangelog(buildInformation, hostingConfiguration, devHosting, changelog, forceUpload);
        }

        // ------------------------------------------------------------------
        // Publish coverage results + (deb, zip, rpm, tar...)
        // ------------------------------------------------------------------
        if (!HasErrors && devHosting is not null)
        {
            await PublishCoveralls(devHosting, buildInformation);
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

    private class AppException : Exception
    {
        public AppException(string message) : base(message)
        {
        }
    }

    private static TableBorder GetTableBorderFromKind(TableBorderKind tableBorderKind)
    {
        return tableBorderKind switch
        {
            TableBorderKind.None => TableBorder.None,
            TableBorderKind.Ascii => TableBorder.Ascii,
            TableBorderKind.Ascii2 => TableBorder.Ascii2,
            TableBorderKind.AsciiDoubleHead => TableBorder.AsciiDoubleHead,
            TableBorderKind.Square => TableBorder.Square,
            TableBorderKind.Rounded => TableBorder.Rounded,
            TableBorderKind.Minimal => TableBorder.Minimal,
            TableBorderKind.MinimalHeavyHead => TableBorder.MinimalHeavyHead,
            TableBorderKind.MinimalDoubleHead => TableBorder.MinimalDoubleHead,
            TableBorderKind.Simple => TableBorder.Simple,
            TableBorderKind.SimpleHeavy => TableBorder.SimpleHeavy,
            TableBorderKind.Horizontal => TableBorder.Horizontal,
            TableBorderKind.Heavy => TableBorder.Heavy,
            TableBorderKind.HeavyEdge => TableBorder.HeavyEdge,
            TableBorderKind.HeavyHead => TableBorder.HeavyHead,
            TableBorderKind.Double => TableBorder.Double,
            TableBorderKind.DoubleEdge => TableBorder.DoubleEdge,
            TableBorderKind.Markdown => TableBorder.Markdown,
            _ => throw new ArgumentOutOfRangeException(nameof(tableBorderKind), tableBorderKind, null)
        };
    }
}