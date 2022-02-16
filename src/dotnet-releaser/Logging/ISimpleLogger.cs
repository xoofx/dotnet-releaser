using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace DotNetReleaser.Logging;

public interface ISimpleLogger
{
    bool HasErrors { get; }

    void Info(string message);
    
    void Warn(string message);

    void Error(string message);
}

public static class SimpleLogger
{
    public static ISimpleLogger CreateConsoleLogger(ILoggerFactory factory, string? appName = null)
    {
        return new SimpleLoggerRedirect(factory.CreateLogger(appName ?? Assembly.GetEntryAssembly()?.FullName ?? Process.GetCurrentProcess().ProcessName));
    }
    
    private class SimpleLoggerRedirect : ISimpleLogger
    {
        private readonly ILogger _log;
        private int _logId;

        public SimpleLoggerRedirect(ILogger log)
        {
            _log = log;
        }

        public bool HasErrors { get; private set; }

        void ISimpleLogger.Info(string message)
        {
            lock (_log)
            {
                _log.LogInformation(new EventId(_logId++), message);
            }
        }

        void ISimpleLogger.Warn(string message)
        {
            lock (_log)
            {
                _log.LogWarning(new EventId(_logId++), message);
            }
        }

        void ISimpleLogger.Error(string message)
        {
            lock (_log)
            {
                HasErrors = true;
                _log.LogError(new EventId(_logId++), message);
            }
        }
    }
}


