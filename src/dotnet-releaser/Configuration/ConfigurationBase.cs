namespace DotNetReleaser.Configuration;

public abstract class ConfigurationBase
{
    protected ConfigurationBase()
    {
        Publish = true;
    }

    public bool Publish { get; set; }

    public override string ToString()
    {
        return $"{nameof(Publish)}: {Publish}";
    }
}