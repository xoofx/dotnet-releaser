using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DotNetReleaser.Configuration;

public class PackageDependency
{
    public PackageDependency()
    {
        Names = new List<string>();
    }

    [DataMember(Name = "name")]
    public List<string> Names { get; }
}