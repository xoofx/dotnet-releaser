using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CliWrap;
using DotNetReleaser.Helpers;
using NUnit.Framework;

namespace DotNetReleaser.Tests
{
    public class BasicTests
    {
        private readonly string _helloWorldFolder = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "tests", "HelloWorld"));
        private readonly string _releaserExe = Path.Combine(AppContext.BaseDirectory, RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet-releaser.exe" : "dotnet-releaser");
        private readonly string _configurationFile;
        private readonly string _artifactsFolder;

        public BasicTests()
        {
            _configurationFile = Path.Combine(_helloWorldFolder, "dotnet-releaser.toml");
            _artifactsFolder = Path.Combine(_helloWorldFolder, "artifacts-dotnet-releaser");
        }


        [Test]
        public async Task TestNew()
        {
            EnsureTestsFolder();
            File.Delete(_configurationFile);
            await CreateConfiguration();
            File.Delete(_configurationFile);
        }

        [Test]
        public async Task TestBuild()
        {
            EnsureTestsFolder();

            File.Delete(_configurationFile);

            await CreateConfiguration();

            var config = await File.ReadAllTextAsync(_configurationFile);

            if (Directory.Exists(_artifactsFolder))
            {
                Directory.Delete(_artifactsFolder, true);
            }

            config = "profile = \"custom\"" + Environment.NewLine + config;
            config += @"[[pack]]
rid = ""win-x64""
kinds = [""zip""]
[[pack]]
rid = ""linux-x64""
kinds = [""tar"", ""deb""]
";
            config = config.Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
            await File.WriteAllTextAsync(_configurationFile, config);

            var resultBuild = await CliWrap.Cli.Wrap(_releaserExe)
                .WithArguments("build --force dotnet-releaser.toml")
                .WithStandardOutputPipe(PipeTarget.ToDelegate(x => Console.Out.WriteLine(x)))
                .WithStandardErrorPipe(PipeTarget.ToDelegate(x => Console.Error.WriteLine(x)))
                .WithWorkingDirectory(_helloWorldFolder).ExecuteAsync();

            Assert.True(Directory.Exists(_artifactsFolder));

            var files = Directory.GetFiles(_artifactsFolder).Select(Path.GetFileName).OrderBy(x => x).ToList();

            var expectedFiles = new List<string>()
            {
                "HelloWorld.0.1.0.linux-x64.deb",
                "HelloWorld.0.1.0.linux-x64.tar.gz",
                "HelloWorld.0.1.0.nupkg",
                "HelloWorld.0.1.0.win-x64.zip",
            }.OrderBy(x => x).ToList();

            foreach (var file in files)
            {
                Console.WriteLine($"-> {file}");
            }

            Assert.AreEqual(expectedFiles, files);

            Directory.Delete(_artifactsFolder, true);
            File.Delete(_configurationFile);
        }


        [TestCase("grpc-curl", "GrpcCurl")]
        [TestCase("ThisIsFine", "ThisIsFine")]
        [TestCase("this_is_fine", "This_Is_Fine")]
        [TestCase("hello_world1", "Hello_World1")]
        public void TestHomebrewNaming(string appName, string expected)
        {
            var className = RubyHelper.GetRubyClassNameFromAppName(appName);
            Assert.AreEqual(expected, className);
        }

        private void EnsureTestsFolder()
        {
            Assert.True(Directory.Exists(_helloWorldFolder), $"The folder `{_helloWorldFolder}` was not found");
        }

        private async Task CreateConfiguration()
        {
            var resultNew = await CliWrap.Cli.Wrap(_releaserExe)
                .WithArguments("new --user=xoofx --repo=HelloWorld --force --project=HelloWorld.csproj dotnet-releaser.toml")
                .WithStandardOutputPipe(PipeTarget.ToDelegate(x => Console.Out.WriteLine(x)))
                .WithStandardErrorPipe(PipeTarget.ToDelegate(x => Console.Error.WriteLine(x)))
                .WithWorkingDirectory(_helloWorldFolder).ExecuteAsync();
        }
    }
}