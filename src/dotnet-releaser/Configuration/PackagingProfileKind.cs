namespace DotNetReleaser.Configuration;

public enum PackagingProfileKind
{
    /// <summary>
    /// Target all supported platforms and architecture with NuGet + all packages (debian/rpm) + Homebrew.
    /// </summary>
    Default,

    /// <summary>
    /// Explicitly user defined.
    /// </summary>
    Custom,
}