using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DotNetReleaser.Logging;

namespace DotNetReleaser.Helpers;

public static class RegexHelper
{
    private static readonly Regex DefaultRegex = new Regex(".");

    public static Regex? Compile(string? pattern, string property, ISimpleLogger log)
    {
        if (pattern is null) return null;
        try
        {
            var regex = new Regex(pattern);
            return regex;
        }
        catch (Exception ex)
        {
            log.Error($"Invalid regex `{pattern}` for property `{property}. {ex.Message}");
        }
        return null;
    }

    public static List<Regex> Compile(IEnumerable<string> patterns, string property, ISimpleLogger log)
    {
        var list = new List<Regex>();
        foreach (var pattern in patterns)
        {
            var regex = Compile(pattern, property, log);
            if (regex != null) list.Add(regex);
        }

        return list;
    }

    public static bool IsMatch(this IEnumerable<Regex> regexList, string input)
    {
        return regexList.Any(regex => regex.IsMatch(input));
    }
}