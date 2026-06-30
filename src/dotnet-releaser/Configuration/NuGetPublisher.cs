namespace DotNetReleaser.Configuration;

public class NuGetPublisher : ConfigurationBase
{
    public NuGetPublisher()
    {
        Source = "https://api.nuget.org/v3/index.json";
    }

    public bool PublishDraft { get; set; }

    public string Source { get; set; }

    public bool TrustedPublishing { get; set; }

    public string? User { get; set; }

    public string TrustedPublishingTokenServiceUrl { get; set; } = "https://www.nuget.org/api/v2/token";

    public string TrustedPublishingAudience { get; set; } = "https://www.nuget.org";

    public override string ToString()
    {
        return $"{base.ToString()}, {nameof(Source)}: {Source}, {nameof(TrustedPublishing)}: {TrustedPublishing}";
    }
}
