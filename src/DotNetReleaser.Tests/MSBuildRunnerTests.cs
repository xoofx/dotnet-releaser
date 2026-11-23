using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DotNetReleaser.Runners;

namespace DotNetReleaser.Tests;

public class MSBuildRunnerTests
{
    private readonly string _helloWorldFolder = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "tests", "HelloWorld"));

    [TestCase("Skipped", true, "no TargetFinished event was received")]
    [TestCase("Empty", true, "the TargetFinished event contained no output items")]
    [TestCase("Empty", false, null)]
    [TestCase("WithOutput", true, null)]
    public async Task RunDiagnosesMissingRequiredTargetOutputs(string target, bool requireOutputs, string? expectedIssue)
    {
        var artifacts = Directory.CreateTempSubdirectory("dotnet-releaser-");
        try
        {
            var project = Path.Combine(artifacts.FullName, "test.proj");
            await File.WriteAllTextAsync(project, """
                <Project>
                  <Target Name="Skipped" Condition="'a' == 'b'" />
                  <Target Name="Empty" />
                  <Target Name="WithOutput" Returns="@(Result)">
                    <ItemGroup><Result Include="value" /></ItemGroup>
                  </Target>
                </Project>
                """);

            var logger = new MockSimpleLogger();
            using var runner = new MSBuildRunner(artifacts.FullName)
            {
                Project = project,
                Targets = { target },
                Properties = { ["TargetFramework"] = "net10.0" },
                RequireTargetOutputs = requireOutputs
            };
            var result = await runner.Run(logger);

            Assert.AreEqual(0, result.CommandResult.ExitCode, logger.Output.ToString());
            Assert.AreEqual(expectedIssue is not null, logger.HasErrors, logger.Output.ToString());
            if (expectedIssue is not null)
            {
                var output = logger.Output.ToString();
                StringAssert.Contains(project, output);
                StringAssert.Contains($"`{target}`", output);
                StringAssert.Contains("configuration `Release`, target framework `net10.0`", output);
                StringAssert.Contains("MSBuild exited with code 0", output);
                StringAssert.Contains(expectedIssue, output);
                StringAssert.Contains("BuildStarted: 1, BuildFinished: 1", output);
                StringAssert.Contains(artifacts.GetFiles("*.binlog", SearchOption.AllDirectories).Single().FullName, output);
            }
        }
        finally
        {
            artifacts.Delete(true);
        }
    }

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
