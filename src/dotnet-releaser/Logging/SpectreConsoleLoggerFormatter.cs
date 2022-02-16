using System;
using System.Text;
using Microsoft.Extensions.Logging;

namespace DotNetReleaser.Logging;

public static class SpectreConsoleLoggerFormatter
{
    public static readonly SpectreConsoleLoggerFormatterDelegate Default = DefaultImpl;

    public static Action<SpectreConsoleLoggerOptions, StringBuilder, DateTime> DefaultTimestampFormatter = TimestampFormatterImpl;

    public static Action<SpectreConsoleLoggerOptions, StringBuilder, EventId> DefaultEventIdFormatter = EventIdFormatterImpl;

    public static Action<SpectreConsoleLoggerOptions, StringBuilder, string> DefaultCategoryFormatter = CategoryFormatterImpl;

    public static Action<SpectreConsoleLoggerOptions, StringBuilder, LogLevel> DefaultLogLevelFormatter = LogLevelFormatterImpl;

    private static void TimestampFormatterImpl(SpectreConsoleLoggerOptions options, StringBuilder builder, DateTime dateTime)
    {
        builder.Append("[grey on black]");
        builder.Append(dateTime.ToString(options.TimestampFormat, options.CultureInfo));
        builder.Append("[/] ");
    }


    private static void EventIdFormatterImpl(SpectreConsoleLoggerOptions options, StringBuilder builder, EventId eventId)
    {
        builder.Append("[grey on black]");
        builder.Append("[[");
        builder.Append(eventId.Id.ToString(options.EventIdFormat, options.CultureInfo));
        builder.Append("]]");
        builder.Append("[/] ");
    }

    private static void CategoryFormatterImpl(SpectreConsoleLoggerOptions options, StringBuilder builder, string category)
    {
        builder.Append(category);
    }

    private static void LogLevelFormatterImpl(SpectreConsoleLoggerOptions options, StringBuilder builder, LogLevel logLevel)
    {
        builder.Append(GetLogLevelMarkup(logLevel));

        if (options.IncludeCategory || !options.IncludeEventId)
        {
            builder.Append(": ");
        }
    }

    private static int DefaultImpl(SpectreConsoleLoggerOptions options, StringBuilder builder, string category, LogLevel logLevel, EventId eventId)
    {
        if (options.IncludeTimestamp)
        {
            options.TimestampFormatter(options, builder, DateTime.Now);
        }

        if (options.IncludeLogLevel)
        {
            options.LogLevelFormatter(options, builder, logLevel);
        }
        
        int indexAfterIncludeLogLevel = builder.Length;
        
        if (options.IncludeCategory)
        {
            options.CategoryFormatter(options, builder, category);
        }

        if (options.IncludeEventId)
        {
            options.EventIdFormatter(options, builder, eventId);
        }

        if (options.IncludeNewLine)
        {
            builder.AppendLine();
        }

        return indexAfterIncludeLogLevel;
    }

    public static string GetLogLevelMarkup(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => "[silver on black]trce[/]",
            LogLevel.Debug => "[silver on black]dbug[/]",
            LogLevel.Information => "[green on black]info[/]",
            LogLevel.Warning => "[yellow on black]warn[/]",
            LogLevel.Error => "[black on maroon]fail[/]",
            LogLevel.Critical => "[white on maroon]crit[/]",
            LogLevel.None => "[silver on black]none[/]",
            _ => throw new ArgumentOutOfRangeException(nameof(logLevel))
        };
    }
}