using System;
using System.Collections.Generic;
using System.Linq;

namespace DotNetReleaser.Coverage;

public class AssemblyCoverage : CoverageBase
{
    public AssemblyCoverage(string name)
    {
        Name = name;
        Files = new List<FileCoverage>();
    }

    public string Name { get; init; }

    public List<FileCoverage> Files { get; }

    public override void UpdateCoverage()
    {
        UpdateCoverageFromList(Files);
    }

    public static List<AssemblyCoverage> Merge(IEnumerable<AssemblyCoverage> list)
    {
        var map = new MapAssemblies();
        foreach (var item in list)
        {
            if (!map.TryGetValue(item.Name, out var mapAssemblyCoverage))
            {
                mapAssemblyCoverage = new MapAssemblyCoverage(item.Name);
                map.Add(item.Name, mapAssemblyCoverage);
            }

            Merge(item.Files, mapAssemblyCoverage);
        }

        return map.ToAssemblyCoverageList();
    }

    private static void Merge(IEnumerable<FileCoverage> list, MapAssemblyCoverage mapAssembly)
    {
        foreach (var fileCoverage in list)
        {
            if (!mapAssembly.TryGetValue(fileCoverage.FullPath, out var mapFileCoverage))
            {
                mapFileCoverage = new MapFileCoverage(fileCoverage.FullPath);
                mapAssembly.Add(fileCoverage.FullPath, mapFileCoverage);
            }
            Merge(fileCoverage.Classes, mapFileCoverage);
        }
    }

    private static void Merge(IEnumerable<ClassCoverage> list, MapFileCoverage mapAssembly)
    {
        foreach (var classCoverage in list)
        {
            if (!mapAssembly.TryGetValue(classCoverage.Name, out var mapClassCoverage))
            {
                mapClassCoverage = new MapClassCoverage(classCoverage.Name);
                mapAssembly.Add(classCoverage.Name, mapClassCoverage);
            }
            Merge(classCoverage.Methods, mapClassCoverage);
        }
    }

    private static void Merge(IEnumerable<MethodCoverage> list, MapClassCoverage mapClass)
    {
        foreach (var methodCoverage in list)
        {
            var methodSignature = methodCoverage.Method;
            if (!mapClass.TryGetValue(methodSignature, out var mapMethodCoverage))
            {
                mapMethodCoverage = new MapMethodCoverage(methodSignature);
                mapClass.Add(methodSignature, mapMethodCoverage);
            }
            Merge(methodCoverage.Lines, mapMethodCoverage);
        }
    }

    private static void Merge(IEnumerable<LineCoverage> list, MapMethodCoverage mapMethod)
    {
        foreach (var lineCoverage in list)
        {
            if (!mapMethod.TryGetValue(lineCoverage.Number, out var mapLineCoverage))
            {
                mapLineCoverage = new LineCoverage()
                {
                    Number = lineCoverage.Number,
                    Hits = lineCoverage.Hits,
                    IsBranch = lineCoverage.IsBranch,
                    ConditionCoverage = lineCoverage.ConditionCoverage
                };
                mapMethod.Add(lineCoverage.Number, mapLineCoverage);
            }
            else
            {
                mapLineCoverage.Hits = Math.Max(mapLineCoverage.Hits, lineCoverage.Hits);
                if (lineCoverage.ConditionCoverage.Rate > mapLineCoverage.ConditionCoverage.Rate)
                {
                    mapLineCoverage.ConditionCoverage = lineCoverage.ConditionCoverage;
                }
            }
        }
    }

    private class MapAssemblies : Dictionary<string, MapAssemblyCoverage>
    {
        public List<AssemblyCoverage> ToAssemblyCoverageList()
        {
            var list = new List<AssemblyCoverage>(Count);
            foreach (var mapAssemblyCoverage in this.Values.OrderBy(x => x.Name))
            {
                var assemblyCoverage = mapAssemblyCoverage.ToAssemblyCoverage();
                assemblyCoverage.UpdateCoverage();
                list.Add(assemblyCoverage);
            }

            return list;
        }
    }

    private class MapAssemblyCoverage : Dictionary<string, MapFileCoverage>
    {
        public MapAssemblyCoverage(string name)
        {
            Name = name;
        }

        public string Name { get; init; }

        public AssemblyCoverage ToAssemblyCoverage()
        {
            var assemblyCoverage = new AssemblyCoverage(Name);
            foreach (var mapClassCoverage in this.Values.OrderBy(x => x.FullPath))
            {
                assemblyCoverage.Files.Add(mapClassCoverage.ToFileCoverage());
            }

            return assemblyCoverage;
        }
    }

    private class MapFileCoverage : Dictionary<string, MapClassCoverage>
    {
        public MapFileCoverage(string fullPath)
        {
            FullPath = fullPath;
        }

        public string FullPath { get; init; }

        public FileCoverage ToFileCoverage()
        {
            var fileCoverage = new FileCoverage(FullPath);
            foreach (var mapClassCoverage in this.Values.OrderBy(x => x.Name))
            {
                fileCoverage.Classes.Add(mapClassCoverage.ToClassCoverage());
            }

            return fileCoverage;
        }
    }

    private class MapClassCoverage : Dictionary<MethodSignature, MapMethodCoverage>
    {
        public MapClassCoverage(string name)
        {
            Name = name;
        }

        public string Name { get; init; }

        public ClassCoverage ToClassCoverage()
        {
            var classCoverage = new ClassCoverage(Name);
            var methods = new List<MethodCoverage>();
            foreach (var value in Values)
            {
                methods.Add(value.ToMethodCoverage());
            }
            classCoverage.Methods.AddRange(methods.OrderBy(x => x.Method.Name).ThenBy(y => y.Method.Signature));

            return classCoverage;
        }
    }

    private class MapMethodCoverage : Dictionary<int, LineCoverage>
    {
        public MapMethodCoverage(MethodSignature method)
        {
            Method = method;
        }

        public MethodSignature Method { get; }

        public MethodCoverage ToMethodCoverage()
        {
            var methodCoverage = new MethodCoverage(Method);
            methodCoverage.Lines.AddRange(this.OrderBy(x => x.Key).Select(x => x.Value));
            return methodCoverage;
        }
    }
}