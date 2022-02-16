using System;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace DotNetReleaser.Logging;

public class SpectreConsoleLoggerOptions
{
    public SpectreConsoleLoggerOptions()
    {
        LogLevel = LogLevel.Information;
        ConsoleSettings = new AnsiConsoleSettings();
        IncludeTimestamp = true;
        IncludeLogLevel = true;
        IncludeCategory = true;
        CultureInfo = CultureInfo.InvariantCulture;
        EventIdFormat = "####";
        TimestampFormat = "yyyy/MM/dd HH:mm:ss.fff";
        IncludeEventId = true;
        IncludeNewLine = false;
        SingleLine = false;
        Formatter = SpectreConsoleLoggerFormatter.Default;
        TimestampFormatter = SpectreConsoleLoggerFormatter.DefaultTimestampFormatter;
        EventIdFormatter = SpectreConsoleLoggerFormatter.DefaultEventIdFormatter;
        LogLevelFormatter = SpectreConsoleLoggerFormatter.DefaultLogLevelFormatter;
        CategoryFormatter = SpectreConsoleLoggerFormatter.DefaultCategoryFormatter;
    }

    public LogLevel LogLevel { get; set; }

    public Action<IAnsiConsole>? ConfigureConsole { get; set; }

    public AnsiConsoleSettings ConsoleSettings { get; set; }

    public bool IncludeTimestamp { get; set; }

    public string TimestampFormat { get; set; }

    public string EventIdFormat { get; set; }

    public CultureInfo CultureInfo { get; set; }

    public bool IncludeLogLevel { get; set; }

    public bool IncludeCategory { get; set; }

    public bool IncludeEventId { get; set; }

    public bool IncludeNewLine { get; set; }

    public bool SingleLine { get; set; }

    public SpectreConsoleLoggerFormatterDelegate Formatter { get; set; }

    public Action<SpectreConsoleLoggerOptions, StringBuilder, DateTime> TimestampFormatter { get; set; }
    
    public Action<SpectreConsoleLoggerOptions, StringBuilder, LogLevel> LogLevelFormatter { get; set; }

    public Action<SpectreConsoleLoggerOptions, StringBuilder, EventId> EventIdFormatter { get; set; }

    public Action<SpectreConsoleLoggerOptions, StringBuilder, string> CategoryFormatter { get; set; }

}