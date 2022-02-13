// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace DotNetReleaser.Logging;

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.IO;

/// <summary>
/// We are duplicating the code from https://github.com/dotnet/runtime/blob/38259c83703dbf6df50c814f7504e7073b5ffaef/src/libraries/Microsoft.Extensions.Logging.Console/src/SimpleConsoleFormatter.cs
/// in order to write a more adequate single line mode to lunet logging:
///
/// - By default, we want the logging on a single line
/// - BUT if the message of the logging has multiple lines we want its lines to be preserved (e.g multiple line parsing error messages from Scriban)
///
/// Search for CHANGE in this file to see the changes applied
/// </summary>
internal class SimpleConsoleFormatter : ConsoleFormatter, IDisposable
{
    private const string LoglevelPadding = ": ";
    private static readonly string _messagePadding = new string(' ', GetLogLevelString(LogLevel.Information).Length + LoglevelPadding.Length);
    private static readonly string _newLineWithMessagePadding = Environment.NewLine + _messagePadding;
    private IDisposable _optionsReloadToken;

    public SimpleConsoleFormatter(IOptionsMonitor<SimpleConsoleFormatterOptions> options)
        : base(ConsoleFormatterNames.Simple)
    {
        ReloadLoggerOptions(options.CurrentValue);
        _optionsReloadToken = options.OnChange(ReloadLoggerOptions);
    }

    private void ReloadLoggerOptions(SimpleConsoleFormatterOptions options)
    {
        FormatterOptions = options;
    }

    public void Dispose()
    {
        _optionsReloadToken?.Dispose();
    }

