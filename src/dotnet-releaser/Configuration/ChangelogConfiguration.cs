using System.Collections.Generic;

namespace DotNetReleaser.Configuration;

public class ChangelogConfiguration : ConfigurationBase
{
    public ChangelogConfiguration()
    {
        Auto = true;
        Version = @"^#+\s+v?((\d+\.)*(\d+))";
        Categories = new List<ChangelogCategory>();
        NameTemplate = "{{ version.tag }}";
        OwnersCommitChangeTemplate = "- {{ commit.title }} ({{ commit.sha}})";
        OwnersPullRequestChangeTemplate = "- {{ pr.title }} (#{{ pr.number }})";
        CommitChangeTemplate = "- {{ commit.title }} ({{ commit.sha}}) @{{ commit.author }}";
        PullRequestChangeTemplate = "- {{ pr.title }} (#{{ pr.number }}) @{{ pr.author }}";
        IncludeCommits = true;
        //ChangeTitleEscape = @"\<*_&@";
        Owners = new List<string>();
        Autolabeler = new List<ChangelogAutolabeler>();
        Replacers = new List<ChangelogReplacer>();
        Exclude = new ChangelogExclude();
        IncludeLabels = new List<string>();
        BodyTemplate = @"# Changes

{{ changes }}

** Full Changelog**: {{ url_full_changelog_compare_changes }}
";
    }
    public string? Path { get; set; }

    public string Version { get; set; }

    public bool Auto { get; set; }

    public string NameTemplate { get; set; }

    public string BodyTemplate { get; set; }

    public bool IncludeCommits { get; set; }

    public List<string> Owners { get; set; }

    public string OwnersCommitChangeTemplate { get; set; }

    public string OwnersPullRequestChangeTemplate { get; set; }
    
    public string CommitChangeTemplate { get; set; }

    public string PullRequestChangeTemplate { get; set; }

    public bool DisableDraftForBuild { get; set; }

    public ChangelogExclude Exclude { get; }

    public List<string> IncludeLabels { get; }

    public List<ChangelogAutolabeler> Autolabeler { get; }

    public List<ChangelogReplacer> Replacers { get; }
    
    public List<ChangelogCategory> Categories { get; }

    public void InitializeDefaults(DevHostingConfiguration hostingConfiguration)
    {
        if (Owners.Count == 0 && !string.IsNullOrEmpty(hostingConfiguration.User))
        {
            Owners.Add(hostingConfiguration.User);
        }
        
        if (Categories.Count == 0)
        {
            Categories.Add(new ChangelogCategory("## 🚨 Breaking Changes", "breaking-change"));
            Categories.Add(new ChangelogCategory("## ✨ New Features", "new-feature", "feature"));
            Categories.Add(new ChangelogCategory("## 🐛 Bug Fixes", "bugfix", "fix", "bug"));
            Categories.Add(new ChangelogCategory("## 🚀 Enhancements", "enhancement", "refactor", "performance"));
            Categories.Add(new ChangelogCategory("## 🧰 Maintenance", "maintenance", "ci"));
            Categories.Add(new ChangelogCategory("## 🏭 Tests", "tests"));
            Categories.Add(new ChangelogCategory("## 🛠 Examples", "examples"));
            Categories.Add(new ChangelogCategory("## 📚 Documentation", "documentation"));
            Categories.Add(new ChangelogCategory("## 🌎 Accessibility", "translations", "accessibility"));
            Categories.Add(new ChangelogCategory("## 📦 Dependencies", "dependencies"));
            Categories.Add(new ChangelogCategory("## 🧰 Misc", "misc"));
        }

        if (Autolabeler.Count == 0)
        {
            Autolabeler.Add(new ChangelogAutolabeler("breaking-change").AppendTitle(@"^[Bb]reaking\s+[Cc]hange"));
            Autolabeler.Add(new ChangelogAutolabeler("maintenance").AppendTitle(@"^(([Aa]dd)|([Ii]improve)|([Ff]ix))\s+ci\b"));
            Autolabeler.Add(new ChangelogAutolabeler("bug").AppendTitle(@"^(([Ff]ix)|([Bb]ugfix))"));
            Autolabeler.Add(new ChangelogAutolabeler("documentation").AppendTitle(@"^(([Aa]dd)|([Ii]improve))\s+[Dd]oc"));
            Autolabeler.Add(new ChangelogAutolabeler("tests").AppendTitle(@"^(([Aa]dd)|([Ii]improve))\s+[Tt]est"));
            Autolabeler.Add(new ChangelogAutolabeler("examples").AppendTitle(@"^(([Aa]dd)|([Ii]improve))\s+[Ee]xample"));
            Autolabeler.Add(new ChangelogAutolabeler("accessibility").AppendTitle(@"^(([Aa]dd)|([Ii]improve))\s+[Tt]ranslation").AppendTitle(@"^(([Aa]dd)|([Ii]improve))\s+[Aa]ccessibility"));
            Autolabeler.Add(new ChangelogAutolabeler("feature").AppendTitle(@"^([Aa]dd)\s+"));
            Autolabeler.Add(new ChangelogAutolabeler("enhancement").AppendTitle(@"^(([Ee]nhance)|([Rr]efactor))").AppendTitle(@"^([Ii]improve)\s+[Pp]erf"));
            Autolabeler.Add(new ChangelogAutolabeler("dependencies").AppendTitle(@"^([Uu]pdate)\s+[Dd]epend"));
            Autolabeler.Add(new ChangelogAutolabeler("misc").AppendTitle(@".")); // match anything left
        }
    }
}