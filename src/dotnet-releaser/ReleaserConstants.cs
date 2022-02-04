namespace DotNetReleaser;

/// <summary>
/// Constants associated with the MSBuild target file app-releaser.targets
/// </summary>
static class ReleaserConstants
{
    public const string DotNetReleaserFileName = "dotnet-releaser.targets";

    public const string DotNetReleaserPackAndGetNuGetPackOutput = nameof(DotNetReleaserPackAndGetNuGetPackOutput);
    public const string DotNetReleaserGetPackageInfo = nameof(DotNetReleaserGetPackageInfo);
    public const string DotNetReleaserPublishAndCreateDeb = nameof(DotNetReleaserPublishAndCreateDeb);
    public const string DotNetReleaserPublishAndCreateRpm = nameof(DotNetReleaserPublishAndCreateRpm);
    public const string DotNetReleaserPublishAndCreateTar = nameof(DotNetReleaserPublishAndCreateTar);
    public const string DotNetReleaserPublishAndCreateZip = nameof(DotNetReleaserPublishAndCreateZip);
    public const string DotNetReleaserPublishAndCreateSetup = nameof(DotNetReleaserPublishAndCreateSetup);
    public const string DotNetReleaserSystemdFile = nameof(DotNetReleaserSystemdFile);
    public const string DotNetReleaserDebDependencies = nameof(DotNetReleaserDebDependencies);
    public const string DotNetReleaserRpmDependencies = nameof(DotNetReleaserRpmDependencies);
    public const string InstallService = nameof(InstallService); // For packaging service

    public const string ItemSpecKind = "Kind";
    public const string PackageId = nameof(PackageId);
    public const string ExeName = nameof(ExeName);
    public const string PackageVersion = nameof(PackageVersion);
    public const string PackageDescription = nameof(PackageDescription);
    public const string PackageLicenseExpression = nameof(PackageLicenseExpression);
    public const string PackageOutputType = nameof(PackageOutputType);
    public const string PackageProjectUrl = nameof(PackageProjectUrl);
    public const string IsNuGetPackable = nameof(IsNuGetPackable);
}