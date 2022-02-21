using System;
using System.IO;
using DotNetReleaser.Coverage;
using NUnit.Framework;

namespace DotNetReleaser.Tests;

public class CoverageTests
{
    [Test]
    public void TestParser()
    {
        using var reader = new StreamReader(Path.Combine(AppContext.BaseDirectory, "coverage.cobertura.xml"));
        var assemblyCoverages = CoberturaParser.Parse(reader);
        foreach (var assemblyCoverage in assemblyCoverages)
        {
            Console.WriteLine($"{assemblyCoverage.Name} LineRate: {assemblyCoverage.LineRate.Rate} BranchRate: {assemblyCoverage.BranchRate.Rate}");
            foreach (var file in assemblyCoverage.Files)
            {
                Console.WriteLine($"  File {file.FullPath} LineRate: {file.LineRate.Rate} BranchRate: {file.BranchRate.Rate}");
                foreach (var classCoverage in file.Classes)
                {
                    Console.WriteLine($"    Class {classCoverage.Name} LineRate: {classCoverage.LineRate.Rate} BranchRate: {classCoverage.BranchRate.Rate}");
                }
            }
        }
    }
}