using System.Collections.Generic;

namespace DotNetReleaser.Configuration;

public class ChangelogConfiguration : ConfigurationBase
{
    public ChangelogConfiguration()
    {
        Version = @"^#+\s+v?((\d+\.)*(\d+))";
        Categories = new List<ChangelogCategory>();
        NameTemplate = "{{ version.tag }}";
        OwnersCommitChangeTemplate = "- {{ commit.title }} ({{ commit.sha}})";
        OwnersPullRequestChangeTemplate = "- {{ pr.title }} (#{{ pr.number }})";
        CommitChangeTemplate = "- {{ commit.title }} ({{ commit.sha}}) by @{{ commit.author }}";
        PullRequestChangeTemplate = "- {{ pr.title }} (#{{ pr.number }}) by @{{ pr.author }}";
        IncludeCommits = true;
        Defaults = true;
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

    public string NameTemplate { get; set; }

    public string BodyTemplate { get; set; }

    public bool IncludeCommits { get; set; }

    public bool Defaults { get; set; }

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

    public void AddDefaults()
    {
        // Add defaults unless user is explicit asking not for them
        if (!Defaults) return;

        Exclude.Labels.Add("skip-release-notes");

        Categories.Add(new ChangelogCategory("## 🚨 Breaking Changes", "breaking-change", "category: breaking-change"));
        Categories.Add(new ChangelogCategory("## ✨ New Features", "new-feature", "feature", "category: feature"));
        Categories.Add(new ChangelogCategory("## 🐛 Bug Fixes", "bugfix", "fix", "bug", "category: bug"));
        Categories.Add(new ChangelogCategory("## 🚀 Enhancements", "enhancement", "refactor", "performance", "category: performance"));
        Categories.Add(new ChangelogCategory("## 🧰 Maintenance", "maintenance", "ci", "category: ci"));
        Categories.Add(new ChangelogCategory("## 🏭 Tests", "tests", "test", "category: tests"));
        Categories.Add(new ChangelogCategory("## 🛠 Examples", "examples", "samples", "category: samples", "category: examples"));
        Categories.Add(new ChangelogCategory("## 📚 Documentation", "documentation", "doc", "category: documentation", "category: doc"));
        Categories.Add(new ChangelogCategory("## 🌎 Accessibility", "translations", "accessibility"));
        Categories.Add(new ChangelogCategory("## 📦 Dependencies", "dependencies", "deps"));
        Categories.Add(new ChangelogCategory("## 🧰 Misc", "misc"));

        var addImproveFix = @"^(([Aa]dd)|([Ii]mprove)|([Ff]ix))\s+";
        Autolabeler.Add(new ChangelogAutolabeler("breaking-change").AppendTitle(@"^[Bb]reaking\s+[Cc]hange"));
        Autolabeler.Add(new ChangelogAutolabeler("maintenance").AppendTitle(@$"{addImproveFix}ci\b"));
        Autolabeler.Add(new ChangelogAutolabeler("documentation").AppendTitle(@$"{addImproveFix}[Dd]oc"));
        Autolabeler.Add(new ChangelogAutolabeler("tests").AppendTitle(@$"{addImproveFix}[Tt]est"));
        Autolabeler.Add(new ChangelogAutolabeler("examples").AppendTitle(@$"{addImproveFix}[Ee]xample"));
        Autolabeler.Add(new ChangelogAutolabeler("accessibility").AppendTitle(@$"{addImproveFix}[Tt]ranslation").AppendTitle(@$"{addImproveFix}[Aa]ccessibility"));
        Autolabeler.Add(new ChangelogAutolabeler("bugfix").AppendTitle(@"^(([Ff]ix)|([Bb]ugfix))"));
        Autolabeler.Add(new ChangelogAutolabeler("feature").AppendTitle(@"^([Aa]dd)\s+"));
        Autolabeler.Add(new ChangelogAutolabeler("enhancement").AppendTitle(@"^(([Ee]nhance)|([Rr]efactor))").AppendTitle(@"^([Ii]mprove)\s+[Pp]erf"));
        Autolabeler.Add(new ChangelogAutolabeler("dependencies").AppendTitle(@"^([Uu]pdate)\s+[Dd]epend").AppendTitle(@"^([Bb]ump)\s+\w+"));
        Autolabeler.Add(new ChangelogAutolabeler("misc").AppendTitle(@".")); // match anything left
    }
}