using System;
using System.Text;
using DotNetReleaser.Logging;
using XenoAtom.Logging;
using XenoAtom.Terminal.UI;

namespace DotNetReleaser.Tests;

public class MockSimpleLogger : ISimpleLogger
{
    public MockSimpleLogger()
    {
        Output = new StringBuilder();
    }

    public StringBuilder Output { get; }

    public bool HasErrors { get; private set; }

    public void LogStartGroup(string name)
    {
    }

    public void LogEndGroup()
    {
    }

    public void LogSimple(LogLevel level, Exception? exception, string? message, bool markup, params Visual?[] args)
    {
        if (level == LogLevel.Error)
        {
            HasErrors = true;
        }

        var prefix = level switch
        {
            LogLevel.All => "all",
            LogLevel.Trace => "trace",
            LogLevel.Debug => "debug",
            LogLevel.Info => "info",
            LogLevel.Warn => "warn",
            LogLevel.Error => "error",
            LogLevel.Fatal => "fatal",
            LogLevel.None => "none",
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
        };
        Output.AppendLine($"{prefix}: {message}");
    }
}
