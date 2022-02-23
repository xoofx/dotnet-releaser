using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DotNetReleaser;

public partial class ReleaserApp 
{
    private async Task<string?> CreateSystemdServiceFile(ProjectPackageInfo projectPackageInfo)
    {
        var writer = new StringWriter() { NewLine = "\n" };
        var systemd = _config.Service.Systemd;

        // Write the section "Unit" first
        var sections = new Dictionary<string, IDictionary<string, object?>>(systemd.Sections);
        var unitSection = new Dictionary<string, object?>(sections["Unit"]);
        sections.Remove("Unit");
        if (!unitSection.ContainsKey("Description"))
        {
            unitSection["Description"] = projectPackageInfo.Description;
        }
        AddSection(writer, "Unit", unitSection);

        // Write all following sections ordered by section name
        foreach (var section in sections.OrderBy(x => x.Key))
        {
            var sectionName = section.Key;
            var sectionValues = new Dictionary<string, object?>(section.Value);

            // Setup default values for Service
            if (sectionName == "Service")
            {
                var arguments = string.IsNullOrWhiteSpace(systemd.Arguments) ? string.Empty : $" {systemd.Arguments}";
                sectionValues["ExecStart"] = $"/usr/local/bin/{projectPackageInfo.AssemblyName}{arguments}";
                if (!sectionValues.ContainsKey("Type"))
                {
                    sectionValues["Type"] = "simple";
                }
                if (!string.IsNullOrEmpty(systemd.User))
                {
                    sectionValues["User"] = systemd.User;
                }
            }

            // Write section
            AddSection(writer, sectionName, sectionValues);
        }

        // If we didn't have any errors, write the final service file to the disk
        if (!HasErrors)
        {
            var systemdServiceFile = Path.GetTempFileName() + $".{projectPackageInfo.AssemblyName}.service";
            await File.WriteAllTextAsync(systemdServiceFile, writer.ToString());
            return systemdServiceFile;
        }

        return null;
    }

    private void AddSection(StringWriter writer, string section, IDictionary<string, object?> properties)
    {
        writer.WriteLine($"[{section}]");
        foreach (var property in properties.OrderBy(x => x.Key))
        {
            var key = property.Key;
            var value = property.Value;

            // Pre-format the boolean to a plain true/false
            if (value is bool b)
            {
                value = b ? "true" : "false";
            }

            // Make sure to format all numbers to invariant culture
            if (value is IFormattable formattable)
            {
                value = formattable.ToString(null, CultureInfo.InvariantCulture);
            }

            if (value is not null)
            {
                if (value.GetType().IsPrimitive || value is string)
                {
                    writer.WriteLine($"{key} = {value}");
                }
                else
                {
                    Error($"Unsupported type `{value.GetType().FullName}` for the value associated to the key `{key}` in section `[{section}]`. Must be integers or strings only.");
                }
            }
        }
    }
}