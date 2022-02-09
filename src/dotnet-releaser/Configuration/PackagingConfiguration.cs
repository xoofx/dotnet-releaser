using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DotNetReleaser.Configuration;

public class PackagingConfiguration : ConfigurationBase
{
    public PackagingConfiguration()
    {
        RuntimeIdentifiers = new List<string>();
        Kinds = new List<PackageKind>();
    }

    [DataMember(Name = "rid")]
    public List<string> RuntimeIdentifiers { get; }

    [DataMember(Name = "kinds")]
    public List<PackageKind> Kinds { get; }

    public static string ToStringRidAndKinds(List<string> rids, List<PackageKind> kinds) => $"platform{(rids.Count > 1 ? "s" : string.Empty)} [{string.Join(", ", rids)}] with [{string.Join(", ", kinds)}] package{(kinds.Count > 1 ? "s" : string.Empty)}";

    public override string ToString()
    {
        return $"{base.ToString()}, {ToStringRidAndKinds(RuntimeIdentifiers, Kinds)}";
    }
}