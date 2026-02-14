using System;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using DotNetReleaser.Helpers;
using XenoAtom.Logging;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace DotNetReleaser.Logging;

public interface ISimpleLogger
{
    bool HasErrors { get; }

    void LogStartGroup(string name);
    void LogEndGroup();
    void LogSimple(LogLevel level, Exception? exception, string? message, bool markup, params Visual?[] args);
}

public static class SimpleLoggerExtensions
{
    public static void Info(this ISimpleLogger log, string message) =>
        log.LogSimple(LogLevel.Info, null, message, false);

    public static void Warn(this ISimpleLogger log, string message) =>
        log.LogSimple(LogLevel.Warn, null, message, false);

    public static void Error(this ISimpleLogger log, string message) =>
        log.LogSimple(LogLevel.Error, null, message, false);

    public static void Debug(this ISimpleLogger log, string message) =>
        log.LogSimple(LogLevel.Debug, null, message, false);

    public static void InfoMarkup(this ISimpleLogger log, string message) =>
        log.LogSimple(LogLevel.Info, null, message, true);

    public static void InfoMarkup(this ISimpleLogger log, string message, params Visual[] renderable) =>
        log.LogSimple(LogLevel.Info, null, message, true, renderable);

    public static void WarnMarkup(this ISimpleLogger log, string message) =>
        log.LogSimple(LogLevel.Warn, null, message, true);

    public static void ErrorMarkup(this ISimpleLogger log, string message) =>
        log.LogSimple(LogLevel.Error, null, message, true);
}

public static class SimpleLogger
{
    public static ISimpleLogger CreateConsoleLogger(Logger? logger = null, string? appName = null)
    {
        var resolvedName = appName ?? Assembly.GetEntryAssembly()?.FullName ?? Process.GetCurrentProcess().ProcessName;
        var resolvedLogger = logger ?? LogManager.GetLogger(resolvedName);
        return new SimpleLoggerRedirect(resolvedLogger);
    }

    private sealed class SimpleLoggerRedirect : ISimpleLogger
    {
        private readonly Logger _log;
        private readonly bool _runningFromGitHubAction;
        private static readonly Regex MatchGitHubWorkflowCommand = new("^::[a-z]+");

        public SimpleLoggerRedirect(Logger log)
        {
            _log = log;
            _runningFromGitHubAction = GitHubActionHelper.IsRunningOnGitHubAction;
        }

        public bool HasErrors { get; private set; }

        public void LogStartGroup(string name)
        {
            if (_runningFromGitHubAction)
            {
                // https://docs.github.com/en/actions/using-workflows/workflow-commands-for-github-actions#grouping-log-lines
                // ::group::{title}
                // ::endgroup::
                Terminal.WriteLine($"::group::{name}");
            }

            Terminal.Write(new Rule(name));
            Terminal.Out.Flush();
        }

        public void LogEndGroup()
        {
            Terminal.WriteLine();
            if (_runningFromGitHubAction)
            {
                Terminal.WriteLine("::endgroup::");
            }
            Terminal.Out.Flush();
        }

        public void LogSimple(LogLevel level, Exception? exception, string? message, bool markup, params Visual?[] args)
        {
            if (message is not null && MatchGitHubWorkflowCommand.IsMatch(message))
            {
                Terminal.WriteLine(message);
                Terminal.Out.Flush();
                return;
            }

            if (level == LogLevel.Error)
            {
                HasErrors = true;
            }

            var finalMessage = message ?? string.Empty;
            if (exception is not null)
            {
                finalMessage = $"{finalMessage}{Environment.NewLine}{exception}";
            }

            if (markup)
            {
                _log.LogMarkup(level, finalMessage);
            }
            else
            {
                switch (level)
                {
                    case LogLevel.Trace:
                        _log.Trace(finalMessage);
                        break;
                    case LogLevel.Debug:
                        _log.Debug(finalMessage);
                        break;
                    case LogLevel.Info:
                        _log.Info(finalMessage);
                        break;
                    case LogLevel.Warn:
                        _log.Warn(finalMessage);
                        break;
                    case LogLevel.Error:
                        _log.Error(finalMessage);
                        break;
                    case LogLevel.Fatal:
                        _log.Fatal(finalMessage);
                        break;
                    default:
                        _log.Info(finalMessage);
                        break;
                }
            }

            foreach (var arg in args)
            {
                if (arg is not null)
                {
                    Terminal.Write(arg);
                }
            }
        }
    }
}
