using System.Collections.Generic;

namespace DotNetReleaser.Configuration;

public class ServiceConfiguration : ConfigurationBase
{
    public ServiceConfiguration()
    {
        Publish = false;
        Systemd = new SystemdConfiguration();
    }

    public SystemdConfiguration Systemd { get; }

    public class SystemdConfiguration : ConfigurationBase
    {
        public SystemdConfiguration()
        {
            Arguments = string.Empty;
            Sections = new Dictionary<string, IDictionary<string, object?>>()
            {
                { "Unit", new Dictionary<string, object?>() },
                { "Service", new Dictionary<string, object?>() },
                { "Install", new Dictionary<string, object?>() },
            };

            AddDefaults();
        }

        public string Arguments { get; set; }

        public string? User { get; set; }

        public bool CreateUser { get; set; }

        public Dictionary<string, IDictionary<string, object?>> Sections { get; }

        public void AddDefaults()
        {
            if (!Sections.TryGetValue("Unit", out var unitSection))
            {
                unitSection = new Dictionary<string, object?>();
                Sections["Unit"] = unitSection;
            }

            if (!Sections.TryGetValue("Service", out var serviceSection))
            {
                serviceSection = new Dictionary<string, object?>();
                Sections["Service"] = serviceSection;
            }

            if (!Sections.TryGetValue("Install", out var installSection))
            {
                installSection = new Dictionary<string, object?>();
                Sections["Install"] = installSection;
            }

            unitSection.TryAdd("StartLimitIntervalSec", 60); // Tries during 60s to restart the service
            unitSection.TryAdd("StartLimitBurst", 4); // Maximum of 4 retries in 60s
            serviceSection.TryAdd("Restart", "always"); // Always tries to restart the service
            serviceSection.TryAdd("RestartSec", 1); // 1s
            installSection.TryAdd("WantedBy", "multi-user.target");
        }
    }
}
