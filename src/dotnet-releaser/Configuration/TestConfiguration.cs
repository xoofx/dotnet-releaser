using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DotNetReleaser.Configuration;

/// <summary>
/// Configures the `[test]` section.
/// </summary>
public class TestConfiguration
{
    public TestConfiguration()
    {
        Enable = true;
        RunTests = true;
        RunTestsForDebug = false;
        Runs = new List<TestRunConfiguration>();
    }

    /// <summary>
    /// Enable or disable running tests. Default is `true`.
    /// </summary>
    public bool Enable { get; set; }

    /// <summary>
    /// Run the tests with the release configuration. `true` by default.
    /// </summary>
    public bool RunTests { get; set; }

    /// <summary>
    /// Run the tests for the debug configuration. `false` by default.
    /// </summary>
    public bool RunTestsForDebug { get; set; }

    /// <summary>
    /// Gets additional test runs with custom configurations, settings, properties, arguments, or environment variables.
    /// </summary>
    public List<TestRunConfiguration> Runs { get; }
}

/// <summary>
/// Configures an additional invocation of <c>dotnet test</c>.
/// </summary>
public sealed class TestRunConfiguration
{
    public TestRunConfiguration()
    {
        Name = string.Empty;
        Arguments = new List<string>();
        Properties = new Dictionary<string, object>();
        EnvironmentVariables = new Dictionary<string, string?>();
    }

    /// <summary>
    /// Gets or sets a descriptive name displayed in the log for this test run.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the build configuration to test. When omitted, the release configuration from
    /// <c>[msbuild]</c> is used.
    /// </summary>
    [JsonPropertyName("config")]
    public string? Configuration { get; set; }

    /// <summary>
    /// Gets or sets the path to a <c>.runsettings</c> file. Relative paths are resolved from the
    /// directory containing the dotnet-releaser configuration file.
    /// </summary>
    public string? Settings { get; set; }

    /// <summary>
    /// Gets additional arguments passed to <c>dotnet test</c>.
    /// </summary>
    [JsonPropertyName("args")]
    public List<string> Arguments { get; }

    /// <summary>
    /// Gets additional MSBuild properties passed to <c>dotnet test</c>.
    /// </summary>
    public Dictionary<string, object> Properties { get; }

    /// <summary>
    /// Gets environment variables set for the test process.
    /// </summary>
    [JsonPropertyName("envs")]
    public Dictionary<string, string?> EnvironmentVariables { get; }
}


