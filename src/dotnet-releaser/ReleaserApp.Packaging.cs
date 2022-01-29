using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using DotNetReleaser.Runners;
using Microsoft.Build.Framework;

namespace DotNetReleaser;

public partial class ReleaserApp 
{
    private async Task<PackageInfo?> LoadPackageInfo()
    {
        var outputs = await RunMSBuild(ReleaserConstants.DotNetReleaserGetPackageInfo);
        if (outputs is null) return null;

        var packageId = outputs.First(x => x.GetMetadata(ReleaserConstants.ItemSpecKind) == ReleaserConstants.PackageId).ItemSpec;
        var packageVersion = outputs.First(x => x.GetMetadata(ReleaserConstants.ItemSpecKind) == ReleaserConstants.PackageVersion).ItemSpec;
        var packageDescription = outputs.FirstOrDefault(x => x.GetMetadata(ReleaserConstants.ItemSpecKind) == ReleaserConstants.PackageDescription)?.ItemSpec;
        var packageLicenseExpression = outputs.FirstOrDefault(x => x.GetMetadata(ReleaserConstants.ItemSpecKind) == ReleaserConstants.PackageLicenseExpression)?.ItemSpec;
        var packageOutputType = outputs.FirstOrDefault(x => x.GetMetadata(ReleaserConstants.ItemSpecKind) == ReleaserConstants.PackageOutputType)?.ItemSpec;
        var packageProjectUrl = outputs.FirstOrDefault(x => x.GetMetadata(ReleaserConstants.ItemSpecKind) == ReleaserConstants.PackageProjectUrl)?.ItemSpec ?? $"{_config.GitHub.GetUrl()}";

        // Check that the output type is actually an exe
        if (packageOutputType is null || !packageOutputType.Contains("exe", StringComparison.OrdinalIgnoreCase))
        {
            Error($"The project is not an executable but is of type {packageOutputType}. This tool supports only packaging executables.");
            return null;
        }

        return new PackageInfo(packageId, packageVersion, packageDescription ?? "No description found", packageLicenseExpression ?? "No license found", packageProjectUrl);
    }

    /// <summary>
    /// This is the part that handles the packaging for tar, zip, deb, rpm
    /// </summary>
    private async Task<List<PackageEntry>?> PackPlatform(bool publish, string rid, params PackageKind[] kinds)
    {
        var properties = new Dictionary<string, object>(_config.MSBuild.Properties)
        {
            { "RuntimeIdentifier", rid }, // Make sure that we have the last word on the target platform
        };

        var clock = Stopwatch.StartNew();
        var entries = new List<PackageEntry>();
        foreach (var kind in kinds)
        {
            string target;
            string mime;
            switch (kind)
            {
                case PackageKind.Deb:
                    target = ReleaserConstants.DotNetReleaserPublishAndCreateDeb;
                    mime = "application/vnd.debian.binary-package";
                    break;
                case PackageKind.Rpm:
                    target = ReleaserConstants.DotNetReleaserPublishAndCreateRpm;
                    mime = "application/x-rpm";
                    break;
                case PackageKind.Zip:
                    target = ReleaserConstants.DotNetReleaserPublishAndCreateZip;
                    mime = "application/zip";
                    break;
                case PackageKind.Tar:
                    target = ReleaserConstants.DotNetReleaserPublishAndCreateTar; // CreateTarball
                    mime = "application/gzip";
                    break;
                case PackageKind.Setup:
                    target = ReleaserConstants.DotNetReleaserPublishAndCreateSetup; // not yet supported
                    mime = "application/vnd.microsoft.portable-executable";
                    break;
                default:
                    throw new ArgumentException($"Invalid kind {kind}", nameof(kind));
            }

            Info($"Building target platform [{rid}] / [{kind.ToString().ToLowerInvariant()}] package");
            clock.Restart();

            // We need to explicitly restore the platform RID before trying to build it
            var restoreResult = await RunMSBuild("Restore", properties);
            if (restoreResult is null)
            {
                continue;
            }

            // Publish
            var result = await RunMSBuild(target, properties);

            if (result is null)
            {
                continue;
            }

            // Copy the file to the output
            var path = result[0].ItemSpec;
            path = CopyToArtifacts(path);

            var sha256 = string.Join("", SHA256.HashData(await File.ReadAllBytesAsync(path)).Select(x => x.ToString("x2")));

            var entry = new PackageEntry(
                Path.GetFileName(path),
                kind,
                path,
                rid,
                mime,
                sha256,
                publish);
            
            entries.Add(entry);

            Info($"Build successful in {clock.Elapsed.TotalSeconds}s for platform [{rid}] / [{kind.ToString().ToLowerInvariant()}] package: {entry.Path}");
        }

        return entries;
    }

    private record PackageEntry(string Name, PackageKind Kind, string Path, string RuntimeId, string Mime, string Sha256, bool Publish)
    {
        public long GetFileSize() => new FileInfo(Path).Length;
    }

    private string CopyToArtifacts(string source)
    {
        var dest = Path.Combine(_config.ArtifactsFolder, Path.GetFileName(source));
        File.Copy(source, dest);
        return dest;
    }

    private async Task<List<ITaskItem>?> RunMSBuild(string target, IDictionary<string, object>? properties = null)
    {
        using var program = new MSBuildRunner()
        {
            Project = _config.MSBuild.Project,
            Configuration = _config.MSBuild.Configuration,
            CustomAfterMicrosoftCommonTargets = DotNetReleaserConfigFile,
            Targets =
            {
                target
            }
        };

        // Copy properties
        if (properties is not null)
        {
            foreach (var property in properties)
            {
                program.Properties[property.Key] = property.Value;
            }
        }
        
        var result = await program.Run(this);

        if (result.TargetOutputs.TryGetValue(target, out var outputs))
        {
            return outputs;
        }
        else if (!result.HasErrors)
        {
            return new List<ITaskItem>();
        }

        return null;
    }

    private bool EnsureArtifactsFolders()
    {
        // Make sure that the artifacts folder is created
        if (Directory.Exists(_config.ArtifactsFolder))
        {
            if (!_forceArtifactsFolder)
            {
                Error($"The artifacts folder `{_config.ArtifactsFolder}` already exists. Use `--force` to delete/recreate this folder during a `build`/`publish`.");
                return false;
            }
            else
            {
                try
                {
                    Directory.Delete(_config.ArtifactsFolder, true);
                }
                catch
                {
                    Warn($"Unable to delete artifacts folder `{_config.ArtifactsFolder}`");
                }
            }
        }

        try
        {
            Directory.CreateDirectory(_config.ArtifactsFolder);
        }
        catch
        {
            Error("Unable to create artifacts folder `{_config.ArtifactsFolder}`");
            return false;
        }

        return true;
    }

    private record PackageInfo(string Name, string Version, string Description, string License, string ProjectUrl);
}