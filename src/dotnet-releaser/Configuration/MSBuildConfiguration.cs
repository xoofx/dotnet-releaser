using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DotNetReleaser.Configuration;

/// <summary>
/// MSBuild Configuration.
/// </summary>
public class MSBuildConfiguration : ConfigurationBase
{
    public MSBuildConfiguration()
    {
        Projects = new List<string>();
        Configuration = "Release";
        ConfigurationDebug = "Debug";
        BuildDebug = false;

        // Default properties for publishing a native app
        Properties = new Dictionary<string, object>()
        {
            { "PublishTrimmed", true },
            { "PublishSingleFile", true },
            { "SelfContained", true },
            { "PublishReadyToRun", true },
            { "CopyOutputSymbolsToPublishDirectory", false },
            { "SkipCopyingSymbolsToOutputDirectory", true }
        };
    }

    /// <summary>
    /// Gets or sets the path to the project that contains the app to build.
    /// </summary>
    [DataMember(Name = "project")]
    public List<string> Projects { get; }

    /// <summary>
    /// The configuration to compile
    /// </summary>
    public string Configuration { get; set; }

    /// <summary>
    /// The debug configuration to compile
    /// </summary>
    public string ConfigurationDebug { get; set; }

    /// <summary>
    /// Build the debug configuration. <c>true</c> by default.
    /// </summary>
    public bool BuildDebug { get; set; }

    /// <summary>
    /// Gets the extra properties that will be passed to MSBuild.
    /// </summary>
    public Dictionary<string, object> Properties { get; }

    public override string ToString()
    {
        return $"{base.ToString()} {nameof(Projects)}: [{string.Join(", ", Projects)}], {nameof(Configuration)}: {Configuration}, {nameof(Properties)} Count = {Properties.Count}";
    }
}