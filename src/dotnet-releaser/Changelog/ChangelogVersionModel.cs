using NuGet.Versioning;

namespace DotNetReleaser.Changelog;

public class ChangelogVersionModel
{
    public static readonly ChangelogVersionModel Empty = new ChangelogVersionModel("", new NuGetVersion(0, 0, 0), null);

    private readonly NuGetVersion? _version;

    public ChangelogVersionModel(string tagPrefix, NuGetVersion? version, string? sha)
    {
        _version = version;
        Tag = version is not null ? $"{tagPrefix}{version.OriginalVersion}" : null;
        Sha = sha;
    }

    public int Major => _version?.Major ?? 0;

    public int Minor => _version?.Minor ?? 0;

    public int Patch => _version?.Patch ?? 0;

    public int Revision => _version?.Revision ?? 0;

    public bool HasVersion => _version is not null;

    public string? Tag { get; }

    public string? Sha { get; }

    public override string ToString() => _version?.OriginalVersion ?? Sha ?? "0.0.0 (no version)";
}