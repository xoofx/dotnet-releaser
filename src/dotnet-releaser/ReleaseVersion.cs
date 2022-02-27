namespace DotNetReleaser;

public record ReleaseVersion(string Version, bool IsDraft, string Tag, string DraftName)
{
    public override string ToString()
    {
        return IsDraft ? $"version: {Version}, draft-name: {DraftName}" : $"version: {Version}";
    }
}