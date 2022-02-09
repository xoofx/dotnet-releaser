using System.Collections.Generic;

namespace DotNetReleaser.Configuration;

public class RpmConfiguration
{
    public RpmConfiguration()
    {
        Depends = new List<PackageDependency>();
    }

    public List<PackageDependency> Depends { get; }
}