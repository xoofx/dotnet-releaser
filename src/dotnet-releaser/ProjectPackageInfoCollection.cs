namespace DotNetReleaser;

public record ProjectPackageInfoCollection(ProjectPackageInfo[] Packages, string? SolutionFile);