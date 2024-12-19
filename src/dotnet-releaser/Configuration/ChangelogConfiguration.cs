using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DotNetReleaser.Configuration;

public class ChangelogConfiguration : ConfigurationBase
{
    public ChangelogConfiguration()
    {
        Version = @"^#+\s+v?((\d+\.)*(\d+))";
        Categories = new List<ChangelogCategory>();
        NameTemplate = "{{ version.tag }}";
        OwnersCommitChangeTemplate = "- {{ commit.title }} ({{ commit.sha}})";
        OwnersPullRequestChangeTemplate = "- {{ pr.title }} (PR #{{ pr.number }})";
        CommitChangeTemplate = "- {{ commit.title }} ({{ commit.sha}}) by @{{ commit.author }}";
        PullRequestChangeTemplate = "- {{ pr.title }} (PR #{{ pr.number }}) by @{{ pr.author }}";
        IncludeCommits = true;
        Defaults = true;
        //ChangeTitleEscape = @"\<*_&@";
        Owners = new List<string>();
        Autolabelers = new List<ChangelogAutolabeler>();
        Replacers = new List<RegexReplacer>();
        Include = new ChangelogFilter();
        Exclude = new ChangelogFilter();
        BodyTemplate = @"# Changes

{{ changes }}

**Full Changelog**: {{ url_full_changelog_compare_changes }}

<sub>Published with [dotnet-releaser](https://github.com/xoofx/dotnet-releaser/)</sub>
";
        TemplateProperties = new Dictionary<string, object>();
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

    public ChangelogFilter Include { get; }

    public ChangelogFilter Exclude { get; }

    [DataMember(Name = "autolabeler")]
    public List<ChangelogAutolabeler> Autolabelers { get; }

    [DataMember(Name = "replacer")]
    public List<RegexReplacer> Replacers { get; } // TBD: not implemented yet
    
    [DataMember(Name = "category")]
    public List<ChangelogCategory> Categories { get; }

    public Dictionary<string, object> TemplateProperties { get; }

    public void AddDefaults()
    {
        // Add defaults unless user is explicitly disabling them.
        if (!Defaults) return;

        Exclude.Labels.Add("skip-release-notes");

        Categories.Add(new ChangelogCategory(10, "## 🚨 Breaking Changes", "breaking-change", "category: breaking-change"));
        Categories.Add(new ChangelogCategory(20, "## ✨ New Features", "new-feature", "feature", "category: feature"));
        Categories.Add(new ChangelogCategory(30, "## 🐛 Bug Fixes", "bugfix", "fix", "bug", "category: bug"));
        Categories.Add(new ChangelogCategory(40, "## 🚀 Enhancements", "enhancement", "refactor", "performance", "category: performance", "category: enhancement"));
        Categories.Add(new ChangelogCategory(50, "## 🧰 Maintenance", "maintenance", "ci", "category: ci"));
        Categories.Add(new ChangelogCategory(60, "## 🏭 Tests", "tests", "test", "category: tests"));
        Categories.Add(new ChangelogCategory(70, "## 🛠 Examples", "examples", "samples", "category: samples", "category: examples"));
        Categories.Add(new ChangelogCategory(80, "## 📚 Documentation", "documentation", "doc", "category: documentation", "category: doc"));
        Categories.Add(new ChangelogCategory(90, "## 🌎 Accessibility", "translations", "accessibility", "category: accessibility"));
        Categories.Add(new ChangelogCategory(100, "## 📦 Dependencies", "dependencies", "deps", "category: dependencies"));
        Categories.Add(new ChangelogCategory(110, "## 🧰 Misc", "misc", "category: misc"));

        var addImproveFix = @"^(([Aa]dd)|([Ii]mprove)|([Ff]ix)|([Uu]pdate))\s+";
        Autolabelers.Add(new ChangelogAutolabeler("breaking-change").AppendTitle(@"^[Bb]reaking\s+[Cc]hange"));
        Autolabelers.Add(new ChangelogAutolabeler("maintenance").AppendTitle(@$"{addImproveFix}ci\b"));
        Autolabelers.Add(new ChangelogAutolabeler("documentation").AppendTitle(@$"{addImproveFix}[Dd]oc"));
        Autolabelers.Add(new ChangelogAutolabeler("tests").AppendTitle(@$"{addImproveFix}[Tt]est"));
        Autolabelers.Add(new ChangelogAutolabeler("examples").AppendTitle(@$"{addImproveFix}[Ee]xample"));
        Autolabelers.Add(new ChangelogAutolabeler("accessibility").AppendTitle(@$"{addImproveFix}[Tt]ranslation").AppendTitle(@$"{addImproveFix}[Aa]ccessibility"));
        Autolabelers.Add(new ChangelogAutolabeler("bugfix").AppendTitle(@"^(([Ff]ix)|([Bb]ugfix))"));
        Autolabelers.Add(new ChangelogAutolabeler("feature").AppendTitle(@"^([Aa]dd)\s+"));
        Autolabelers.Add(new ChangelogAutolabeler("enhancement").AppendTitle(@"^(([Ee]nhance)|([Rr]efactor))").AppendTitle(@"^([Ii]mprove)\s+[Pp]erf"));
        Autolabelers.Add(new ChangelogAutolabeler("dependencies").AppendTitle(@"^([Uu]pdate)\s+[Dd]epend").AppendTitle(@"^([Bb]ump)\s+\w{3,}")); // We match more then Bump xxx to make sure that we won't match a "Bump to 0.9.1"
        Autolabelers.Add(new ChangelogAutolabeler("misc").AppendTitle(@".")); // match anything left
    }
}