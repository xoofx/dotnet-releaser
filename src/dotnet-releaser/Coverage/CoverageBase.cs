using System.Collections.Generic;

namespace DotNetReleaser.Coverage;

public abstract class CoverageBase
{
    protected CoverageBase()
    {
    }

    public HitCoverage LineRate { get; set; }

    public HitCoverage BranchRate { get; set; }

    public abstract void UpdateCoverage();

    protected void UpdateCoverageFromList<T>(IEnumerable<T> items) where T : CoverageBase
    {
        HitCoverage lineRate = default;
        HitCoverage branchRate = default;
        foreach (var item in items)
        {
            item.UpdateCoverage();
            lineRate += item.LineRate;
            branchRate += item.BranchRate;
        }

        LineRate = lineRate;
        BranchRate = branchRate;
    }
}