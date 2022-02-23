using System.Linq;
using Wcwidth;

namespace DotNetReleaser.Helpers;

internal class StringHelper
{
    public static int Measure(string text)
    {
        return text.EnumerateRunes().Select(x => UnicodeCalculator.GetWidth(x.Value)).Sum();
    }
}