using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace DotNetReleaser.Configuration;

public class RegexReplacer
{
    public RegexReplacer()
    {
        Pattern = string.Empty;
        Replace = string.Empty;
    }

    [DataMember(Name = "pattern")]
    public string Pattern { get; set; }

    [DataMember(Name = "replace")]
    public string Replace { get; set; }
    
    internal string Run(string input) => Regex.Replace(input, Pattern, Replace);
}