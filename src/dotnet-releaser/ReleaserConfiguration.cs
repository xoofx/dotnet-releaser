using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using DotNetReleaser.Logging;
using Microsoft.Extensions.Logging;
using Tomlyn;
using Tomlyn.Model;
using Tomlyn.Syntax;

namespace DotNetReleaser;

public class ReleaserConfiguration
{
    public ReleaserConfiguration()
    {
        ArtifactsFolder = "artifacts-dotnet-releaser";
        Packs = new List<Packaging>();
        MSBuild = new MSBuildConfiguration();
        Changelog = new ChangelogConfiguration();
        GitHub = new GitHubPublisher();
        NuGet = new NuGetPublisher();
        Brew = new BrewPublisher();
        Service = new ServiceConfiguration();
    }
    public ProfileKind Profile { get; set; }

    public string ArtifactsFolder { get; set; }

    public MSBuildConfiguration MSBuild { get; }

    [DataMember(Name="github")]
    public GitHubPublisher GitHub { get; }

    public ChangelogConfiguration Changelog { get; }

    [DataMember(Name = "nuget")]
    public NuGetPublisher NuGet { get; }

    public BrewPublisher Brew { get; }

    /// <summary>
    /// Configuration for service
    /// </summary>
    public ServiceConfiguration Service { get; }
    
    /// <summary>
    /// Configuration for packs
    /// </summary>
    [DataMember(Name = "pack")]
    public List<Packaging> Packs { get; }
    
