using System.IO;
using System.Text;
using Spectre.Console;

namespace DotNetReleaser.Logging;

/// <summary>
/// Allows to override the output to set the width/heigth (e.g for console out in GitHub Action)
/// </summary>
public class AnsiConsoleOutputOverride : IAnsiConsoleOutput
{
    private readonly IAnsiConsoleOutput _delegate;

    public AnsiConsoleOutputOverride(IAnsiConsoleOutput @delegate)
    {
        _delegate = @delegate;
        Width = 80;
        Height = 80;
    }

    public void SetEncoding(Encoding encoding)
    {
        _delegate.SetEncoding(encoding);
    }

    public TextWriter Writer => _delegate.Writer;

    public bool IsTerminal => _delegate.IsTerminal;

    public int Width { get; set; }

    public int Height { get; set; }
}