    internal SimpleConsoleFormatterOptions FormatterOptions { get; set; }

    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider scopeProvider, TextWriter textWriter)
    {
        string message = logEntry.Formatter(logEntry.State, logEntry.Exception);
        if (logEntry.Exception == null && message == null)
        {
            return;
        }

        LogLevel logLevel = logEntry.LogLevel;
        ConsoleColors logLevelColors = GetLogLevelConsoleColors(logLevel);
        string logLevelString = GetLogLevelString(logLevel);

        string timestamp = null;
        string timestampFormat = FormatterOptions.TimestampFormat;
        if (timestampFormat != null)
        {
            DateTimeOffset dateTimeOffset = GetCurrentDateTime();
            timestamp = dateTimeOffset.ToString(timestampFormat);
        }

        if (timestamp != null)
        {
            textWriter.Write(timestamp);
        }

        if (logLevelString != null)
        {
            textWriter.WriteColoredMessage(logLevelString, logLevelColors.Background, logLevelColors.Foreground);
        }

        CreateDefaultLogMessage(textWriter, logEntry, message, scopeProvider);
    }

    private void CreateDefaultLogMessage<TState>(TextWriter textWriter, in LogEntry<TState> logEntry, string message, IExternalScopeProvider scopeProvider)
    {
        bool singleLine = FormatterOptions.SingleLine;
        int eventId = logEntry.EventId.Id;
        Exception exception = logEntry.Exception;

        // Example:
        // info: ConsoleApp.Program[10]
        //       Request received

        // category and event id
        textWriter.Write(LoglevelPadding);
        textWriter.Write(logEntry.Category);
        textWriter.Write('[');

#if NETCOREAPP
        Span<char> span = stackalloc char[10];
        if (eventId.TryFormat(span, out int charsWritten))
            textWriter.Write(span.Slice(0, charsWritten));
        else
#endif
            textWriter.Write(eventId.ToString());

        textWriter.Write(']');

        if (!singleLine)
        {
            textWriter.Write(Environment.NewLine);
        }

        // scope information
        WriteScopeInformation(textWriter, scopeProvider, singleLine);
        WriteMessage(textWriter, message, singleLine);

        // Example:
        // System.InvalidOperationException
        //    at Namespace.Class.Function() in File:line X
        if (exception != null)
        {
            // exception message
            WriteMessage(textWriter, exception.ToString(), singleLine);
        }

        if (singleLine)
        {
            textWriter.Write(Environment.NewLine);
        }
    }

    private void WriteMessage(TextWriter textWriter, string message, bool singleLine)
    {
        if (!string.IsNullOrEmpty(message))
        {
            if (singleLine)
            {
                textWriter.Write(' ');
                // CHANGE: Keep newlines + padding
                WriteReplacing(textWriter, Environment.NewLine, _newLineWithMessagePadding, message);
            }
            else
            {
                textWriter.Write(_messagePadding);
                WriteReplacing(textWriter, Environment.NewLine, _newLineWithMessagePadding, message);
                textWriter.Write(Environment.NewLine);
            }
        }

        static void WriteReplacing(TextWriter writer, string oldValue, string newValue, string message)
        {
            string newMessage = message;
            // CHANGE: If we are in an environment where \r\n is used
            // make sure that \n is replaced as well
            if (oldValue == "\r\n")
            {
                newMessage = newMessage.Replace("\r\n", "\n");
                oldValue = "\n";
            }
            newMessage = newMessage.Replace(oldValue, newValue);
            writer.Write(newMessage);
        }
    }

    private DateTimeOffset GetCurrentDateTime()
    {
        return FormatterOptions.UseUtcTimestamp ? DateTimeOffset.UtcNow : DateTimeOffset.Now;
    }

    private static string GetLogLevelString(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => "trce",
            LogLevel.Debug => "dbug",
            LogLevel.Information => "info",
            LogLevel.Warning => "warn",
            LogLevel.Error => "fail",
            LogLevel.Critical => "crit",
            _ => throw new ArgumentOutOfRangeException(nameof(logLevel))
        };
    }

    private ConsoleColors GetLogLevelConsoleColors(LogLevel logLevel)
    {
        bool disableColors = (FormatterOptions.ColorBehavior == LoggerColorBehavior.Disabled) ||
                             (FormatterOptions.ColorBehavior == LoggerColorBehavior.Default && System.Console.IsOutputRedirected);
        if (disableColors)
        {
            return new ConsoleColors(null, null);
        }

        // We must explicitly set the background color if we are setting the foreground color,
        // since just setting one can look bad on the users console.
        return logLevel switch
        {
            LogLevel.Trace => new ConsoleColors(ConsoleColor.Gray, ConsoleColor.Black),
            LogLevel.Debug => new ConsoleColors(ConsoleColor.Gray, ConsoleColor.Black),
            LogLevel.Information => new ConsoleColors(ConsoleColor.DarkGreen, ConsoleColor.Black),
            LogLevel.Warning => new ConsoleColors(ConsoleColor.Yellow, ConsoleColor.Black),
            LogLevel.Error => new ConsoleColors(ConsoleColor.Black, ConsoleColor.DarkRed),
            LogLevel.Critical => new ConsoleColors(ConsoleColor.White, ConsoleColor.DarkRed),
            _ => new ConsoleColors(null, null)
        };
    }

    private void WriteScopeInformation(TextWriter textWriter, IExternalScopeProvider scopeProvider, bool singleLine)
    {
        if (FormatterOptions.IncludeScopes && scopeProvider != null)
        {
            bool paddingNeeded = !singleLine;
            scopeProvider.ForEachScope((scope, state) =>
            {
                if (paddingNeeded)
                {
                    paddingNeeded = false;
                    state.Write(_messagePadding);
                    state.Write("=> ");
                }
                else
                {
                    state.Write(" => ");
                }

                state.Write(scope);
            }, textWriter);

            if (!paddingNeeded && !singleLine)
            {
                textWriter.Write(Environment.NewLine);
            }
        }
    }

    private readonly struct ConsoleColors
    {
        public ConsoleColors(ConsoleColor? foreground, ConsoleColor? background)
        {
            Foreground = foreground;
            Background = background;
        }

        public ConsoleColor? Foreground { get; }

        public ConsoleColor? Background { get; }
    }
}

