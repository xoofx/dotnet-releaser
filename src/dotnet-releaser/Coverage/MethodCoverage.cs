using System.Collections.Generic;

namespace DotNetReleaser.Coverage;

public class MethodCoverage : CoverageBase
{
    public MethodCoverage(MethodSignature method)
    {
        Method = method;
        Lines = new List<LineCoverage>();
        Branches = new List<BranchCoverage>();
    }

    public MethodSignature Method { get; init; }

    public List<LineCoverage> Lines { get; }

    public List<BranchCoverage> Branches { get; }

    public override void UpdateCoverage()
    {
        int linesCovered = 0;
        int branchCovered = 0;
        foreach (var lineCoverage in Lines)
        {
            if (lineCoverage.Hits != 0)
            {
                linesCovered++;
            }
        }

        foreach (var branchCoverage in Branches)
        {
            if (branchCoverage.Hits != 0)
            {
                branchCovered++;
            }
        }


        LineRate = new HitCoverage(linesCovered, Lines.Count);
        BranchRate = new HitCoverage(branchCovered, Branches.Count);
        MethodRate = new HitCoverage(linesCovered > 0 ? 1 : 0, 1);
    }
}

public record MethodSignature(string Name, string Arguments, string ReturnType);