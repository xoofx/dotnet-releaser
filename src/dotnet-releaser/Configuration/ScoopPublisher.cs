namespace DotNetReleaser.Configuration;

public class ScoopPublisher : ConfigurationBase
{
    public ScoopPublisher()
    {
        Home = string.Empty;
    }

    public string Home { get; set; }
}