using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;
using NuGet.Versioning;
using Spectre.Console;
using Spectre.Console.Rendering;
using Wcwidth;

namespace DotNetReleaser.Logging;

internal class SpectreConsoleLogger : ILogger
{
    [ThreadStatic]
    internal static bool EnableMarkup;
    [ThreadStatic]
    private static StringBuilder? _stringBuilderTls;
    private static StringBuilder StringBuilderTls => _stringBuilderTls ??= new StringBuilder();
    [ThreadStatic]
    internal static SpectreConsoleLogger? Current;

    private readonly string _name;
    private readonly SpectreConsoleLoggerOptions _options;
    private readonly IAnsiConsole _console;
    private readonly IAnsiConsole _offScreenConsole;
    private readonly AnsiConsoleStringWriterOutput _offScreenOutput;

    public SpectreConsoleLogger(string name, IAnsiConsole console, SpectreConsoleLoggerOptions options)
    {
        _name = name;
        _console = console;
        _options = options;

        // Create a console to output to a StringBuilder.
        _offScreenOutput = new AnsiConsoleStringWriterOutput(console.Profile.Out);
        // Fetch the encoding from the current console
        _offScreenOutput.SetEncoding(_console.Profile.Encoding);
        _offScreenConsole = AnsiConsole.Create(new AnsiConsoleSettings()
        {
            // Fetch what was detected
            Ansi = _console.Profile.Capabilities.Ansi ? AnsiSupport.Yes : AnsiSupport.No,
            ColorSystem = GetColorSystemSupport(_console.Profile.Capabilities.ColorSystem),
            Enrichment = _options.ConsoleSettings.Enrichment,
            EnvironmentVariables = _options.ConsoleSettings.EnvironmentVariables,
            ExclusivityMode = null,
            Interactive = InteractionSupport.No,
            Out = _offScreenOutput,
        });

        static ColorSystemSupport GetColorSystemSupport(ColorSystem colorSystem)
        {
            return colorSystem switch
            {
                ColorSystem.NoColors => ColorSystemSupport.NoColors,
                ColorSystem.Legacy => ColorSystemSupport.Legacy,
                ColorSystem.Standard => ColorSystemSupport.Standard,
                ColorSystem.EightBit => ColorSystemSupport.EightBit,
                ColorSystem.TrueColor => ColorSystemSupport.TrueColor,
                _ => throw new ArgumentOutOfRangeException(nameof(colorSystem), colorSystem, null)
            };
        }
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var builder = StringBuilderTls;
        try
        {
            int indexAfterLogLevel = _options.Formatter(_options, builder, _name, logLevel, eventId);
            
            // Calculate indent
            int indent = 0;
            if ((_options.IncludeNewLine || _options.IndentAfterNewLine))
            {
                if (_options.UseFixedIndent)
                {
                    // Fixed indentation
                    indent = _options.FixedIndent;
                }
                else
                {
                    var prefix = Markup.Remove(builder.ToString(0, indexAfterLogLevel));

                    // Calculate the indentation level up to the category/eventid
                    for (int i = 0; i < prefix.Length; i++)
                    {
                        var c = prefix[i];
                        int uni = c;
                        if (char.IsHighSurrogate(c) && i + 1 < indexAfterLogLevel && char.IsLowSurrogate(prefix[i + 1]))
                        {
                            i++;
                            uni = char.ConvertToUtf32(c, prefix[i]);
                        }

                        indent += UnicodeCalculator.GetWidth(uni);
                    }
                }
            }

            var previous = Current;
            Current = this;
            string formattedMessage;
            bool rawFormattedMessage = false;
            StringBuilder builderForMessage = builder;
            bool enableMarkup = EnableMarkup;
            try
            {
                if (state is MarkupTextAndRenderable messageAndRenderable)
                {
                    _offScreenOutput.Reset();
                    if (!string.IsNullOrEmpty(messageAndRenderable.Message))
                    {
                        _offScreenConsole.MarkupLine(messageAndRenderable.Message);
                    }

                    foreach (var renderable in messageAndRenderable.Renderables)
                    {
                        _offScreenConsole.Write(renderable);
                        _offScreenConsole.WriteLine();
                    }

                    formattedMessage = _offScreenOutput.ToString();
                    builderForMessage = _offScreenOutput.Builder;
                    builderForMessage.Clear();
                    rawFormattedMessage = true;
                }
                else
                {
                    formattedMessage = formatter(state, exception);
                }
            }
            finally
            {
                Current = previous;
            }

            if (!string.IsNullOrEmpty(formattedMessage))
            {
                var message = rawFormattedMessage || enableMarkup ? formattedMessage : Markup.Escape(formattedMessage);

                if ((_options.IncludeNewLine || _options.IndentAfterNewLine))
                {
                    AppendMessage(builderForMessage, message, indent, _options.IncludeNewLine, _options.SingleLine, _options.IndentAfterNewLine);
                }
                else
                {
                    if (builderForMessage.Length > 0 && !char.IsWhiteSpace(builderForMessage[^1]))
                    {
                        builderForMessage.Append(' ');
                    }

                    if (_options.SingleLine)
                    {
                        AppendMessage(builderForMessage, message, 0, _options.IncludeNewLine, true, false);
                    }
                    else
                    {
                        builderForMessage.Append(message);
                    }
                }
            }

            if (rawFormattedMessage)
            {
                _console.Markup(builder.ToString());
                var writer = _options.ConsoleSettings.Out?.Writer ?? System.Console.Out;
                var message = builderForMessage.ToString();
                if (message.EndsWith('\n'))
                {
                    writer.Write(message);
                }
                else
                {
                    writer.WriteLine(message);
                }
            }
            else
            {
                _console.MarkupLine(builder.ToString());
            }
        }
        finally
        {
            builder.Length = 0;
        }
    }

