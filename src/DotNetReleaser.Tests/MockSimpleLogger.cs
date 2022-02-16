using System;
using System.Text;
using DotNetReleaser.Logging;
using Microsoft.Extensions.Logging;

namespace DotNetReleaser.Tests;

public class MockSimpleLogger : ISimpleLogger
{
    public MockSimpleLogger()
    {
        Output = new StringBuilder();
    }

    public StringBuilder Output { get; }

    public bool HasErrors { get; private set; }

    public void LogSimple(LogLevel level, Exception? exception, string? message, bool markup, params object?[] args)
    {
        if (level == LogLevel.Error) HasErrors = true;
        var prefix = level switch
        {
            LogLevel.Trace => "trace",
            LogLevel.Debug => "debug",
            LogLevel.Information => "info",
            LogLevel.Warning => "warn",
            LogLevel.Error => "error",
            LogLevel.Critical => "critical",
            LogLevel.None => "none",
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
        };
        Output.AppendLine($"{prefix}: {message}");
    }
}