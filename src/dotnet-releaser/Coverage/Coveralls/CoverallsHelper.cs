using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using DotNetReleaser.Helpers;
using DotNetReleaser.Logging;

namespace DotNetReleaser.Coverage.Coveralls;

/// <summary>
/// Helper class to convert <see cref="AssemblyCoverage"/> to the coveralls.io format.
/// </summary>
public static class CoverallsHelper
{
    public static List<CoverallsSourceFileData> ConvertToCoverallsSourceFiles(ISimpleLogger logger, List<AssemblyCoverage> coverages, string rootDirectory)
    {
        var fileCoverages = new Dictionary<string, MapFileCoverage>();
        foreach (var assemblyCoverage in coverages)
        {
            foreach (var fileCoverage in assemblyCoverage.Files)
            {
                if (!fileCoverages.TryGetValue(fileCoverage.FullPath, out var mapFileCoverage))
                {
                    var relativeFilePath = fileCoverage.FullPath.Substring(rootDirectory.Length).Trim(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }).Replace('\\', '/');
                    mapFileCoverage = new MapFileCoverage(relativeFilePath);
                    fileCoverages[fileCoverage.FullPath] = mapFileCoverage;
                }

                foreach (var classCoverage in fileCoverage.Classes)
                {
                    foreach (var methodCoverage in classCoverage.Methods)
                    {
                        // Collect line coverage
                        foreach (var line in methodCoverage.Lines)
                        {
                            if (!mapFileCoverage.Lines.TryGetValue(line.LineNumber, out var lineCoverage))
                            {
                                lineCoverage = new LineCoverage()
                                {
                                    LineNumber = line.LineNumber,
                                    Hits = line.Hits,
                                };
                                mapFileCoverage.Lines[line.LineNumber] = lineCoverage;
                            }
                            else
                            {
                                lineCoverage.Hits = Math.Max(lineCoverage.Hits, line.Hits);
                            }
                        }

                        // Collect branch coverage
                        foreach (var branch in methodCoverage.Branches)
                        {
                            var key = new BranchCoverageKey(branch.LineNumber, branch.BlockNumber, branch.BranchNumber);
                            if (!mapFileCoverage.Branches.TryGetValue(key, out var branchCoverage))
                            {
                                branchCoverage = new BranchCoverage()
                                {
                                    LineNumber = branch.LineNumber,
                                    BlockNumber = branch.BlockNumber,
                                    BranchNumber = branch.BranchNumber,
                                    Hits = branch.Hits,
                                };
                                mapFileCoverage.Branches[key] = branchCoverage;
                            }
                            else
                            {
                                branchCoverage.Hits = Math.Max(branchCoverage.Hits, branch.Hits);
                            }
                        }
                    }
                }
            }
        }

        var sourceFiles = new List<CoverallsSourceFileData>();
        foreach (var (fullPath, fileCoverage) in fileCoverages)
        {
            string digest = string.Empty;
            string source = string.Empty;
            int lineCount = 0;
            if (!File.Exists(fullPath))
            {
                logger.Warn($"Unable to find file {fullPath} to calculate digest for coverage.");
            }
            else
            {
                var stream = new MemoryStream();
                {
                    using var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
                    fileStream.CopyToAsync(stream);
                }
                digest = BitConverter.ToString(MD5.HashData(stream.ToArray())).Replace("-", "").ToLowerInvariant();
                stream.Position = 0;
                source = new StreamReader(stream).ReadToEnd();
                lineCount = source.CountNewLines();
            }

            var coverallsSourceFile = new CoverallsSourceFileData(fileCoverage.FullPath, digest)
            {
                Source = source
            };

            // Collect lines, 1 element per line: null (no info) or hits
            var maxLineNumber = fileCoverage.Lines.Values.Select(x => x.LineNumber).Max();
            coverallsSourceFile.Coverage = new int?[Math.Max(lineCount, maxLineNumber)];
            foreach (var fileCoverageLine in fileCoverage.Lines.Values)
            {
                // Index = 0 => LineNumber = 1
                coverallsSourceFile.Coverage[fileCoverageLine.LineNumber - 1] = fileCoverageLine.Hits;
            }
            
            // 4 elements in the array: line number, block number, branch number, hits
            if (fileCoverage.Branches.Count > 0)
            {
                var branches = coverallsSourceFile.Branches = new int[fileCoverage.Branches.Count * 4];
                int i = 0;
                foreach (var branch in fileCoverage.Branches.Values.OrderBy(x => x.LineNumber).ThenBy(y => y.BlockNumber).ThenBy(z => z.BranchNumber))
                {
                    branches[i++] = branch.LineNumber;
                    branches[i++] = branch.BlockNumber;
                    branches[i++] = branch.BranchNumber;
                    branches[i++] = branch.Hits;
                }
            }

            sourceFiles.Add(coverallsSourceFile);
        }

        return sourceFiles;
    }

    private class MapFileCoverage
    {
        public MapFileCoverage(string fullPath)
        {
            FullPath = fullPath;
            Lines = new Dictionary<int, LineCoverage>();
            Branches = new Dictionary<BranchCoverageKey, BranchCoverage>();
        }

        public string FullPath { get; init; }

        public Dictionary<int, LineCoverage> Lines { get; }

        public Dictionary<BranchCoverageKey, BranchCoverage> Branches { get; }
    }
}