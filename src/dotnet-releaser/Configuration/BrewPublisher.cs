namespace DotNetReleaser.Configuration;

public class BrewPublisher : ConfigurationBase
{
    public BrewPublisher()
    {
        Home = string.Empty;
    }

    public string Home { get; set; }
}