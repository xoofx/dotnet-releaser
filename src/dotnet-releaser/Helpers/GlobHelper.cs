using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DotNet.Globbing;
using DotNetReleaser.Logging;

namespace DotNetReleaser.Helpers;

public static class GlobHelper
{
    public static Glob? Compile(string? globPattern, string property, ISimpleLogger log)
    {
        if (globPattern is null) return null;
        try
        {
            var glob = Glob.Parse(globPattern);
            return glob;
        }
        catch (Exception ex)
        {
            log.Error($"Invalid blog `{globPattern}` for property `{property}. {ex.Message}");
        }
        return null;
    }

    public static List<Glob> Compile(IEnumerable<string> patterns, string property, ISimpleLogger log)
    {
        var list = new List<Glob>();
        foreach (var pattern in patterns)
        {
            var glob = Compile(pattern, property, log);
            if (glob != null) list.Add(glob);
        }

        return list;
    }

    public static bool IsMatch(this IEnumerable<Glob> regexList, string input)
    {
        return regexList.Any(regex => regex.IsMatch(input));
    }
}