using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using DotNetReleaser.Logging;
using Tomlyn;
using Tomlyn.Syntax;

namespace DotNetReleaser.Configuration;

public class ReleaserConfiguration
{
    public ReleaserConfiguration()
    {
        ConfigurationFilePath = string.Empty;
        ArtifactsFolder = "artifacts-dotnet-releaser";
        Packs = new List<PackagingConfiguration>();
        Coverage = new CoverageConfiguration();
        Test = new TestConfiguration();
        MSBuild = new MSBuildConfiguration();
        Changelog = new ChangelogConfiguration();
        GitHub = new GitHubDevHostingConfiguration();
        NuGet = new NuGetPublisher();
        Brew = new BrewPublisher();
        Scoop = new ScoopPublisher();
        Service = new ServiceConfiguration();
        Debian = new DebianConfiguration();
        Rpm = new RpmConfiguration();
    }
    public PackagingProfileKind Profile { get; set; }

    internal string ConfigurationFilePath { get; set; }

    internal string? ConfigurationFolder => Path.GetDirectoryName(ConfigurationFilePath);

    public string ArtifactsFolder { get; set; }

    public bool EnablePublishPackagesInDraft { get; set; }

    public CoverageConfiguration Coverage { get; }

    public TestConfiguration Test { get; }

    [JsonPropertyName("msbuild")]
    public MSBuildConfiguration MSBuild { get; }

    [JsonPropertyName("github")]
    public GitHubDevHostingConfiguration GitHub { get; }

    public ChangelogConfiguration Changelog { get; }

    [JsonPropertyName("nuget")]
    public NuGetPublisher NuGet { get; }

    public BrewPublisher Brew { get; }

    public ScoopPublisher Scoop { get; set; }

    /// <summary>
    /// Configuration for service
    /// </summary>
    public ServiceConfiguration Service { get; }

    /// <summary>
    /// Debian configuration
    /// </summary>
    [JsonPropertyName("deb")]
    public DebianConfiguration Debian { get; }
    
    /// <summary>
    /// Rpm configuration.
    /// </summary>
    public RpmConfiguration Rpm { get; }

    /// <summary>
    /// Configuration for packs
    /// </summary>
    [JsonPropertyName("pack")]
    public List<PackagingConfiguration> Packs { get; }

    public static async Task<ReleaserConfiguration?> From(string filePath, ISimpleLogger logger)
    {
        try
        {
            filePath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, filePath));
            if (!File.Exists(filePath))
            {
                logger.Error($"Configuration file `{filePath}' not found.");
                return null;
            }

            logger.Info($"Loading configuration from {filePath}");
            var content = await File.ReadAllTextAsync(filePath);

            var configuration = TomlSerializer.Deserialize<ReleaserConfiguration>(content, new TomlSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate,
                SourceName = filePath
            });
            if (configuration is null) return null;
            
            if (!configuration.Initialize(Path.GetDirectoryName(filePath) ?? Environment.CurrentDirectory, logger))
            {
                return null;
            }

            configuration.ConfigurationFilePath = filePath;
            return configuration;
        }
        catch (Exception ex)
        {
            logger.Error($"Unexpected exception while trying to load configuration from `{filePath}`. Reason: {ex.Message}");
        }

        return null;
    }

    private bool Initialize(string configurationDirectory, ISimpleLogger logger)
    {
        ArtifactsFolder = Path.GetFullPath(Path.Combine(configurationDirectory, ArtifactsFolder));

        // Make sure that the path is absolute
        if (MSBuild.Publish && MSBuild.Projects.Count > 0)
        {
            for (var i = 0; i < MSBuild.Projects.Count; i++)
            {
                var project = MSBuild.Projects[i];
                project = Path.GetFullPath(Path.Combine(configurationDirectory, project));
                if (!File.Exists(project))
                {
                    logger.Error($"The MSBuild project file `{project}` was not found.");
                }
                else
                {
                    // Replace with a full path
                    MSBuild.Projects[i] = project;
                }
            }

            if (logger.HasErrors) return false;
        }

        // Check changelog
        if (Changelog.Publish && !string.IsNullOrEmpty(Changelog.Path))
        {
            Changelog.Path = Path.GetFullPath(Path.Combine(configurationDirectory, Changelog.Path));
            if (!File.Exists(Changelog.Path))
            {
                logger.Error($"The changelog file {Changelog.Path} was not found.");
                return false;
            }
        }

        // Defaults for coverage
        if (Coverage.Enable)
        {
            Coverage.AddDefaults();
        }
        
        if (Profile == PackagingProfileKind.Default)
        {
            AddPackages(new List<PackagingConfiguration>()
            {
                new() { RuntimeIdentifiers = { "win-x64", "win-arm64" }, Kinds = { PackageKind.Zip } },
                new() { RuntimeIdentifiers = { "linux-x64", "linux-arm64" }, Kinds = { PackageKind.Rpm, PackageKind.Deb, PackageKind.Tar } },
                new() { RuntimeIdentifiers = { "osx-x64", "osx-arm64" }, Kinds = { PackageKind.Tar } },
            }, logger);
        }

        // Initialize defaults for changelog
        if (Changelog.Owners.Count == 0 && !string.IsNullOrEmpty(GitHub.User))
        {
            Changelog.Owners.Add(GitHub.User);
        }
        Changelog.AddDefaults();

        // Add defaults to GitHub
        GitHub.AddDefaults();

        return true;
    }

    private void AddPackages(List<PackagingConfiguration> packages, ISimpleLogger logger)
    {
        var builder = new StringBuilder();
        foreach (var packaging in packages)
        {
            foreach (var rid in packaging.RuntimeIdentifiers)
            {
                if (Packs.Any(x => x.RuntimeIdentifiers.Contains(rid)))
                {
                    builder.AppendLine($"Skipping {Profile.ToString().ToLowerInvariant()} profile for {PackagingConfiguration.ToStringRidAndKinds(new () { rid }, packaging.Kinds)} because there is a custom entry in the configuration file");
                }
                else
                {
                    var singleRid = new PackagingConfiguration()
                    {
                        RuntimeIdentifiers = { rid },
                    };
                    singleRid.Kinds.AddRange(packaging.Kinds);
                    Packs.Add(singleRid);
                    builder.AppendLine($"Adding {Profile.ToString().ToLowerInvariant()} profile for {PackagingConfiguration.ToStringRidAndKinds(new() { rid }, packaging.Kinds)}");
                }
            }
        }

        if (builder.Length > 0)
        {
            logger.Debug(builder.ToString());
        }
    }
}
