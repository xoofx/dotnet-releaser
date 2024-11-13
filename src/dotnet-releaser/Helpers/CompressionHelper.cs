using System.Formats.Tar;
using System.IO;
using System.IO.Compression;

namespace DotNetReleaser.Helpers;

internal static class CompressionHelper
{
    public static string? MakeTarGz(ProjectPackageInfo projectPackageInfo, string publishDir, string artifactsFolder, string rid)
    {
        string? outputPath = null;
        var publishPath = Path.Combine(Path.GetDirectoryName(projectPackageInfo.ProjectFullPath) ?? "", publishDir);
        if (Directory.Exists(publishPath))
        {
            var gzipPath = Path.GetFullPath(Path.Combine(artifactsFolder, 
                projectPackageInfo.Name + "." + projectPackageInfo.Version + "." + rid + ".tar.gz"));
            if (File.Exists(gzipPath))
            {
                return null; // file already exists
            }
            using FileStream fs = new(gzipPath, FileMode.CreateNew, FileAccess.Write);
            using GZipStream gz = new(fs, CompressionMode.Compress, leaveOpen: true);
            {
                TarFile.CreateFromDirectory(publishPath, gz, includeBaseDirectory: false);
            }
            outputPath = gzipPath;
        }
        return outputPath;
    }

    public static string? MakeZip(ProjectPackageInfo projectPackageInfo, string publishDir, string artifactsFolder, string rid)
    {
        string? outputPath = null;
        var publishPath = Path.Combine(Path.GetDirectoryName(projectPackageInfo.ProjectFullPath) ?? "", publishDir);
        if (Directory.Exists(publishPath))
        {
            var zipPath = Path.GetFullPath(Path.Combine(artifactsFolder, 
                projectPackageInfo.Name + "." + projectPackageInfo.Version + "." + rid + ".zip"));
            if (File.Exists(zipPath))
            {
                return null; // file already exists
            }
            using FileStream fs = new(zipPath, FileMode.CreateNew, FileAccess.Write);
            {
                ZipFile.CreateFromDirectory(publishPath, fs, compressionLevel: CompressionLevel.Optimal, includeBaseDirectory: false);
            }
            outputPath = zipPath;
        }
        return outputPath;
    }
}