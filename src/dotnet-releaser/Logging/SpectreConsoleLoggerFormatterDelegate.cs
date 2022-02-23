using System.Text;
using Microsoft.Extensions.Logging;

namespace DotNetReleaser.Logging;

public delegate int SpectreConsoleLoggerFormatterDelegate(SpectreConsoleLoggerOptions options, StringBuilder builder, string category, LogLevel logLevel, EventId eventId);