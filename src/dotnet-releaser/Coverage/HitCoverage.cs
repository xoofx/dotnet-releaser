namespace DotNetReleaser.Coverage;

public record struct HitCoverage(int Hit, int Total)
{
    public decimal Rate => Total == 0 ? 0 : (decimal)Hit / Total;

    public HitCoverage Add(HitCoverage value)
    {
        return new HitCoverage(Hit + value.Hit, Total + value.Total);
    }
    public static HitCoverage operator +(HitCoverage left, HitCoverage right)
    {
        return new HitCoverage(left.Hit + right.Hit, left.Total + right.Total);
    }
}