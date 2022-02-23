using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace DotNetReleaser.Logging;

public class SpectreConsoleLoggerProvider : ILoggerProvider
{
    private readonly SpectreConsoleLoggerOptions _config;
    private readonly ConcurrentDictionary<string, SpectreConsoleLogger> _loggers = new();
    private readonly IAnsiConsole _console;

    public SpectreConsoleLoggerProvider(SpectreConsoleLoggerOptions config)
    {
        _config = config;
        _console = AnsiConsole.Create(config.ConsoleSettings);
        config.ConfigureConsole?.Invoke(_console);
    }
    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new SpectreConsoleLogger(name, _console, _config));
    }

    public void Dispose()
    {
        _loggers.Clear();
    }
}