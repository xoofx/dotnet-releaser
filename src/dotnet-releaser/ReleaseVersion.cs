namespace DotNetReleaser;

public record ReleaseVersion(string Version, bool IsDraft, string Tag, string DraftName)
{
    public override string ToString()
    {
        return IsDraft ? $"{Version} (draft: \"{DraftName}\")" : Version;
    }
}