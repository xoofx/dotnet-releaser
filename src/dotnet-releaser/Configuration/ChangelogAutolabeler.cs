using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DotNet.Globbing;
using DotNetReleaser.Changelog;
using DotNetReleaser.Helpers;
using DotNetReleaser.Logging;

namespace DotNetReleaser.Configuration;

public class ChangelogAutolabeler 
{
    public ChangelogAutolabeler() : this(string.Empty)
    {
    }

    public ChangelogAutolabeler(string label)
    {
        Label = label;
        Title = new List<string>();
        Body = new List<string>();
        Files = new List<string>();
        Branch = new List<string>();
    }
    
    public string Label { get; set; }

    public List<string> Title { get; }

    public List<string> Body { get; }
    
    public List<string> Files { get; }

    public List<string> Branch { get; }

    public ChangelogAutolabeler AppendTitle(params string[] title)
    {
        Title.AddRange(title);
        return this;
    }

    public ChangelogAutolabeler AppendBody(params string[] body)
    {
        Body.AddRange(body);
        return this;
    }

    public ChangelogAutolabeler AppendFiles(params string[] files)
    {
        Files.AddRange(files);
        return this;
    }
    
    public ChangelogAutolabeler AppendBranch(params string[] branch)
    {
        Branch.AddRange(branch);
        return this;
    }

    public ChangelogAutolabelerCompiled Compile(ISimpleLogger logger)
    {
        return new ChangelogAutolabelerCompiled(Label ?? string.Empty) 
        {
            TitleRegex = RegexHelper.Compile(Title, "autolabeler.title", logger),
            BodyRegex = RegexHelper.Compile(Body, "autolabeler.body", logger),
            BranchRegex = RegexHelper.Compile(Branch, "autolabeler.branch", logger),
            FilesGlob = GlobHelper.Compile(Files, "autolabeler.file", logger)
        };
    }
}

public class ChangelogAutolabelerCompiled
{
    public ChangelogAutolabelerCompiled(string label)
    {
        Label = label;
        TitleRegex = new List<Regex>();
        BodyRegex = new List<Regex>();
        BranchRegex = new List<Regex>();
        FilesGlob = new List<Glob>();
    }

    public string Label { get; }
    public List<Regex> TitleRegex { get; init; }
    public List<Regex> BodyRegex { get; init; }
    public List<Regex> BranchRegex { get; init; }
    public List<Glob> FilesGlob { get; init; }

    public bool Match(ChangelogPullRequestChangeModel change)
    {
        if (TitleRegex.IsMatch(change.Title)) return true;
        if (BodyRegex.IsMatch(change.Body)) return true;
        if (BranchRegex.IsMatch(change.Branch)) return true;
        return change.Files.Any(file => FilesGlob.IsMatch(file));
    }

    public bool Match(ChangelogCommitChangeModel change)
    {
        if (TitleRegex.IsMatch(change.Title)) return true;
        return BodyRegex.IsMatch(change.Body);
    }
}