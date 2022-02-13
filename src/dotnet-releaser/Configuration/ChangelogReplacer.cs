namespace DotNetReleaser.Configuration;

public class ChangelogReplacer
{
    public ChangelogReplacer()
    {
        Search = string.Empty;
        Replace = string.Empty;
    }

    public string Search { get; set; }

    public string Replace { get; set; }
}