using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DotNetReleaser.Runners;

namespace DotNetReleaser.Tests;

public class MSBuildRunnerTests
{
    private readonly string _helloWorldFolder = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "tests", "HelloWorld"));

    [Test]
    public async Task RunIgnoresTheMSBuildEnvironmentOfTheParentProcess()
    {
        // A process started by the dotnet CLI or by MSBuild (e.g. `dotnet test`) inherits the paths of that SDK.
        // The msbuild started by dotnet-releaser must resolve the SDK selected by the global.json of the project instead.
        var artifactsFolder = Path.Combine(Path.GetTempPath(), $"dotnet-releaser-{Guid.NewGuid():N}");
        var bogusPath = Path.Combine(artifactsFolder, "bogus-sdk");
        var names = new[] { "MSBUILD_EXE_PATH", "MSBuildExtensionsPath", "MSBuildSDKsPath" };
        var previousValues = names.ToDictionary(x => x, Environment.GetEnvironmentVariable);
        try
        {
            foreach (var name in names)
            {
                Environment.SetEnvironmentVariable(name, bogusPath);
            }

            using var runner = new MSBuildRunner(artifactsFolder)
            {
                Project = Path.Combine(_helloWorldFolder, "HelloWorld.csproj"),
                WorkingDirectory = _helloWorldFolder,
                Arguments = { "-getProperty:MSBuildSDKsPath" }
            };
            var result = await runner.Run(new MockSimpleLogger());

            Assert.AreEqual(0, result.CommandResult.ExitCode, result.Output);
            StringAssert.DoesNotContain(bogusPath, result.Output);
        }
        finally
        {
            foreach (var (name, value) in previousValues)
            {
                Environment.SetEnvironmentVariable(name, value);
            }

            if (Directory.Exists(artifactsFolder))
            {
                Directory.Delete(artifactsFolder, true);
            }
        }
    }
}