    private bool Initialize(string configurationDirectory, ISimpleLogger logger)
    {
        ArtifactsFolder = Path.GetFullPath(Path.Combine(configurationDirectory, ArtifactsFolder));

        // Make sure that the path is absolute
        MSBuild.Project = Path.GetFullPath(Path.Combine(configurationDirectory, MSBuild.Project));
        if (!File.Exists(MSBuild.Project))
        {
            logger.Error($"The MSBuild project file `{MSBuild.Project}` was not found.");
            return false;
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

        if (Profile == ProfileKind.Default)
        {
            AddPackages(new List<Packaging>()
            {
                new() { RuntimeIdentifiers = { "win-x64", "win-arm", "win-arm64" }, Kinds = { PackageKind.Zip } },
                new() { RuntimeIdentifiers = { "linux-x64", "linux-arm", "linux-arm64" }, Kinds = { PackageKind.Deb, PackageKind.Tar } },
                new() { RuntimeIdentifiers = { "rhel-x64" }, Kinds = { PackageKind.Rpm, PackageKind.Tar } },
                new() { RuntimeIdentifiers = { "osx-x64", "osx-arm64" }, Kinds = { PackageKind.Tar } },
            }, logger);
        }

        return true;
    }

    private void AddPackages(List<Packaging> packages, ISimpleLogger logger)
    {
        var builder = new StringBuilder();
        foreach (var packaging in packages)
        {
            foreach (var rid in packaging.RuntimeIdentifiers)
            {
                if (Packs.Any(x => x.RuntimeIdentifiers.Contains(rid)))
                {
                    builder.AppendLine($"Skipping {Profile.ToString().ToLowerInvariant()} profile for {Packaging.ToStringRidAndKinds(new () { rid }, packaging.Kinds)} because there is a custom entry in the configuration file");
                }
                else
                {
                    var singleRid = new Packaging()
                    {
                        RuntimeIdentifiers = { rid },
                    };
                    singleRid.Kinds.AddRange(packaging.Kinds);
                    Packs.Add(singleRid);
                    builder.AppendLine($"Adding {Profile.ToString().ToLowerInvariant()} profile for {Packaging.ToStringRidAndKinds(new() { rid }, packaging.Kinds)}");
                }
            }
        }

        if (builder.Length > 0)
        {
            logger.Info(builder.ToString());
        }
    }

    /// <summary>
    /// MSBuild Configuration.
    /// </summary>
    public class MSBuildConfiguration
    {
        public MSBuildConfiguration()
        {
            Project = string.Empty;
            Configuration = "Release";

            // Default properties for publishing a native app
            Properties = new Dictionary<string, object>()
            {
                { "PublishTrimmed", true },
                { "PublishSingleFile", true },
                { "SelfContained", true },
                { "PublishReadyToRun", true },
                //{ "PublishReadyToRunComposite", true }, // not by default
                { "CopyOutputSymbolsToPublishDirectory", false },
                { "SkipCopyingSymbolsToOutputDirectory", true }
            };
        }

        /// <summary>
        /// Gets or sets the path to the project that contains the app to build.
        /// </summary>
        public string Project { get; set; }
        
        /// <summary>
        /// The configuration to compile
        /// </summary>
        public string Configuration { get; set; }

        /// <summary>
        /// Gets the extra properties that will be passed to MSBuild.
        /// </summary>
        public Dictionary<string, object> Properties { get; }

        public override string ToString()
        {
            return $"{nameof(Project)}: {Project}, {nameof(Configuration)}: {Configuration}, {nameof(Properties)} Count = {Properties.Count}";
        }
    }
    
    /// <summary>
    /// Configuration for GitHub.
    /// </summary>
    public class GitHubPublisher : PublisherBase
    {
        public GitHubPublisher()
        {
            VersionPrefix = string.Empty;
            Base = "https://github.com";
        }

        public string Base { get; set; }

        public string? User { get; set; }

        public string? Repo { get; set; }

        public string VersionPrefix { get; set; }

        public string GetUrl() => $"{Base.Trim('/')}/{User}/{Repo}";

        public override string ToString()
        {
            return $"{base.ToString()}, {nameof(User)}: {User}, {nameof(Repo)}: {Repo}, {nameof(VersionPrefix)}: {VersionPrefix}, Url: {GetUrl()}";
        }
    }

    public class ChangelogConfiguration : PublisherBase
    {
        public ChangelogConfiguration()
        {
            Version = @"^##\s+v?((\d+\.)*(\d+))";
        }

        public string? Path { get; set; }

        public string Version { get; set; }

        public override string ToString()
        {
            return $"{base.ToString()}, {nameof(Path)}: {Path}, {nameof(Version)}: {Version}";
        }
    }

    public class NuGetPublisher : PublisherBase
    {
        public NuGetPublisher()
        {
            Source = "https://api.nuget.org/v3/index.json";
        }

        public string Source { get; set; }

        public override string ToString()
        {
            return $"{base.ToString()}, {nameof(Source)}: {Source}";
        }
    }

    public class BrewPublisher : PublisherBase
    {
    }

    public class Packaging : PublisherBase
    {
        public Packaging()
        {
            RuntimeIdentifiers = new List<string>();
            Kinds = new List<PackageKind>();
        }

        [DataMember(Name = "rid")]
        public List<string> RuntimeIdentifiers { get; }

        [DataMember(Name = "kinds")]
        public List<PackageKind> Kinds { get; }

        public static string ToStringRidAndKinds(List<string> rids, List<PackageKind> kinds) => $"platform{(rids.Count > 1 ? "s" : string.Empty)} [{string.Join(", ", rids)}] with [{string.Join(", ", kinds)}] package{(kinds.Count > 1 ? "s" : string.Empty)}";
        
        public override string ToString()
        {
            return $"{base.ToString()}, {ToStringRidAndKinds(RuntimeIdentifiers, Kinds)}";
        }
    }

    public class ServiceConfiguration : PublisherBase
    {
        public ServiceConfiguration()
        {
            Publish = false;
            Systemd = new SystemdConfiguration();
        }

        public SystemdConfiguration Systemd { get; }

        public class SystemdConfiguration : PublisherBase
        {
            public SystemdConfiguration()
            {
                Arguments = string.Empty;
                Sections = new Dictionary<string, IDictionary<string, object?>>()
                {
                    { "Unit", new Dictionary<string, object?>() },
                    { "Service", new Dictionary<string, object?>() },
                    { "Install", new Dictionary<string, object?>() },
                };

                // Defaults for restarting
                Sections["Unit"]["StartLimitIntervalSec"] = 60; // Tries during 60s to restart the service
                Sections["Unit"]["StartLimitBurst"] = 4; // Maximum of 4 retries in 60s
                Sections["Service"]["Restart"] = "always"; // Always tries to restart the service
                Sections["Service"]["RestartSec"] = 1; // 1s
                Sections["Install"]["WantedBy"] = "multi-user.target";
            }

            public string Arguments { get; set; }

            public string? User { get; set; }

            public string? Group { get; set; }

            public Dictionary<string, IDictionary<string, object?>> Sections { get; }
        }
    }

    public static async Task<ReleaserConfiguration?> From(string filePath, ISimpleLogger logger)
    {
        try
        {
            logger.Info($"Loading configuration from {filePath}");
            var content = await File.ReadAllTextAsync(filePath);

            if (Toml.TryToModel(content, out ReleaserConfiguration? configuration, out var diagnostics, filePath))
            {
                if (!configuration.Initialize(Path.GetDirectoryName(filePath) ?? Environment.CurrentDirectory, logger))
                {
                    return null;
                }

                return configuration;
            }

            // Log any messages
            foreach (var message in diagnostics!)
            {
                if (message.Kind == DiagnosticMessageKind.Error)
                {
                    logger.Error(message.ToString());
                }
                else if (message.Kind == DiagnosticMessageKind.Warning)
                {
                    logger.Warn(message.ToString());
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error($"Unexpected exception while trying to load configuration from `{filePath}`. Reason: {ex.Message}");
        }

        return null;
    }

    private static void TransferValue<T>(TomlTable table, string name, ILogger log, ref bool hasErrors, Action<T> setter, bool canBeNull = false)
    {
        if (!table.TryGetValue(name, out var value) || value == null)
        {
            if (!canBeNull)
            {
                hasErrors = true;
                log.LogError($"Missing entry `{name}` in configuration.");
            }
        }
        else if (value is T valueT)
        {
            setter(valueT);
        }
        else
        {
            hasErrors = true;
            log.LogError($"Entry `{name}` in configuration is not of type {typeof(T).Name.ToLowerInvariant()}.");
        }
    }

    public abstract class PublisherBase
    {
        protected PublisherBase()
        {
            Publish = true;
        }

        public bool Publish { get; set; }

        public override string ToString()
        {
            return $"{nameof(Publish)}: {Publish}";
        }
    }
}

public enum PackageKind
{
    Zip,
    Tar,
    Deb,
    Rpm,
    Setup,
}

public enum ProfileKind
{
    /// <summary>
    /// Target all supported platforms and architecture with NuGet + all packages (debian/rpm) + Homebrew.
    /// </summary>
    Default,

    /// <summary>
    /// Explicitly user defined.
    /// </summary>
    Custom,
}