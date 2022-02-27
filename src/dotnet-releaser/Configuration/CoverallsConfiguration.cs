namespace DotNetReleaser.Configuration;

public class CoverallsConfiguration : ConfigurationBase
{
    public CoverallsConfiguration()
    {
        Url = "https://coveralls.io";
    }
    
    public string Url { get; set; }
}