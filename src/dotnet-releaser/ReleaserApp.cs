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
using Microsoft.Build.Locator;
using XenoAtom.CommandLine;
using XenoAtom.CommandLine.Terminal;
using XenoAtom.Logging;
using XenoAtom.Logging.Writers;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Styling;

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
    private TableStyle _tableBorder;
    private bool _skipAppPackagesForBuildOnly;

    private ReleaserApp(ISimpleLogger logger)
    {
        ExeName = "dotnet-releaser";
        _logger = logger;
        _config = new ReleaserConfiguration();
        _assemblyCoverages = new List<AssemblyCoverage>();
        _tableBorder = TableStyle.Default;
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
        var runningOnGitHubAction = GitHubActionHelper.IsRunningOnGitHubAction;
        var exeName = "dotnet-releaser";

        if (LogManager.IsInitialized)
        {
            LogManager.Shutdown();
        }

        var terminalWriter = new TerminalLogWriter();
        LogManager.Initialize(new LogManagerConfig
        {
            RootLogger =
            {
                MinimumLevel = LogLevel.Info,
                Writers =
                {
                    terminalWriter
                }
            }
        });
        try
        {
            var logger = SimpleLogger.CreateConsoleLogger(LogManager.GetLogger(exeName), exeName);
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

            if (appReleaser.HasErrors)
            {
                return 1;
            }

            var app = new CommandApp(exeName, config: new CommandConfig
            {
                OutputFactory = _ => new TerminalMarkupCommandOutput(new TerminalMarkupOutputOptions
                {
                    UseTerminalWindowWidth = !runningOnGitHubAction
                })
            });

            app.Add(new VersionOption($"{exeName} {appReleaser.Version} - {DateTime.Now.Year} (c) Copyright Alexandre Mutel", "version"));
            app.Add(new HelpOption());
            app.Add(CreatePublishOrBuildCommand("publish"));
            app.Add(CreatePublishOrBuildCommand("build"));
            app.Add(CreatePublishOrBuildCommand("run"));
            app.Add(CreateNewCommand());
            app.Add(CreateChangelogCommand());
            app.Add((ctx, _) =>
            {
                ctx.Out.WriteLine("Specify a sub-command");
                app.ShowHelp(ctx.RunConfig);
                return ValueTask.FromResult(1);
            });

            int result;
            try
            {
                result = runningOnGitHubAction
                    ? await app.RunAsync(args, new CommandRunConfig(Width: 256, OptionWidth: 29)).ConfigureAwait(false)
                    : await app.RunAsync(args).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                Terminal.WriteMarkupLine($"[red]Unexpected error {exception.Message}[/]");
                result = 1;
            }

            if (runningOnGitHubAction)
            {
                await Task.Delay(16).ConfigureAwait(false);
                await Terminal.Out.FlushAsync().ConfigureAwait(false);
            }

            return result;

            Command CreateNewCommand()
            {
                string? configurationFilePath = null;
                string? projectFilePath = null;
                string? gitHubUser = null;
                string? gitHubRepo = null;
                var forceOverwrite = false;

                var command = new Command("new", "Create a dotnet-releaser TOML configuration file for a specified project.");
                command.Add(new HelpOption());
                command.Add("<dotnet-releaser.toml>?", "TOML configuration file path to create. Default is: dotnet-releaser.toml", value => configurationFilePath = value);
                command.Add("project=", "A - relative - path to a solution file (.sln, .slnx) or project file (.csproj, .fsproj, .vbproj). By default, it will try to find a solution file where this command is run or where the output dotnet-releaser.toml file is specified.", value => projectFilePath = value);
                command.Add("user=", "The GitHub user/org where the packages will be published. If not specified, it will try to detect automatically if there is a git repository configured from the folder (and parents) of the TOML configuration file, and extract any git remote that could give this information.", value => gitHubUser = value);
                command.Add("repo=", "The GitHub repo name where the packages will be published. If not specified, it will try to detect automatically if there is a git repository configured from the folder (and parents) of the TOML configuration file, and extract any git remote that could give this information.", value => gitHubRepo = value);
                command.Add("force", "Force overwriting the existing TOML configuration file.", value => forceOverwrite = value is not null);
                command.Add(async (_, _) =>
                {
                    var result = await appReleaser.CreateConfigurationFile(configurationFilePath, projectFilePath, gitHubUser, gitHubRepo, forceOverwrite).ConfigureAwait(false);
                    return result ? 0 : 1;
                });
                return command;
            }

            Command CreateChangelogCommand()
            {
                string? configurationFilePath = null;
                string? version = null;
                string? gitHubToken = null;
                var updateChangelog = false;

                var command = new Command("changelog", "Generate changelog for the specified GitHub owner/repository and optionally upload them back.");
                command.Add(new HelpOption());
                command.Add("<dotnet-releaser.toml>", "The input TOML configuration file.", value => configurationFilePath = value);
                command.Add("<version>?", "An optional version to generate the changelog for. If it is not defined, it will fetch all existing tags and generate the logs for them.", value => version = value);
                command.Add("update", "Update the changelog on GitHub for the specified version or all versions if no versions are specified.", value => updateChangelog = value is not null);
                command.Add("github-token=", "GitHub Api Token. Required if publish to GitHub is true in the config file", value => gitHubToken = value);
                command.Add(async (_, _) =>
                {
                    if (string.IsNullOrWhiteSpace(gitHubToken))
                    {
                        appReleaser.Error("Missing required option `--github-token`.");
                        return 1;
                    }

                    var result = await appReleaser.ListOrUpdateChangelog(configurationFilePath ?? string.Empty, gitHubToken, version ?? string.Empty, updateChangelog).ConfigureAwait(false);
                    return result ? 0 : 1;
                });
                return command;
            }

            Command CreatePublishOrBuildCommand(string commandName)
            {
                string? configurationFilePath = null;
                string? gitHubToken = null;
                string? nugetToken = null;
                string? gitHubTokenExtra = null;
                string? gitHubTokenGist = null;
                string? publishVersion = null;
                var skipAppPackagesForBuildOnly = false;
                var forceArtifactsFolder = false;
                var forceUpload = false;
                var hasTableOption = false;
                EnumWrapper<TableBorderKind> tableKind = TableBorderKind.Square;

                var description = commandName switch
                {
                    "run" => "Automatically build and publish a project when running from a GitHub Action based on which branch is active, if there is a tag (for publish), and if the change is a `push`.",
                    "publish" => "Build and publish the project.",
                    _ => "Build only the project."
                };

                var command = new Command(commandName, description);
                command.Add(new HelpOption());
                command.Add("<dotnet-releaser.toml>", "The input TOML configuration file.", value => configurationFilePath = value);
                command.Add("github-token=", "GitHub Api Token. Required if publish to GitHub is true in the config file", value => gitHubToken = value);

                if (commandName is "publish" or "run")
                {
                    command.Add("nuget-token=", "NuGet Api Token. Required if publish to NuGet is true in the config file", value => nugetToken = value);
                    command.Add("github-token-extra=", "GitHub Api Token. Required if publish homebrew to GitHub is true in the config file. In that case dotnet-releaser needs a personal access GitHub token which can create the homebrew repository. This token has usually more access than the --github-token that is only used for the current repository. ", value => gitHubTokenExtra = value);
                    command.Add("github-token-gist=", "GitHub Api Token. Required if publishing to a gist used for e.g coverage.", value => gitHubTokenGist = value);
                }

                if (commandName is "run" or "build")
                {
                    command.Add("skip-app-packages-for-build-only", "Skip building application packages (e.g tar) when building only (but not publishing). This is useful when running on a CI and you want to build app packages only when publishing.", value => skipAppPackagesForBuildOnly = value is not null);
                }

                command.Add("table=", "Specifies the rendering of the tables. Default is square.", (EnumWrapper<TableBorderKind> value) =>
                {
                    tableKind = value;
                    hasTableOption = true;
                });

                command.Add("force", "Force deleting and recreating the artifacts folder.", value => forceArtifactsFolder = value is not null);

                if (commandName == "publish")
                {
                    command.Add("version=", "Tag version used when publishing the changelog and creating the release tag.", value => publishVersion = value);
                    command.Add("force-upload", "Force uploading the release assets.", value => forceUpload = value is not null);
                }

                command.Add(async (_, _) =>
                {
                    var buildKind = commandName switch
                    {
                        "run" => BuildKind.Run,
                        "publish" => BuildKind.Publish,
                        _ => BuildKind.Build
                    };

                    appReleaser._skipAppPackagesForBuildOnly = skipAppPackagesForBuildOnly;
                    if (hasTableOption)
                    {
                        appReleaser._tableBorder = GetTableBorderFromKind(tableKind);
                    }

                    var result = await appReleaser.RunImpl(configurationFilePath ?? string.Empty, buildKind, gitHubToken ?? string.Empty, gitHubTokenExtra, gitHubTokenGist, nugetToken, forceArtifactsFolder, forceUpload, publishVersion).ConfigureAwait(false);
                    return result ? 0 : 1;
                });

                return command;
            }
        }
        finally
        {
            LogManager.Shutdown();
        }
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
            // TODO: After removing coveralls, check what to do here. The publishing of coverage via gist is done in another place.
        }

        return !HasErrors;
    }

    public bool HasErrors => _logger.HasErrors;

    public void Info(string message)
    {
        _logger.Info(message);
    }

    public void Info(string message, Visual renderable)
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

    private static TableStyle GetTableBorderFromKind(TableBorderKind tableBorderKind)
    {
        return tableBorderKind switch
        {
            TableBorderKind.None => TableStyle.Default,
            TableBorderKind.Rounded => TableStyle.RoundedGrid,
            TableBorderKind.Minimal => TableStyle.Minimal,
            TableBorderKind.MinimalHeavyHead => TableStyle.Minimal,
            TableBorderKind.MinimalDoubleHead => TableStyle.Minimal,
            TableBorderKind.Simple => TableStyle.Minimal,
            TableBorderKind.SimpleHeavy => TableStyle.Minimal,
            TableBorderKind.Horizontal => TableStyle.Minimal,
            TableBorderKind.Heavy => TableStyle.DoubleGrid,
            TableBorderKind.HeavyEdge => TableStyle.DoubleGrid,
            TableBorderKind.HeavyHead => TableStyle.DoubleGrid,
            TableBorderKind.Double => TableStyle.DoubleGrid,
            TableBorderKind.DoubleEdge => TableStyle.DoubleGrid,
            TableBorderKind.Markdown => TableStyle.Minimal,
            _ => TableStyle.Default
        };
    }
}
