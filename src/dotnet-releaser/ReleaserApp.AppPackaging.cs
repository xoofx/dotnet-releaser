using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using DotNetReleaser.Configuration;
using DotNetReleaser.Logging;
using Spectre.Console;

namespace DotNetReleaser;

public partial class ReleaserApp 
{
    private async Task<bool> BuildAppPackages(BuildInformation buildInformation)
    {
        foreach (var packageInfo in buildInformation.GetAllPackableProjects())
        {
            var entriesToPublish = await BuildAppPackages(buildInformation, packageInfo);

            // Exit if we have any errors.
            if (HasErrors)
            {
                return false;
            }

            var buildPackageInformation = buildInformation.GetOrCreateBuildPackageInformation(packageInfo);
            buildPackageInformation.AppPackages.AddRange(entriesToPublish);
        }

        return true;
    }

    private async Task<List<AppPackageInfo>> BuildAppPackages(BuildInformation buildInformation, ProjectPackageInfo packageInfo)
    {
        var entriesToPublish = new List<AppPackageInfo>();

        // No AppPackages to build for libraries
        if (packageInfo.OutputType == PackageOutputType.Library || _config.Packs.Count == 0)
        {
            return entriesToPublish;
        }

        _logger.LogStartGroup($"App Packaging {packageInfo.PackageId} - {packageInfo.Version}");

        var table = new Table();
        table.AddColumn("Platform");
        table.AddColumn("Packages");
        table.AddColumn(new TableColumn("Publish?").Centered());
        table.Border = _tableBorder;

        foreach (var pack in _config.Packs)
        {
            var rids = string.Join(", ", pack.RuntimeIdentifiers.Select(x => x.ToString().ToLowerInvariant()));
            var kinds = string.Join(", ", pack.Kinds.Select(x => x.ToString().ToLowerInvariant()));
            var publish = pack.Publish ? "x" : string.Empty;
            table.AddRow(rids, kinds, publish);
        }

        _logger.InfoMarkup("Platforms and Packages Configured:", table);
        try
        {
            if (buildInformation.BuildKind == BuildKind.Build && _skipAppPackagesForBuildOnly)
            {
                Info("Skipping building app packages during build-only.");
            }
            else
            {
                foreach (var pack in _config.Packs)
                {
                    foreach (var rid in pack.RuntimeIdentifiers)
                    {
                        var list = await PackPlatform(packageInfo, pack.Publish, rid, pack.Kinds.ToArray());
                        if (HasErrors) goto exitPackOnError; // break on first errors

                        if (list is not null && pack.Publish)
                        {
                            entriesToPublish.AddRange(list);
                        }
                    }
                }

                exitPackOnError:
                if (HasErrors)
                {
                    Error($"Error while building platform packages for `{packageInfo.PackageId}`.");
                }
            }
        }
        finally
        {
            _logger.LogEndGroup();
        }

        return entriesToPublish;
    }

    /// <summary>
    /// This is the part that handles the packaging for tar, zip, deb, rpm
    /// </summary>
    private async Task<List<AppPackageInfo>?> PackPlatform(ProjectPackageInfo projectPackageInfo, bool publish, string rid, params PackageKind[] kinds)
    {
        var properties = new Dictionary<string, object>(_config.MSBuild.Properties)
        {
            { "RuntimeIdentifier", rid }, // Make sure that we have the last word on the target platform
        };

        var clock = Stopwatch.StartNew();
        var entries = new List<AppPackageInfo>();
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

            var entry = new AppPackageInfo(
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
        var fileName = Path.GetFileName(source);
        var dest = Path.GetFullPath(Path.Combine(_config.ArtifactsFolder, fileName));
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