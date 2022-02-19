using System.Collections.Generic;

namespace DotNetReleaser;

public record ProjectPackageInfo(string ProjectFullPath, string Name, string AssemblyName, PackageOutputType OutputType, string Version, string Description, string License, string ProjectUrl, bool IsPackable, bool IsTestProject, string[] ProjectReferences);