using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Builders;

namespace DotNetReleaser.Runners;

public record DotNetResult(CommandResult CommandResult, string CommandLine, string Output)
{
    public bool HasErrors => CommandResult.ExitCode != 0;
}

[DebuggerDisplay("{" + nameof(ToDebuggerDisplay) + "(),nq}")]
public abstract class DotNetRunnerBase : IDisposable
{
    protected DotNetRunnerBase(string command)
    {
        Command = command ?? throw new ArgumentNullException(nameof(command));
        Arguments = new List<string>();
        Properties = new Dictionary<string, object>();
        WorkingDirectory = Environment.CurrentDirectory;
    }

    public string Command { get; }

    public List<string> Arguments { get; }

    public Dictionary<string, object> Properties { get; }

    public string WorkingDirectory { get; set; }

    public Action<string>? LogStandardOutput { get; set; }

    public Action<string>? LogStandardError { get; set; }

    protected Action? RunAfterStart { get; set; }

    protected virtual IEnumerable<string> ComputeArguments() => Arguments;

    protected virtual IReadOnlyDictionary<string, object> ComputeProperties() => Properties;

    protected async Task<DotNetResult> RunImpl()
    {
        return await Run(Command, ComputeArguments(), ComputeProperties(), WorkingDirectory);
    }

    private string ToDebuggerDisplay()
    {
        return $"dotnet {GetFullArguments(Command, ComputeArguments(), ComputeProperties())}";
    }

    private static string GetFullArguments(string command, IEnumerable<string> arguments, IReadOnlyDictionary<string, object>? properties)
    {
        var argsBuilder = new ArgumentsBuilder();
        argsBuilder.Add($"{command}");

        // Pass all our user properties to msbuild
        if (properties != null)
        {
            foreach (var property in properties)
            {
                argsBuilder.Add($"-p:{property.Key}={GetPropertyValueAsString(property.Value)}");
            }
        }

        // Add all arguments
        foreach (var arg in arguments)
        {
            argsBuilder.Add(arg);
        }

        return argsBuilder.Build();
    }

    private static string GetPropertyValueAsString(object value)
    {
        if (value is bool b) return b ? "true" : "false";
        if (value is IFormattable formattable) return formattable.ToString(null, CultureInfo.InvariantCulture);
        return value.ToString() ?? string.Empty;
    }

    private async Task<DotNetResult> Run(string command, IEnumerable<string> args, IReadOnlyDictionary<string, object>? properties = null, string? workingDirectory = null)
    {
        var stdOutAndErrorBuffer = new StringBuilder();

        var arguments = GetFullArguments(command, args, properties);

        //Console.WriteLine($"dotnet {arguments}");

        var wrap = Cli.Wrap("dotnet")
            .WithArguments(arguments)
            .WithWorkingDirectory(workingDirectory ?? Environment.CurrentDirectory)
            .WithStandardOutputPipe(LogStandardOutput is not null ? PipeTarget.ToDelegate(LogStandardOutput): PipeTarget.ToStringBuilder(stdOutAndErrorBuffer))
            .WithStandardErrorPipe(LogStandardError is not null ? PipeTarget.ToDelegate(LogStandardError) : PipeTarget.ToStringBuilder(stdOutAndErrorBuffer))
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync();
        
        RunAfterStart?.Invoke();

        var result = await wrap.ConfigureAwait(false);
        return new DotNetResult(result, $"dotnet {arguments}",stdOutAndErrorBuffer.ToString());
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}