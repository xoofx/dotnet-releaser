using System;
using System.Text;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace DotNetReleaser.Logging;

internal class SpectreConsoleLogger : ILogger
{
    [ThreadStatic]
    internal static bool EnableMarkup;

    private readonly string _name;
    private readonly SpectreConsoleLoggerOptions _config;
    private readonly IAnsiConsole _console;

    public SpectreConsoleLogger(string name, IAnsiConsole console, SpectreConsoleLoggerOptions config)
    {
        _name = name;
        _console = console;
        _config = config;
    }

    public IAnsiConsole Console => _console;

    [ThreadStatic]
    private static StringBuilder? _stringBuilderTls;

    private static StringBuilder StringBuilderTls => _stringBuilderTls ??= new StringBuilder();

    [ThreadStatic]
    internal static SpectreConsoleLogger? Current;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var builder = StringBuilderTls;
        try
        {
            int indexAfterLogLevel = _config.Formatter(_config, builder, _name, logLevel, eventId);

            var previous = Current;
            Current = this;
            string formattedMessage;
            try
            {
                formattedMessage = formatter(state, exception);
            }
            finally
            {
                Current = previous;
            }

            builder.Append(EnableMarkup ? formattedMessage : Markup.Escape(formattedMessage));

            _console.MarkupLine(builder.ToString());
        }
        finally
        {
            builder.Length = 0;
        }
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= _config.LogLevel;
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        // Not supported
        return NullDisposer.Instance;
    }

    private class NullDisposer : IDisposable
    {
        public static readonly NullDisposer Instance = new NullDisposer();
        public void Dispose()
        {
        }
    }
}