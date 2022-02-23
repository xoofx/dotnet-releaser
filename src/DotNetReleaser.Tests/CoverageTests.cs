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

        Assert.AreEqual(2, assemblyCoverages.Count, "Invalid number of assemblies");

        Assert.AreEqual(new HitCoverage(2664, 3162), assemblyCoverages[0].LineRate);
        Assert.AreEqual(new HitCoverage(1738, 2182), assemblyCoverages[0].BranchRate);

        Assert.AreEqual(new HitCoverage(796, 842), assemblyCoverages[1].LineRate);
        Assert.AreEqual(new HitCoverage(96, 150), assemblyCoverages[1].BranchRate);
        
        //foreach (var assemblyCoverage in assemblyCoverages)
        //{
        //    Console.WriteLine($"{assemblyCoverage.Name} LineRate: {assemblyCoverage.LineRate.Rate} BranchRate: {assemblyCoverage.BranchRate.Rate}");
        //    foreach (var file in assemblyCoverage.Files)
        //    {
        //        Console.WriteLine($"  File {file.FullPath} LineRate: {file.LineRate.Rate} BranchRate: {file.BranchRate.Rate}");
        //        foreach (var classCoverage in file.Classes)
        //        {
        //            Console.WriteLine($"    Class {classCoverage.Name} LineRate: {classCoverage.LineRate.Rate} BranchRate: {classCoverage.BranchRate.Rate}");
        //        }
        //    }
        //}
    }
}