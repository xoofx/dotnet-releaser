using System.Collections.Generic;
using System.IO;

namespace DotNetReleaser;

public record ProjectPackageInfo(string ProjectFullPath, string PackageId, string AssemblyName, PackageOutputType OutputType, string Version, string Description, string License, string ProjectUrl, bool IsPackable, bool IsTestProject,
    string[] ProjectReferences, TargetFrameworkInfo TargetFrameworkInfo, bool IsWebApp)
{
    public string ProjectName => Path.GetFileNameWithoutExtension(ProjectFullPath);

    public string? WebAppPublishProfile { get; set; }
}