using System.Collections.Generic;

namespace DotNetReleaser.Configuration;

/// <summary>
/// Configures the `[coverage]` section.
/// </summary>
public class CoverageConfiguration
{
    public CoverageConfiguration()
    {
        Enable = true;

        Package = "coverlet.collector";
        Version = "3.1.*";
        SingleHit = false;
        SourceLink = true;
        IncludeTestAssembly = true;
        SkipAutoProps = true;
        DoesNotReturnAttribute = true;
        DeterministicReport = false;
        Format = new List<string>();
        Exclude = new List<string>();
        ExcludeByFile = new List<string>();
        Include = new List<string>();
        IncludeDirectory = new List<string>();
    }

    /// <summary>
    /// Enable running coverage during tests. Default is `true`.
    /// </summary>
    public bool Enable { get; set; }

    /// <summary>
    /// Gets or sets the package that will be used for collecting. Default is `coverlet.collector`
    /// </summary>
    public string Package { get; set; }

    /// <summary>
    /// Gets or sets the version of the package that will be used for collecting.
    /// </summary>
    public string Version { get; set; }

    public List<string> Format { get; set; }

    public bool SingleHit { get; set; }

    public bool SourceLink { get; set; }

    public bool IncludeTestAssembly { get; set; }

    public bool SkipAutoProps { get; set; }

    public bool DoesNotReturnAttribute { get; set; }

    public bool DeterministicReport { get; set; }

    public List<string> Exclude { get; }

    public List<string> ExcludeByFile { get; }

    public List<string> Include { get; }

    public List<string> IncludeDirectory { get; }

    public void AddDefaults()
    {
        if (Format.Count == 0) Format.Add("cobertura");
    }
}