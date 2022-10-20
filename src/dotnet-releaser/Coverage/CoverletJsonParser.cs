using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace DotNetReleaser.Coverage;

public class CoverletJsonParser
{
    public static List<AssemblyCoverage> Parse(Stream reader)
    {
        var list = new List<AssemblyCoverage>();
        var modules = JsonSerializer.Deserialize<Modules>(reader);
        if (modules is null)
        {
            return list;
        }

        foreach (var (module, documents) in modules)
        {
            var assemblyCoverage = ParseAssemblyCoverage(module, documents);
            list.Add(assemblyCoverage);

            // Discard files added by test framework
            bool hasRemoved = false;
            for (var i = 0; i < assemblyCoverage.Files.Count; i++)
            {
                var fileCoverage = assemblyCoverage.Files[i];
                if (fileCoverage.FullPath.Contains("packages/microsoft.net.test.sdk/", StringComparison.OrdinalIgnoreCase))
                {
                    assemblyCoverage.Files.RemoveAt(i);
                    i--;
                    hasRemoved = true;
                }
            }

            if (hasRemoved)
            {
                assemblyCoverage.UpdateCoverage();
            }
        }

        return list;
    }

    private static AssemblyCoverage ParseAssemblyCoverage(string name, Documents documents)
    {
        var assemblyCoverage = new AssemblyCoverage(Path.GetFileNameWithoutExtension(name));
        foreach (var (fileName, classes) in documents)
        {
            assemblyCoverage.Files.Add(ParseFileCoverage(fileName, classes));
        }
        assemblyCoverage.UpdateCoverage();
        return assemblyCoverage;
    }

    private static FileCoverage ParseFileCoverage(string fileName, Classes classes)
    {
        var fileCoverage = new FileCoverage(fileName);

        foreach (var (className, methods) in classes)
        {
            fileCoverage.Classes.Add(ParseClassCoverage(className, methods));
        }

        return fileCoverage;
    }

    private static ClassCoverage ParseClassCoverage(string className, Methods methods)
    {
        var classCoverage = new ClassCoverage(className);

        foreach (var (methodName, method) in methods)
        {
            classCoverage.Methods.Add(ParseMethodCoverage(methodName, method));
        }

        return classCoverage;
    }


    //                                                              1      2     3      4
    private static readonly Regex MethodRegex = new Regex(@"(.+)\s+(.+)::(.+)(\(.*\))");

    private static MethodCoverage ParseMethodCoverage(string methodRawSignature, Method method)
    {
        var match = MethodRegex.Match(methodRawSignature);
        if (!match.Success)
        {
            throw new InvalidOperationException($"Unable to match coverlet method name: {methodRawSignature}");
        }

        var returnType = match.Groups[1].Value;
        //var className = match.Groups[2].Value;
        var methodName  = match.Groups[3].Value;
        var methodArguments = match.Groups[4].Value;

        var methodSignature = new MethodSignature(methodName, methodArguments, returnType);

        var methodCoverage = new MethodCoverage(methodSignature);

        foreach (var (line, hits) in method.Lines)
        {
            methodCoverage.Lines.Add(new LineCoverage() { LineNumber = line, Hits = hits});
        }

        foreach (var branch in method.Branches)
        {
            methodCoverage.Branches.Add(new BranchCoverage()
            {
                LineNumber = branch.Line, 
                BlockNumber = branch.Offset,
                BranchNumber = branch.Path,
                Hits = branch.Hits
            });
        }

        return methodCoverage;
    }

    internal class BranchInfo
    {
        public int Line { get; set; }
        public int Offset { get; set; }
        public int EndOffset { get; set; }
        public int Path { get; set; }
        public uint Ordinal { get; set; }
        public int Hits { get; set; }
    }

    internal class Lines : SortedDictionary<int, int> { }

    internal class Branches : List<BranchInfo>
    {
    }

    internal class Method
    {
        public Method()
        {
            Lines = new Lines();
            Branches = new Branches();
        }

        [JsonPropertyName("Lines")]
        public Lines Lines { get; set; }

        [JsonPropertyName("Branches")]
        public Branches Branches { get; set; }
    }
    internal class Methods : Dictionary<string, Method> { }
    internal class Classes : Dictionary<string, Methods> { }
    internal class Documents : Dictionary<string, Classes> { }
    internal class Modules : Dictionary<string, Documents> { }

}