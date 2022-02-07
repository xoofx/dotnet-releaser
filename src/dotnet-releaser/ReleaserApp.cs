using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DotNetReleaser.Logging;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using Octokit;

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
public partial class ReleaserApp : ISimpleLogger
{
    private static readonly string DotNetReleaserConfigFile = Path.Combine(AppContext.BaseDirectory, ReleaserConstants.DotNetReleaserFileName);
    
    private readonly ISimpleLogger _logger;
    private string _githubApiToken;
    private string _nugetApiToken;
    private string _configurationFile;
    private bool _forceArtifactsFolder;
    private BuildKind _buildKind;
    private ReleaserConfiguration _config;

    private ReleaserApp(ISimpleLogger logger)
    {
        _githubApiToken = string.Empty;
        _nugetApiToken = string.Empty;
        _configurationFile = string.Empty;
        _logger = logger;
        _config = new ReleaserConfiguration();
    }

    /// <summary>
    /// Main entry for the releaser. Parser the argument and delegate to <see cref="RunImpl"/>
    /// </summary>
    /// <param name="args">The command line arguments</param>
    /// <returns>0 if successful; 1 otherwise.</returns>
    public static async Task<int> Run(string[] args)
    {
        // Create our log
        using var factory = LoggerFactory.Create(builder =>
        {
            builder.AddSimpleConsole(configure: options =>
            {
                //options.SingleLine = true;
            });
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
                var configurationFileArg = newCommand.Argument("dotnet-releaser.toml", "TOML configuration file path to create. Default is: dotnet-releaser.toml");
                var projectOption = newCommand.Option<string>("--project <project_file>", "A - relative - path to project file (csproj, vbproj, fsproj)", CommandOptionType.SingleValue).IsRequired();
                var userOption = newCommand.Option<string>("--user <GitHub_user/org>", "The GitHub user/org where the packages will be published", CommandOptionType.SingleValue);
                var repoOption = newCommand.Option<string>("--repo <GitHub_repo>", "The GitHub repo name where the packages will be published", CommandOptionType.SingleValue);
                var forceOption = newCommand.Option<bool>("--force", "Force overwriting the existing TOML configuration file.", CommandOptionType.NoValue);

                newCommand.OnExecuteAsync(async token =>
                    {
                        var result = await appReleaser.CreateConfigurationFile(configurationFileArg.Value, projectOption.ParsedValue, userOption.ParsedValue, repoOption.ParsedValue, forceOption.ParsedValue);
                        return result ? 0 : 1;
                    }
                );
            }
        );

        void AddPublishOrBuildArgs(CommandLineApplication cmd)
        {
            CommandOption<string>? githubToken = null;
            CommandOption<string>? nugetToken = null;

            if (cmd.Name == "publish")
            {
                cmd.Description = "Build and publish the project.";
                githubToken = cmd.Option<string>("--github-token <token>", "GitHub Api Token. Required if publish to GitHub is true in the config file", CommandOptionType.SingleValue);
                nugetToken = cmd.Option<string>("--nuget-token <token>", "NuGet Api Token. Required if publish to NuGet is true in the config file", CommandOptionType.SingleValue);
            }
            else
            {
                cmd.Description = "Build only the project.";
            }

            var forceOption = cmd.Option<bool>("--force", "Force deleting and recreating the artifacts folder.", CommandOptionType.NoValue);
            var configurationFileArg = cmd.Argument<string>("dotnet-releaser.toml", "TOML configuration file").IsRequired();

            cmd.OnExecuteAsync(async (token) =>
            {
                appReleaser._forceArtifactsFolder = forceOption.ParsedValue;

                // Check configuration file
                var configurationFilePath = configurationFileArg.ParsedValue;
                configurationFilePath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, configurationFilePath));
                if (!File.Exists(configurationFilePath))
                {
                    throw new AppException($"Configuration file `{configurationFilePath}' not found.");
                }
                appReleaser._configurationFile = configurationFilePath;
                
                appReleaser._buildKind = cmd.Name == "publish" ? BuildKind.Publish : BuildKind.Build;
                if (githubToken is not null) appReleaser._githubApiToken = githubToken.ParsedValue;
                if (nugetToken is not null) appReleaser._nugetApiToken = nugetToken.ParsedValue;
                var result = await appReleaser.RunImpl();
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

    /// <summary>
    /// Runs the releaser app
    /// </summary>
    private async Task<bool> RunImpl()
    {
        // ------------------------------------------------------------------
        // Load Configuration
        // ------------------------------------------------------------------
        var configuration = await ReleaserConfiguration.From(_configurationFile, this);
        if (configuration is null) return false;
        _config = configuration;

        // Don't continue if we had errors when deserializing the config file
        if (HasErrors) return false;

        if (!EnsureArtifactsFolders()) return false;

        // ------------------------------------------------------------------
        // Load Package Information from MSBuild project
        // ------------------------------------------------------------------
        var packageInfo = await LoadPackageInfo();
        if (packageInfo is null) return false;

        Info($"Package to build: {packageInfo}");

        // If the project is not packable as a NuGet package but we still (by default)
        // ask for a NuGet package, produce a warning
        var willDoNuGetPack = packageInfo.IsNuGetPackable && _config.NuGet.Publish;
        if (!packageInfo.IsNuGetPackable && _config.NuGet.Publish)
        {
            Warn("The project is not packable as a NuGet package (IsPackable = false). Skipping NuGet building/publishing.");
        }

        // ------------------------------------------------------------------
        // Validate Publish parameters
        // ------------------------------------------------------------------
        GitHubClient? gitHubClient = null;
        if (_buildKind == BuildKind.Publish)
        {
            if (_config.GitHub.Publish)
            {
                if (string.IsNullOrEmpty(_githubApiToken))
                {
                    Error("Publishing to GitHub requires to pass --github-token");
                    return false;
                }
                else
                {
                    gitHubClient = await ConnectGitHubClient();
                    if (gitHubClient is null)
                    {
                        return false;
                    }
                }
            }

            if (willDoNuGetPack && string.IsNullOrEmpty(_nugetApiToken))
            {
                Error("Publishing to NuGet requires to pass --nuget-token");
                return false;
            }
        }

        // Update homebrew config (and log if necessary)
        UpdateHomebrewConfigurationFromPackage(packageInfo);
        
        // ------------------------------------------------------------------
        // Parse Changelog
        // ------------------------------------------------------------------
        string? changelog = null;
        if (_config.Changelog.Publish)
        {
            changelog = await LoadChangeLog(packageInfo);
            if (changelog is not null)
            {
                Info($"Changelog found:{Environment.NewLine}{changelog}");
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

        // ------------------------------------------------------------------
        // Build NuGet package
        // ------------------------------------------------------------------
        if (!await BuildNuGetPackage()) return false;

        // ------------------------------------------------------------------
        // Build executable packages (deb, zip, rpm, tar...)
        // ------------------------------------------------------------------
        var entriesToPublish = new List<PackageEntry>();

        var builder = new StringBuilder();
        bool hasPackagesToBuild = false;
        foreach (var pack in _config.Packs)
        {
            foreach (var rid in pack.RuntimeIdentifiers)
            {
                if (pack.Publish) hasPackagesToBuild = true;
                builder.AppendLine($"Build configured for {ReleaserConfiguration.Packaging.ToStringRidAndKinds(new () { rid }, pack.Kinds)}");
            }
        }
        Info(builder.ToString());

        if (hasPackagesToBuild)
        {
            Info("Begin building platform packages...");
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
                Error("Error while building platform packages.");
            }
            else
            {
                Info("End building platform packages successful.");
            }
        }
        else
        {
            Info("No packages to build");
        }

        // Exit if we have any errors.
        if (HasErrors)
        {
            return false;
        }

        // ------------------------------------------------------------------
        // Publish all packages NuGet + (deb, zip, rpm, tar...)
        // ------------------------------------------------------------------
        if (_buildKind == BuildKind.Publish)
        {
            if (willDoNuGetPack)
            {
                await PublishNuGet(packageInfo, _nugetApiToken);
            }

            // Don't try to continue publishing if we had errors with NuGet publishing
            // Otherwise publish any packages that we have generated before
            if (!HasErrors && gitHubClient is not null)
            {
                await UpdateGitHub(gitHubClient, packageInfo, changelog, entriesToPublish);

                if (!HasErrors && _config.Brew.Publish)
                {
                    await UploadBrewFormula(gitHubClient, packageInfo, entriesToPublish);
                }
            }
        }

        return !HasErrors;
    }


    public bool HasErrors => _logger.HasErrors;

    public void Info(string message)
    {
        _logger.Info(message);
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