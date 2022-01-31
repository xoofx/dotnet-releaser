using System.Text;

namespace DotNetReleaser.Helpers;

internal class RubyHelper
{
    public static string GetRubyClassNameFromAppName(string appName)
    {
        // Make sure that the ruby class name is valid 
        // Capitalize the name
        var classNameBuilder = new StringBuilder();
        bool nextUpper = true;
        foreach (var c in appName)
        {
            if (c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z' || c >= '0' && c <= '9')
            {
                classNameBuilder.Append(nextUpper && !char.IsUpper(c) ? char.ToUpperInvariant(c) : c);
                nextUpper = false;
            }
            else if (c == '_')
            {
                classNameBuilder.Append('_');
                nextUpper = true;
            }
            else
            {
                nextUpper = true;
            }
        }
        var className = classNameBuilder.ToString();
        return className;
    }
}