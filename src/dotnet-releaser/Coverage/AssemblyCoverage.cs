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
            Merge(methodCoverage.Branches, mapMethodCoverage);
        }
    }

    private static void Merge(IEnumerable<LineCoverage> list, MapMethodCoverage mapMethod)
    {
        foreach (var lineCoverage in list)
        {
            if (!mapMethod.Lines.TryGetValue(lineCoverage.LineNumber, out var mapLineCoverage))
            {
                mapLineCoverage = new LineCoverage()
                {
                    LineNumber = lineCoverage.LineNumber,
                    Hits = lineCoverage.Hits,
                };
                mapMethod.Lines.Add(lineCoverage.LineNumber, mapLineCoverage);
            }
            else
            {
                mapLineCoverage.Hits = Math.Max(mapLineCoverage.Hits, lineCoverage.Hits);
            }
        }
    }

    private static void Merge(IEnumerable<BranchCoverage> list, MapMethodCoverage mapMethod)
    {
        foreach (var branchCoverage in list)
        {
            var key = new BranchKey(branchCoverage.LineNumber, branchCoverage.BlockNumber, branchCoverage.BranchNumber);
            
            if (!mapMethod.Branches.TryGetValue(key, out var mapBranchCoverage))
            {
                mapBranchCoverage = new BranchCoverage()
                {
                    LineNumber = branchCoverage.LineNumber,
                    BlockNumber = branchCoverage.BlockNumber,
                    BranchNumber = branchCoverage.BranchNumber,
                    Hits = branchCoverage.Hits,
                };
                mapMethod.Branches.Add(key, mapBranchCoverage);
            }
            else
            {
                mapBranchCoverage.Hits = Math.Max(mapBranchCoverage.Hits, branchCoverage.Hits);
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
            classCoverage.Methods.AddRange(methods.OrderBy(x => x.Method.Name).ThenBy(y => y.Method.Arguments));

            return classCoverage;
        }
    }

    private class MapMethodCoverage
    {
        public MapMethodCoverage(MethodSignature method)
        {
            Method = method;
            Lines = new Dictionary<int, LineCoverage>();
            Branches = new Dictionary<BranchKey, BranchCoverage>();
        }

        public Dictionary<int, LineCoverage> Lines { get; }

        public Dictionary<BranchKey, BranchCoverage> Branches { get; }

        public MethodSignature Method { get; }

        public MethodCoverage ToMethodCoverage()
        {
            var methodCoverage = new MethodCoverage(Method);
            methodCoverage.Lines.AddRange(this.Lines.OrderBy(x => x.Key).Select(x => x.Value));
            methodCoverage.Branches.AddRange(this.Branches.OrderBy(x => x.Key).Select(x => x.Value));
            return methodCoverage;
        }
    }

    private readonly record struct BranchKey(int LineNumber, int BlockNumber, int BranchNumber) : IComparable<BranchKey>, IComparable
    {
        public int CompareTo(BranchKey other)
        {
            var lineNumberComparison = LineNumber.CompareTo(other.LineNumber);
            if (lineNumberComparison != 0) return lineNumberComparison;
            var blockNumberComparison = BlockNumber.CompareTo(other.BlockNumber);
            if (blockNumberComparison != 0) return blockNumberComparison;
            return BranchNumber.CompareTo(other.BranchNumber);
        }

        public int CompareTo(object? obj)
        {
            if (ReferenceEquals(null, obj)) return 1;
            return obj is BranchKey other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(BranchKey)}");
        }

        public static bool operator <(BranchKey left, BranchKey right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator >(BranchKey left, BranchKey right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator <=(BranchKey left, BranchKey right)
        {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >=(BranchKey left, BranchKey right)
        {
            return left.CompareTo(right) >= 0;
        }
    }


}