using System.Collections.Generic;

namespace DotNetReleaser.Configuration;

public class ChangelogFilter
{
    public ChangelogFilter()
    {
        Labels = new List<string>();
        Contributors = new List<string>();
    }

    public List<string> Labels { get; }

    public List<string> Contributors { get; }
}