internal static class TextWriterExtensions
{
    public static void WriteColoredMessage(this TextWriter textWriter, string message, ConsoleColor? background, ConsoleColor? foreground)
    {
        // Order: backgroundcolor, foregroundcolor, Message, reset foregroundcolor, reset backgroundcolor
        if (background.HasValue)
        {
            textWriter.Write(AnsiParser.GetBackgroundColorEscapeCode(background.Value));
        }
        if (foreground.HasValue)
        {
            textWriter.Write(AnsiParser.GetForegroundColorEscapeCode(foreground.Value));
        }
        textWriter.Write(message);
        if (foreground.HasValue)
        {
            textWriter.Write(AnsiParser.DefaultForegroundColor); // reset to default foreground color
        }
        if (background.HasValue)
        {
            textWriter.Write(AnsiParser.DefaultBackgroundColor); // reset to the background color
        }
    }
}

internal class AnsiParser
{
    private readonly Action<string, int, int, ConsoleColor?, ConsoleColor?> _onParseWrite;
    public AnsiParser(Action<string, int, int, ConsoleColor?, ConsoleColor?> onParseWrite)
    {
        if (onParseWrite == null)
        {
            throw new ArgumentNullException(nameof(onParseWrite));
        }
        _onParseWrite = onParseWrite;
    }

