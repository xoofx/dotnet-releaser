using System.IO;

namespace DotNetReleaser;

public record PackageEntry(string Name, PackageKind Kind, string Path, string RuntimeId, string Mime, string Sha256, bool Publish)
{
    public long GetFileSize() => new FileInfo(Path).Length;
}