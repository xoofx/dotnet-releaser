using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DotNetReleaser.Coverage;
using DotNetReleaser.Coverage.Coveralls;
using DotNetReleaser.Helpers;
using NUnit.Framework;

namespace DotNetReleaser.Tests;

public class CoverageTests
{
    [Test]
    public void ParseEventJson()
    {
        var evt = JsonHelper.FromFile(Path.Combine(AppContext.BaseDirectory, "event.json")) as Dictionary<string, object>;

        Assert.NotNull(evt);

        if (evt!.TryGetValue("number", out var prObject) && prObject is int value)
        {
            Assert.AreEqual(2, value);
        }
    }

    [Test]
    public void TestParser()
    {
        using var reader = new FileStream(Path.Combine(AppContext.BaseDirectory, "coverage.json"), FileMode.Open, FileAccess.Read);
        var assemblyCoverages = CoverletJsonParser.Parse(reader);

        Assert.AreEqual(2, assemblyCoverages.Count, "Invalid number of assemblies");

        Assert.AreEqual("Tomlyn", assemblyCoverages[0].Name);
        Assert.AreEqual("Tomlyn.Tests", assemblyCoverages[1].Name);

        Assert.AreEqual(new HitCoverage(2664, 3162), assemblyCoverages[0].LineRate);
        Assert.AreEqual(new HitCoverage(1740, 2182), assemblyCoverages[0].BranchRate);

        Assert.AreEqual(new HitCoverage(817, 863), assemblyCoverages[1].LineRate);
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

    [Test]
    public void TestConverterToCoveralls()
    {
        using var reader = new FileStream(Path.Combine(AppContext.BaseDirectory, "coverage.json"), FileMode.Open, FileAccess.Read);
        var assemblyCoverages = CoverletJsonParser.Parse(reader);

        var logger = new MockSimpleLogger();
        var result = CoverallsHelper.ConvertToCoverallsSourceFiles(logger, assemblyCoverages, @"C:\code\Tomlyn\");

        
        //var stream = new MemoryStream();
        //JsonSerializer.Serialize(stream, result, new JsonSerializerOptions() { WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull});
        //stream.Position = 0;
        //var utf8 = Encoding.UTF8.GetString(stream.ToArray());
        //Console.WriteLine(utf8);
    }
}