using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Tomlyn.Serialization;

namespace DotNetReleaser.Configuration;

public class PackageDependency
{
    public PackageDependency()
    {
        Names = new List<string>();
    }

    [JsonPropertyName("name")]
    [TomlSingleOrArray]
    public List<string> Names { get; }
}