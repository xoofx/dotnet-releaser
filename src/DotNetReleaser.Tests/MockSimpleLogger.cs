using System.Text;
using DotNetReleaser.Logging;

namespace DotNetReleaser.Tests;

public class MockSimpleLogger : ISimpleLogger
{
    public MockSimpleLogger()
    {
        Output = new StringBuilder();
    }

    public StringBuilder Output { get; }


    public bool HasErrors { get; private set; }

    public void Info(string message)
    {
        Output.AppendLine($"info: {message}");
    }

    public void Warn(string message)
    {
        Output.AppendLine($"warn: {message}");
    }

    public void Error(string message)
    {
        HasErrors = true;
        Output.AppendLine($"error: {message}");
    }
}