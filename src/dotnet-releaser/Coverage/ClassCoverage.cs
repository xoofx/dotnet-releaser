using System.Collections.Generic;

namespace DotNetReleaser.Coverage;

public class ClassCoverage : CoverageBase
{
    public ClassCoverage(string name)
    {
        Name = name;
        LineRate = default;
        BranchRate = default;
        Methods = new List<MethodCoverage>();
    }

    public string Name { get; init; }

    public List<MethodCoverage> Methods { get; }

    public override void UpdateCoverage()
    {
        UpdateCoverageFromList(Methods);
    }
}