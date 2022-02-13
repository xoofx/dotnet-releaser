namespace DotNetReleaser;

public record PackageInfo(string Name, string ExeName, string Version, string Description, string License, string ProjectUrl, bool IsNuGetPackable);