using System.Collections.Generic;

namespace DotNetReleaser.Configuration;

public class ChangelogCategory
{
    public ChangelogCategory()
    {
        Title = string.Empty;
        Labels = new List<string>();
        Exclude = new ChangelogFilter();
    }

    public ChangelogCategory(int order, string title, params string[] labels)
    {
        Order = order;
        Title = title;
        Labels = new List<string>(labels);
        Exclude = new ChangelogFilter();
    }

    public int Order { get; set; }

    public string Title { get; set; }
    
    public List<string> Labels { get; }

    public ChangelogFilter Exclude { get; }
}