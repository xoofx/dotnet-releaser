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

            // Defaults for restarting
            Sections["Unit"]["StartLimitIntervalSec"] = 60; // Tries during 60s to restart the service
            Sections["Unit"]["StartLimitBurst"] = 4; // Maximum of 4 retries in 60s
            Sections["Service"]["Restart"] = "always"; // Always tries to restart the service
            Sections["Service"]["RestartSec"] = 1; // 1s
            Sections["Install"]["WantedBy"] = "multi-user.target";
        }

        public string Arguments { get; set; }

        public string? User { get; set; }

        public bool CreateUser { get; set; }

        public Dictionary<string, IDictionary<string, object?>> Sections { get; }
    }
}