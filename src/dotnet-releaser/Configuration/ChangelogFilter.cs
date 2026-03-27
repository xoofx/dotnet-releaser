using System.Collections.Generic;
using Tomlyn.Serialization;

namespace DotNetReleaser.Configuration;

public class ChangelogFilter
{
    public ChangelogFilter()
    {
        Labels = new List<string>();
        Contributors = new List<string>();
    }

    [TomlSingleOrArray]
    public List<string> Labels { get; }

    [TomlSingleOrArray]
    public List<string> Contributors { get; }
}