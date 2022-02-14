using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using DotNetReleaser.Configuration;
using DotNetReleaser.Runners;
using Microsoft.Build.Framework;

namespace DotNetReleaser;

public enum PackageOutputType
{
    Exe,
    WinExe,
    AppContainerExe,
    Library,
}

public partial class ReleaserApp 
{
    private async Task<ProjectPackageInfo?> LoadPackageInfo(string project)
    {
        var outputs = await RunMSBuild(project, ReleaserConstants.DotNetReleaserGetPackageInfo);
        if (outputs is null) return null;

        if (outputs.Count == 0)
        {
            Error($"Unexpected error. Unable to read build results for target `{ReleaserConstants.DotNetReleaserGetPackageInfo}`");
            return null;
        }

        try
        {
            var packageId = outputs.First(x => x.GetMetadata(ReleaserConstants.ItemSpecKind) == ReleaserConstants.PackageId).ItemSpec!;
            var assemblyName = outputs.First(x => x.GetMetadata(ReleaserConstants.ItemSpecKind) == ReleaserConstants.AssemblyName).ItemSpec!;
            var packageVersion = outputs.First(x => x.GetMetadata(ReleaserConstants.ItemSpecKind) == ReleaserConstants.PackageVersion).ItemSpec;
            var packageDescription = outputs.FirstOrDefault(x => x.GetMetadata(ReleaserConstants.ItemSpecKind) == ReleaserConstants.PackageDescription)?.ItemSpec;
            var packageLicenseExpression = outputs.FirstOrDefault(x => x.GetMetadata(ReleaserConstants.ItemSpecKind) == ReleaserConstants.PackageLicenseExpression)?.ItemSpec;
            var packageOutputType = outputs.FirstOrDefault(x => x.GetMetadata(ReleaserConstants.ItemSpecKind) == ReleaserConstants.PackageOutputType)?.ItemSpec?.Trim() ?? string.Empty;
            var packageProjectUrl = outputs.FirstOrDefault(x => x.GetMetadata(ReleaserConstants.ItemSpecKind) == ReleaserConstants.PackageProjectUrl)?.ItemSpec ?? $"{_config.GitHub.GetUrl()}";
            var isNuGetPackable = string.Equals(outputs.FirstOrDefault(x => x.GetMetadata(ReleaserConstants.ItemSpecKind) == ReleaserConstants.IsNuGetPackable)?.ItemSpec?.Trim(), "true", StringComparison.OrdinalIgnoreCase);
            var isTestProject = string.Equals(outputs.FirstOrDefault(x => x.GetMetadata(ReleaserConstants.ItemSpecKind) == ReleaserConstants.IsTestProject)?.ItemSpec?.Trim(), "true", StringComparison.OrdinalIgnoreCase);

            if (!Enum.TryParse<PackageOutputType>(packageOutputType, true, out var result))
            {
                Error($"Unsupported project type `{packageOutputType}` found for project `{project}`");
                return null;
            }
            
            return new ProjectPackageInfo(project, packageId, assemblyName, result, packageVersion, packageDescription ?? "No description found", packageLicenseExpression ?? "No license found", packageProjectUrl, isNuGetPackable, isTestProject);
        }
        catch (Exception ex)
        {
            Error($"Unexpected error while trying to read build results for target `{ReleaserConstants.DotNetReleaserGetPackageInfo}`. Outputs: {string.Join(", ", outputs.Select(x=> x.ItemSpec))}. Reason: {ex}");
            return null;
        }
    }

    /// <summary>
    /// This is the part that handles the packaging for tar, zip, deb, rpm
    /// </summary>
    private async Task<List<PackageEntry>?> PackPlatform(ProjectPackageInfo projectPackageInfo, bool publish, string rid, params PackageKind[] kinds)
    {
        var properties = new Dictionary<string, object>(_config.MSBuild.Properties)
        {
            { "RuntimeIdentifier", rid }, // Make sure that we have the last word on the target platform
        };

        var clock = Stopwatch.StartNew();
        var entries = new List<PackageEntry>();
        foreach (var kind in kinds)
        {
            var propertiesForTarget = new Dictionary<string, object>(properties);

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

            Info($"Building {FormatRidAndKind(rid, kind)}.");
            clock.Restart();

            // We need to explicitly restore the platform RID before trying to build it
            var restoreResult = await RunMSBuild(projectPackageInfo.ProjectFullPath, "Restore", propertiesForTarget);
            if (restoreResult is null)
            {
                // Stop on first error
                break;
            }

            // Create service
            if (_config.Service.Publish)
            {
                if (_config.Service.Systemd.Publish)
                {
                    if (kind == PackageKind.Deb || kind == PackageKind.Rpm)
                    {
                        Info($"Creating service file for {FormatRidAndKind(rid, kind)}.");
                        var systemdFile = await CreateSystemdServiceFile(projectPackageInfo);
                        if (systemdFile is null)
                        {
                            break;
                        }

                        propertiesForTarget[ReleaserConstants.DotNetReleaserSystemdFile] = systemdFile;
                        propertiesForTarget[ReleaserConstants.InstallService] = "true";

                        if (_config.Service.Systemd.CreateUser && !string.IsNullOrEmpty(_config.Service.Systemd.User))
                        {
                            propertiesForTarget[ReleaserConstants.UserName] = _config.Service.Systemd.User;
                            propertiesForTarget[ReleaserConstants.CreateUser] = "true";
                        }
                    }
                    else
                    {
                        Warn($"Creating a service is not supported for {FormatRidAndKind(rid, kind)}.");
                    }
                }
            }

            // Add dependencies for Debian/Rpm packages
            string? dependenciesPropertyName = null;
            List<PackageDependency>? dependencies = null;

            if (kind == PackageKind.Deb)
            {
                dependenciesPropertyName = ReleaserConstants.DotNetReleaserDebDependencies;
                dependencies = _config.Debian.Depends;
            }
            else if (kind == PackageKind.Rpm)
            {
                dependenciesPropertyName = ReleaserConstants.DotNetReleaserRpmDependencies;
                dependencies = _config.Rpm.Depends;
            }

            if (dependenciesPropertyName is not null && dependencies is not null && dependencies.Count > 0)
            {
                var dependenciesAsString = string.Join(";", _config.Debian.Depends.Select(x => string.Join("|", x.Names))).Trim();
                if (dependenciesAsString.Length > 0)
                {
                    propertiesForTarget[dependenciesPropertyName] = dependenciesAsString;
                }
            }

            // Publish
            var result = await RunMSBuild(projectPackageInfo.ProjectFullPath, target, propertiesForTarget);

            if (result is null)
            {
                // Stop on first error
                break;
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

    private string FormatRidAndKind(string rid, PackageKind kind) => $"target platform [{rid}] / [{kind.ToString().ToLowerInvariant()}] package";


    private string CopyToArtifacts(string source)
    {
        var dest = Path.Combine(_config.ArtifactsFolder, Path.GetFileName(source));
        File.Copy(source, dest);
        return dest;
    }

    private bool EnsureArtifactsFolders(bool forceArtifactsFolder)
    {
        // Make sure that the artifacts folder is created
        if (Directory.Exists(_config.ArtifactsFolder))
        {
            if (!forceArtifactsFolder)
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

}