    private static void AppendMessage(StringBuilder builder, string message, int indent, bool hasNewLine, bool singleLine, bool indentAfterNewLine)
    {
        if (!string.IsNullOrEmpty(message))
        {
            int currentIndex = 0;

            bool isFirstLine = true;
            bool needsNewLine = false;
            while (currentIndex < message.Length)
            {
                needsNewLine = false;
                if (!isFirstLine || hasNewLine)
                {
                    if (!singleLine)
                    {
                        if (!isFirstLine && !hasNewLine)
                        {
                            builder.AppendLine();
                        }

                        if (indentAfterNewLine)
                        {
                            builder.Append(' ', indent);
                        }
                    }
                    else
                    {
                        builder.Append(' ', singleLine ? 1 : indent);
                    }
                }

                var nextIndex = message.IndexOf('\n', currentIndex);
                if (nextIndex >= 0)
                {
                    var nextCurrentIndex = nextIndex + 1;
                    do
                    {
                        nextIndex--;
                    } while (nextIndex >= currentIndex && message[nextIndex] == '\r');

                    var length = nextIndex - currentIndex + 1;
                    if (length > 0)
                    {
                        builder.Append(message, currentIndex, length);
                    }
                    currentIndex = nextCurrentIndex;
                    needsNewLine = true;
                    hasNewLine = false;
                }
                else
                {
                    builder.Append(message, currentIndex, message.Length - currentIndex);
                    break;
                }

                isFirstLine = false;
            }

            if (!singleLine && needsNewLine)
            {
                builder.AppendLine();
            }
        }
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= _options.LogLevel;
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        // Not supported
        return NullDisposer.Instance;
    }

    public record MarkupTextAndRenderable(string? Message, IRenderable[] Renderables);

    private class NullDisposer : IDisposable
    {
        public static readonly NullDisposer Instance = new NullDisposer();
        public void Dispose()
        {
        }
    }

    private class AnsiConsoleStringWriterOutput : IAnsiConsoleOutput
    {
        private readonly StringBuilder _builder;
        private Encoding _encoding;

        public AnsiConsoleStringWriterOutput(IAnsiConsoleOutput inputConsole)
        {
            _encoding = Encoding.Default;
            _builder = new StringBuilder();
            Writer = new StringWriter(_builder);
            IsTerminal = false;
            Width = inputConsole.Width <= 0 || inputConsole.Width == int.MaxValue ? 120 : inputConsole.Width;
            Height = inputConsole.Height <= 0 || inputConsole.Height == int.MaxValue ? 120 : inputConsole.Height;
        }

        public void SetEncoding(Encoding encoding)
        {
            _encoding = encoding;
        }

        public StringBuilder Builder => _builder;

        public TextWriter Writer { get; }

        public bool IsTerminal { get; }

        public int Width { get; }

        public int Height { get; }

        public void Reset()
        {
            _builder.Clear();
        }

        public override string ToString()
        {
            return _builder.ToString();
        }
    }
}