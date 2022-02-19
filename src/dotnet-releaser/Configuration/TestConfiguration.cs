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
    /// Run the tests for the debug configuration. `true` by default.
    /// </summary>
    public bool RunTestsForDebug { get; set; }
}



