using System;
using Microsoft.Extensions.Logging;

namespace DotNetReleaser.Logging;

/// <summary>
/// ILogger extension methods for common scenarios.
/// </summary>
public static class SpectreConsoleLoggerExtensions
{
    //------------------------------------------DEBUG------------------------------------------//

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogDebug(0, exception, "Error while processing request from {Address}", address)</example>
    public static void LogDebugMarkup(this ILogger logger, EventId eventId, Exception? exception, string? message, params object?[] args)
    {
        logger.Log(LogLevel.Debug, eventId, exception, message, args);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogDebug(0, "Processing request from {Address}", address)</example>
    public static void LogDebugMarkup(this ILogger logger, EventId eventId, string? message, params object?[] args)
    {
        logger.LogMarkup(LogLevel.Debug, eventId, message, args);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogDebug(exception, "Error while processing request from {Address}", address)</example>
    public static void LogDebugMarkup(this ILogger logger, Exception? exception, string? message, params object?[] args)
    {
        logger.LogMarkup(LogLevel.Debug, exception, message, args);
    }

    /// <summary>
    /// Formats and writes a debug log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogDebug("Processing request from {Address}", address)</example>
    public static void LogDebugMarkup(this ILogger logger, string? message, params object?[] args)
    {
        logger.LogMarkup(LogLevel.Debug, message, args);
    }

    //------------------------------------------TRACE------------------------------------------//

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogTrace(0, exception, "Error while processing request from {Address}", address)</example>
    public static void LogTraceMarkup(this ILogger logger, EventId eventId, Exception? exception, string? message, params object?[] args)
    {
        logger.LogMarkup(LogLevel.Trace, eventId, exception, message, args);
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogTrace(0, "Processing request from {Address}", address)</example>
    public static void LogTraceMarkup(this ILogger logger, EventId eventId, string? message, params object?[] args)
    {
        logger.LogMarkup(LogLevel.Trace, eventId, message, args);
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogTrace(exception, "Error while processing request from {Address}", address)</example>
    public static void LogTraceMarkup(this ILogger logger, Exception? exception, string? message, params object?[] args)
    {
        logger.LogMarkup(LogLevel.Trace, exception, message, args);
    }

    /// <summary>
    /// Formats and writes a trace log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogTrace("Processing request from {Address}", address)</example>
    public static void LogTraceMarkup(this ILogger logger, string? message, params object?[] args)
    {
        logger.LogMarkup(LogLevel.Trace, message, args);
    }

    //------------------------------------------INFORMATION------------------------------------------//

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogInformation(0, exception, "Error while processing request from {Address}", address)</example>
    public static void LogInformationMarkup(this ILogger logger, EventId eventId, Exception? exception, string? message, params object?[] args)
    {
        logger.LogMarkup(LogLevel.Information, eventId, exception, message, args);
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogInformation(0, "Processing request from {Address}", address)</example>
    public static void LogInformationMarkup(this ILogger logger, EventId eventId, string? message, params object?[] args)
    {
        logger.LogMarkup(LogLevel.Information, eventId, message, args);
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogInformation(exception, "Error while processing request from {Address}", address)</example>
    public static void LogInformationMarkup(this ILogger logger, Exception? exception, string? message, params object?[] args)
    {
        logger.LogMarkup(LogLevel.Information, exception, message, args);
    }

    /// <summary>
    /// Formats and writes an informational log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogInformation("Processing request from {Address}", address)</example>
    public static void LogInformationMarkup(this ILogger logger, string? message, params object?[] args)
    {
        logger.LogMarkup(LogLevel.Information, message, args);
    }

    //------------------------------------------WARNING------------------------------------------//

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogWarning(0, exception, "Error while processing request from {Address}", address)</example>
    public static void LogWarningMarkup(this ILogger logger, EventId eventId, Exception? exception, string? message, params object?[] args)
    {
        logger.LogMarkup(LogLevel.Warning, eventId, exception, message, args);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogWarning(0, "Processing request from {Address}", address)</example>
    public static void LogWarningMarkup(this ILogger logger, EventId eventId, string? message, params object?[] args)
    {
        logger.LogMarkup(LogLevel.Warning, eventId, message, args);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogWarning(exception, "Error while processing request from {Address}", address)</example>
    public static void LogWarningMarkup(this ILogger logger, Exception? exception, string? message, params object?[] args)
    {
        logger.LogMarkup(LogLevel.Warning, exception, message, args);
    }

    /// <summary>
    /// Formats and writes a warning log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogWarning("Processing request from {Address}", address)</example>
    public static void LogWarningMarkup(this ILogger logger, string? message, params object?[] args)
    {
        logger.LogMarkup(LogLevel.Warning, message, args);
    }

    //------------------------------------------ERROR------------------------------------------//

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogError(0, exception, "Error while processing request from {Address}", address)</example>
    public static void LogErrorMarkup(this ILogger logger, EventId eventId, Exception? exception, string? message, params object?[] args)
    {
        logger.LogMarkup(LogLevel.Error, eventId, exception, message, args);
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogError(0, "Processing request from {Address}", address)</example>
    public static void LogErrorMarkup(this ILogger logger, EventId eventId, string? message, params object?[] args)
    {
        logger.LogMarkup(LogLevel.Error, eventId, message, args);
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogError(exception, "Error while processing request from {Address}", address)</example>
    public static void LogErrorMarkup(this ILogger logger, Exception? exception, string? message, params object?[] args)
    {
        logger.LogMarkup(LogLevel.Error, exception, message, args);
    }

    /// <summary>
    /// Formats and writes an error log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogError("Processing request from {Address}", address)</example>
    public static void LogErrorMarkup(this ILogger logger, string? message, params object?[] args)
    {
        logger.LogMarkup(LogLevel.Error, message, args);
    }

    //------------------------------------------CRITICAL------------------------------------------//

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogCritical(0, exception, "Error while processing request from {Address}", address)</example>
    public static void LogCriticalMarkup(this ILogger logger, EventId eventId, Exception? exception, string? message, params object?[] args)
    {
        logger.LogMarkup(LogLevel.Critical, eventId, exception, message, args);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogCritical(0, "Processing request from {Address}", address)</example>
    public static void LogCriticalMarkup(this ILogger logger, EventId eventId, string? message, params object?[] args)
    {
        logger.LogMarkup(LogLevel.Critical, eventId, message, args);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogCritical(exception, "Error while processing request from {Address}", address)</example>
    public static void LogCriticalMarkup(this ILogger logger, Exception? exception, string? message, params object?[] args)
    {
        logger.LogMarkup(LogLevel.Critical, exception, message, args);
    }

    /// <summary>
    /// Formats and writes a critical log message.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>logger.LogCritical("Processing request from {Address}", address)</example>
    public static void LogCriticalMarkup(this ILogger logger, string? message, params object?[] args)
    {
        logger.LogMarkup(LogLevel.Critical, message, args);
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="message">Format string of the log message.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    public static void LogMarkup(this ILogger logger, LogLevel logLevel, string? message, params object?[] args)
    {
        logger.LogMarkup(logLevel, 0, null, message, args);
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="message">Format string of the log message.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    public static void LogMarkup(this ILogger logger, LogLevel logLevel, EventId eventId, string? message, params object?[] args)
    {
        logger.LogMarkup(logLevel, eventId, null, message, args);
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    public static void LogMarkup(this ILogger logger, LogLevel logLevel, Exception? exception, string? message, params object?[] args)
    {
        logger.LogMarkup(logLevel, 0, exception, message, args);
    }

    /// <summary>
    /// Formats and writes a log message at the specified log level.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="logLevel">Entry will be written on this level.</param>
    /// <param name="eventId">The event id associated with the log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">Format string of the log message.</param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    public static void LogMarkup(this ILogger logger, LogLevel logLevel, EventId eventId, Exception? exception, string? message, params object?[] args)
    {
        if (logger == null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        var previousEscape = SpectreConsoleLogger.EnableMarkup;
        try
        {
            SpectreConsoleLogger.EnableMarkup = true;
            logger.Log(logLevel, eventId, exception, message, args, exception, args);
        }
        finally
        {
            SpectreConsoleLogger.EnableMarkup = previousEscape;
        }
    }

    //------------------------------------------Scope------------------------------------------//

    /// <summary>
    /// Formats the message and creates a scope.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to create the scope in.</param>
    /// <param name="messageFormat">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <returns>A disposable scope object. Can be null.</returns>
    /// <example>
    /// using(logger.BeginScope("Processing request from {Address}", address))
    /// {
    /// }
    /// </example>
    public static IDisposable BeginScopeMarkup(
        this ILogger logger,
        string messageFormat,
        params object?[] args)
    {
        if (logger == null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        var previousEscape = SpectreConsoleLogger.EnableMarkup;
        try
        {
            SpectreConsoleLogger.EnableMarkup = true;
            return logger.BeginScope(messageFormat, args);
        }
        finally
        {
            SpectreConsoleLogger.EnableMarkup = previousEscape;
        }
    }
}