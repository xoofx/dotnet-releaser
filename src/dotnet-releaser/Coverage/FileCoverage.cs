using System.Collections.Generic;

namespace DotNetReleaser.Coverage;

public class FileCoverage : CoverageBase
{
    public FileCoverage(string fullPath)
    {
        FullPath = fullPath;
        Classes = new List<ClassCoverage>();
    }

    public string FullPath { get; init; }

    public List<ClassCoverage> Classes { get; }

    public override void UpdateCoverage()
    {
        UpdateCoverageFromList(Classes);
    }
}