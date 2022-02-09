using System.Collections.Generic;

namespace DotNetReleaser.Configuration;

public class DebianConfiguration
{
    public DebianConfiguration()
    {
        Depends = new List<PackageDependency>();
    }

    public List<PackageDependency> Depends { get; }
}