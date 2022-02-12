using System;
using NUnit.Framework;

namespace DotNetReleaser.Tests;

public static class AssertHelper
{
    public static void Equals(string expected, string result)
    {
        expected = NormalizeEndOfLines(expected);
        result = NormalizeEndOfLines(result);
        if (result != expected)
        {
            Console.WriteLine("Result");
            Console.WriteLine("-------------------------------------------------");
            Console.WriteLine(result);
            Console.WriteLine("Expected");
            Console.WriteLine("-------------------------------------------------");
            Console.WriteLine(expected);
            Assert.AreEqual(expected, result);
        }
    }

    private static string NormalizeEndOfLines(string text) => text.Replace("\r\n", "\n");

}