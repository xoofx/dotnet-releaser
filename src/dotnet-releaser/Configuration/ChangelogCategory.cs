using System.Collections.Generic;

namespace DotNetReleaser.Configuration;

public class ChangelogCategory
{
    public ChangelogCategory()
    {
        Title = string.Empty;
        Labels = new List<string>();
        Exclude = new ChangelogExclude();
    }

    public ChangelogCategory(string title, params string[] labels)
    {
        Title = title;
        Labels = new List<string>(labels);
        Exclude = new ChangelogExclude();
    }
    
    public string Title { get; set; }

    public List<string> Labels { get; }


    public ChangelogExclude Exclude { get; }
}