using System.Collections.Generic;

namespace DotNetReleaser.Coverage;

public class MethodCoverage : CoverageBase
{
    public MethodCoverage(MethodSignature method)
    {
        Method = method;
        Lines = new List<LineCoverage>();
    }

    public MethodSignature Method { get; init; }

    public List<LineCoverage> Lines { get; }

    public override void UpdateCoverage()
    {
        int linesCovered = 0;
        HitCoverage branchRate = default;
        foreach (var lineCoverage in Lines)
        {
            if (lineCoverage.Hits != 0)
            {
                linesCovered++;
            }
            if (lineCoverage.IsBranch)
            {
                branchRate += lineCoverage.ConditionCoverage;
            }
        }

        LineRate = new HitCoverage(linesCovered, Lines.Count);
        BranchRate = branchRate;
        MethodRate = new HitCoverage(linesCovered > 0 ? 1 : 0, 1);
    }
}

public record MethodSignature(string Name, string Signature);