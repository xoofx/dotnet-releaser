namespace DotNetReleaser.Coverage;

public class LineCoverage
{
    public int LineNumber { get; set; }

    public int Hits { get; set; }
}

public class BranchCoverage
{
    public int LineNumber { get; set; }

    public int BlockNumber { get; set; }

    public int BranchNumber { get; set; }

    public int Hits { get; set; }
}