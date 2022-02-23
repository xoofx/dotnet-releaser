using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;

namespace DotNetReleaser.Coverage;

public static class CoberturaParser
{
    public static List<AssemblyCoverage> Parse(TextReader reader)
    {
        var list = new List<AssemblyCoverage>();
        var element = XElement.Load(reader);

        var folders = element.XPathSelectElements("./sources/source").Select(x => x.Value).ToList();

        var packages = element.XPathSelectElements("./packages/package");
        foreach (var package in packages)
        {
            var assemblyCoverage = ParseAssemblyCoverage(package, folders);
            list.Add(assemblyCoverage);
        }

        return AssemblyCoverage.Merge(list);
    }

    private static AssemblyCoverage ParseAssemblyCoverage(XElement elt, List<string> folders)
    {
        var assemblyCoverage = new AssemblyCoverage(elt.Attribute("name")!.Value);
        var classes = elt.XPathSelectElements("./classes/class");
        foreach (var subElt in classes)
        {
            var fileCoverage = ParseFileCoverage(subElt, folders);
            assemblyCoverage.Files.Add(fileCoverage);
        }
        assemblyCoverage.UpdateCoverage();

        return assemblyCoverage;
    }

    private static FileCoverage ParseFileCoverage(XElement elt, List<string> folders)
    {
        var classCoverage = new ClassCoverage(elt.Attribute("name")!.Value);
        var filename = elt.Attribute("filename")!.Value;
        string fullPath = filename;
        foreach (var folder in folders)
        {
            fullPath = Path.GetFullPath(Path.Combine(folder, filename));
            if (File.Exists(fullPath))
            {
                break;
            }
        }
        var methods = elt.XPathSelectElements("./methods/method");
        foreach (var subElt in methods)
        {
            var methodCoverage = ParseMethodCoverage(subElt);
            classCoverage.Methods.Add(methodCoverage);
        }

        var fileCoverage = new FileCoverage(fullPath);
        fileCoverage.Classes.Add(classCoverage);
        return fileCoverage;
    }

    private static MethodCoverage ParseMethodCoverage(XElement elt)
    {
        var methodSignature = new MethodSignature(elt.Attribute("name")!.Value, elt.Attribute("signature")!.Value);
        var methodCoverage = new MethodCoverage(methodSignature);
        var lines = elt.XPathSelectElements("./lines/line");
        foreach (var subElt in lines)
        {
            var lineCoverage = ParseLineCoverage(subElt);
            methodCoverage.Lines.Add(lineCoverage);
        }

        return methodCoverage;
    }

    private static readonly Regex ConditionCoverageRegex = new Regex(@"\((\d+)/(\d+)\)");

    private static LineCoverage ParseLineCoverage(XElement elt)
    {
        var lineCoverage = new LineCoverage
        {
            Number = int.Parse(elt.Attribute("number")?.Value ?? "0", CultureInfo.InvariantCulture),
            Hits = int.Parse(elt.Attribute("hits")?.Value ?? "0", CultureInfo.InvariantCulture),
            IsBranch = elt.Attribute("branch")?.Value?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false
        };
        var conditionCoverage = elt.Attribute("condition-coverage")?.Value ?? string.Empty;
        var match = ConditionCoverageRegex.Match(conditionCoverage);
        if (match.Success)
        {
            lineCoverage.ConditionCoverage = new HitCoverage(int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value));
        }
        return lineCoverage;
    }

    private static decimal ParseRate(XAttribute? attr)
    {
        if (attr?.Value?.Equals("nan", StringComparison.OrdinalIgnoreCase) ?? false)
        {
            return 0;
        }
        return decimal.Parse(attr?.Value?.Replace(',', '.') ?? "0");
    }
}