    /// <summary>
    /// Parses a subset of display attributes
    /// Set Display Attributes
    /// Set Attribute Mode [{attr1};...;{attrn}m
    /// Sets multiple display attribute settings. The following lists standard attributes that are getting parsed:
    /// 1 Bright
    /// Foreground Colours
    /// 30 Black
    /// 31 Red
    /// 32 Green
    /// 33 Yellow
    /// 34 Blue
    /// 35 Magenta
    /// 36 Cyan
    /// 37 White
    /// Background Colours
    /// 40 Black
    /// 41 Red
    /// 42 Green
    /// 43 Yellow
    /// 44 Blue
    /// 45 Magenta
    /// 46 Cyan
    /// 47 White
    /// </summary>
    public void Parse(string message)
    {
        int startIndex = -1;
        int length = 0;
        int escapeCode;
        ConsoleColor? foreground = null;
        ConsoleColor? background = null;
        var span = message.AsSpan();
        const char EscapeChar = '\x1B';
        ConsoleColor? color = null;
        bool isBright = false;
        for (int i = 0; i < span.Length; i++)
        {
            if (span[i] == EscapeChar && span.Length >= i + 4 && span[i + 1] == '[')
            {
                if (span[i + 3] == 'm')
                {
                    // Example: \x1B[1m
                    if (IsDigit(span[i + 2]))
                    {
                        escapeCode = (int)(span[i + 2] - '0');
                        if (startIndex != -1)
                        {
                            _onParseWrite(message, startIndex, length, background, foreground);
                            startIndex = -1;
                            length = 0;
                        }
                        if (escapeCode == 1)
                            isBright = true;
                        i += 3;
                        continue;
                    }
                }
                else if (span.Length >= i + 5 && span[i + 4] == 'm')
                {
                    // Example: \x1B[40m
                    if (IsDigit(span[i + 2]) && IsDigit(span[i + 3]))
                    {
                        escapeCode = (int)(span[i + 2] - '0') * 10 + (int)(span[i + 3] - '0');
                        if (startIndex != -1)
                        {
                            _onParseWrite(message, startIndex, length, background, foreground);
                            startIndex = -1;
                            length = 0;
                        }
                        if (TryGetForegroundColor(escapeCode, isBright, out color))
                        {
                            foreground = color;
                            isBright = false;
                        }
                        else if (TryGetBackgroundColor(escapeCode, out color))
                        {
                            background = color;
                        }
                        i += 4;
                        continue;
                    }
                }
            }
            if (startIndex == -1)
            {
                startIndex = i;
            }
            int nextEscapeIndex = -1;
            if (i < message.Length - 1)
            {
                nextEscapeIndex = message.IndexOf(EscapeChar, i + 1);
            }
            if (nextEscapeIndex < 0)
            {
                length = message.Length - startIndex;
                break;
            }
            length = nextEscapeIndex - startIndex;
            i = nextEscapeIndex - 1;
        }
        if (startIndex != -1)
        {
            _onParseWrite(message, startIndex, length, background, foreground);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsDigit(char c) => (uint)(c - '0') <= ('9' - '0');

    internal const string DefaultForegroundColor = "\x1B[39m\x1B[22m"; // reset to default foreground color
    internal const string DefaultBackgroundColor = "\x1B[49m"; // reset to the background color

    internal static string GetForegroundColorEscapeCode(ConsoleColor color)
    {
        return color switch
        {
            ConsoleColor.Black => "\x1B[30m",
            ConsoleColor.DarkRed => "\x1B[31m",
            ConsoleColor.DarkGreen => "\x1B[32m",
            ConsoleColor.DarkYellow => "\x1B[33m",
            ConsoleColor.DarkBlue => "\x1B[34m",
            ConsoleColor.DarkMagenta => "\x1B[35m",
            ConsoleColor.DarkCyan => "\x1B[36m",
            ConsoleColor.Gray => "\x1B[37m",
            ConsoleColor.Red => "\x1B[1m\x1B[31m",
            ConsoleColor.Green => "\x1B[1m\x1B[32m",
            ConsoleColor.Yellow => "\x1B[1m\x1B[33m",
            ConsoleColor.Blue => "\x1B[1m\x1B[34m",
            ConsoleColor.Magenta => "\x1B[1m\x1B[35m",
            ConsoleColor.Cyan => "\x1B[1m\x1B[36m",
            ConsoleColor.White => "\x1B[1m\x1B[37m",
            _ => DefaultForegroundColor // default foreground color
        };
    }

    internal static string GetBackgroundColorEscapeCode(ConsoleColor color)
    {
        return color switch
        {
            ConsoleColor.Black => "\x1B[40m",
            ConsoleColor.DarkRed => "\x1B[41m",
            ConsoleColor.DarkGreen => "\x1B[42m",
            ConsoleColor.DarkYellow => "\x1B[43m",
            ConsoleColor.DarkBlue => "\x1B[44m",
            ConsoleColor.DarkMagenta => "\x1B[45m",
            ConsoleColor.DarkCyan => "\x1B[46m",
            ConsoleColor.Gray => "\x1B[47m",
            _ => DefaultBackgroundColor // Use default background color
        };
    }

    private static bool TryGetForegroundColor(int number, bool isBright, out ConsoleColor? color)
    {
        color = number switch
        {
            30 => ConsoleColor.Black,
            31 => isBright ? ConsoleColor.Red : ConsoleColor.DarkRed,
            32 => isBright ? ConsoleColor.Green : ConsoleColor.DarkGreen,
            33 => isBright ? ConsoleColor.Yellow : ConsoleColor.DarkYellow,
            34 => isBright ? ConsoleColor.Blue : ConsoleColor.DarkBlue,
            35 => isBright ? ConsoleColor.Magenta : ConsoleColor.DarkMagenta,
            36 => isBright ? ConsoleColor.Cyan : ConsoleColor.DarkCyan,
            37 => isBright ? ConsoleColor.White : ConsoleColor.Gray,
            _ => null
        };
        return color != null || number == 39;
    }

    private static bool TryGetBackgroundColor(int number, out ConsoleColor? color)
    {
        color = number switch
        {
            40 => ConsoleColor.Black,
            41 => ConsoleColor.DarkRed,
            42 => ConsoleColor.DarkGreen,
            43 => ConsoleColor.DarkYellow,
            44 => ConsoleColor.DarkBlue,
            45 => ConsoleColor.DarkMagenta,
            46 => ConsoleColor.DarkCyan,
            47 => ConsoleColor.Gray,
            _ => null
        };
        return color != null || number == 49;
    }
}