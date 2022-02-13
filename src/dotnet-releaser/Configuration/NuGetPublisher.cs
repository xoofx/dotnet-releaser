namespace DotNetReleaser.Configuration;

public class NuGetPublisher : ConfigurationBase
{
    public NuGetPublisher()
    {
        Source = "https://api.nuget.org/v3/index.json";
    }

    public string Source { get; set; }

    public override string ToString()
    {
        return $"{base.ToString()}, {nameof(Source)}: {Source}";
    }
}