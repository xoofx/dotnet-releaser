using System.Collections.Generic;

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
        Branches = new List<string>();
    }

    internal abstract string Provider { get; }

    public string Base { get; set; }

    public string Api { get; set; }

    public string User { get; set; }

    public string Repo { get; set; }

    public string VersionPrefix { get; set; }

    public List<string> Branches { get; }

    public string GetUrl() => $"{Base.Trim('/')}/{User}/{Repo}";

    public void AddDefaults()
    {
        // Don't add default branches if they are added manually
        if (Branches.Count > 0) return;

        Branches.Add("main");
        Branches.Add("master");
    }

    public override string ToString()
    {
        return $"{base.ToString()}, {nameof(User)}: {User}, {nameof(Repo)}: {Repo}, {nameof(VersionPrefix)}: {VersionPrefix}, Url: {GetUrl()}";
    }
}