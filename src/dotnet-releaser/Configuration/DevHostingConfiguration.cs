namespace DotNetReleaser.Configuration;

/// <summary>
/// Shared configuration for dev hosting.
/// </summary>
public abstract class DevHostingConfiguration : ConfigurationBase
{
    protected DevHostingConfiguration()
    {
        VersionPrefix = string.Empty;
        Base = string.Empty;
        Api = string.Empty;
        User = string.Empty;
        Repo = string.Empty;
    }

    internal abstract string Provider { get; }

    public string Base { get; set; }

    public string Api { get; set; }

    public string User { get; set; }

    public string Repo { get; set; }

    public string VersionPrefix { get; set; }

    public string GetUrl() => $"{Base.Trim('/')}/{User}/{Repo}";

    public override string ToString()
    {
        return $"{base.ToString()}, {nameof(User)}: {User}, {nameof(Repo)}: {Repo}, {nameof(VersionPrefix)}: {VersionPrefix}, Url: {GetUrl()}";
    }
}