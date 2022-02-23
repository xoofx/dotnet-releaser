namespace DotNetReleaser.Coverage;

public class LineCoverage
{
    public int Number { get; set; }

    public int Hits { get; set; }

    public bool IsBranch { get; set; }

    public HitCoverage ConditionCoverage { get; set; }
}