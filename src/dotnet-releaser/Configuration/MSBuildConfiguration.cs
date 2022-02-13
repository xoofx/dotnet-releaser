using System.Collections.Generic;

namespace DotNetReleaser.Configuration;

/// <summary>
/// MSBuild Configuration.
/// </summary>
public class MSBuildConfiguration : ConfigurationBase
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
        return $"{base.ToString()} {nameof(Project)}: {Project}, {nameof(Configuration)}: {Configuration}, {nameof(Properties)} Count = {Properties.Count}";
    }
}