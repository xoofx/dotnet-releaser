﻿using System;
using System.Collections.Generic;

namespace DotNetReleaser.Helpers;

internal static class LicenseHelper
{
    /// <summary>
    /// Check if the specified license is a well known license.
    /// </summary>
    /// <param name="license">The license to check.</param>
    /// <returns><c>true</c> if the specified license is a well known license. <c>false</c> otherwise.</returns>
    public static bool IsKnownLicense(string license) => Licenses.ContainsKey(license);

    /// <summary>
    /// Check if the specified license is defined.
    /// </summary>
    /// <param name="license">The license to check.</param>
    /// <returns><c>true</c> if the specified license is defined. <c>false</c> otherwise.</returns>
    public static bool IsLicenseDefined(string license) => string.IsNullOrEmpty(license) || license.Equals("No license found", StringComparison.OrdinalIgnoreCase);
    
    // List from https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/licensing-a-repository#searching-github-by-license-type
    private static readonly Dictionary<string, string> Licenses = new (StringComparer.OrdinalIgnoreCase)
    {
        {"afl-3.0", "Academic Free License v3.0"},
        {"apache-2.0", "Apache license 2.0"},
        {"artistic-2.0", "Artistic license 2.0"},
        {"bsl-1.0", "Boost Software License 1.0"},
        {"bsd-2-clause", "BSD 2-clause \"Simplified\" license"},
        {"bsd-3-clause", "BSD 3-clause \"New\" or \"Revised\" license"},
        {"bsd-3-clause-clear", "BSD 3-clause Clear license"},
        {"cc", "Creative Commons license family"},
        {"cc0-1.0", "Creative Commons Zero v1.0 Universal"},
        {"cc-by-4.0", "Creative Commons Attribution 4.0"},
        {"cc-by-sa-4.0", "Creative Commons Attribution Share Alike 4.0"},
        {"wtfpl", "Do What The F*ck You Want To Public License"},
        {"ecl-2.0", "Educational Community License v2.0"},
        {"epl-1.0", "Eclipse Public License 1.0"},
        {"epl-2.0", "Eclipse Public License 2.0"},
        {"eupl-1.1", "European Union Public License 1.1"},
        {"agpl-3.0", "GNU Affero General Public License v3.0"},
        {"gpl", "GNU General Public License family"},
        {"gpl-2.0", "GNU General Public License v2.0"},
        {"gpl-3.0", "GNU General Public License v3.0"},
        {"lgpl", "GNU Lesser General Public License family"},
        {"lgpl-2.1", "GNU Lesser General Public License v2.1"},
        {"lgpl-3.0", "GNU Lesser General Public License v3.0"},
        {"isc", "ISC"},
        {"lppl-1.3c", "LaTeX Project Public License v1.3c"},
        {"ms-pl", "Microsoft Public License"},
        {"mit", "MIT"},
        {"mpl-2.0", "Mozilla Public License 2.0"},
        {"osl-3.0", "Open Software License 3.0"},
        {"postgresql", "PostgreSQL License"},
        {"ofl-1.1", "SIL Open Font License 1.1"},
        {"ncsa", "University of Illinois/NCSA Open Source License"},
        {"unlicense", "The Unlicense"},
        {"zlib", "zLib License"},
    };
}