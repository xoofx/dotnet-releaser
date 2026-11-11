using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Builders;

namespace DotNetReleaser.Runners;

public record CommandResulExtended(CommandResult CommandResult, string CommandLine, string Output)
{
    public bool HasErrors => CommandResult.ExitCode != 0;
}

[DebuggerDisplay("{" + nameof(ToDebuggerDisplay) + "(),nq}")]
public abstract class DotNetRunnerBase : IDisposable
{
    // The dotnet CLI and MSBuild export the paths of their SDK to their child processes. When dotnet-releaser
    // is itself started by one of them (e.g. from `dotnet test`), these paths can belong to another SDK than
    // the one selected by the global.json of the project: remove them so that dotnet resolves its own SDK.
    private static readonly string[] InheritedSdkEnvironmentVariables = ["MSBUILD_EXE_PATH", "MSBuildExtensionsPath", "MSBuildSDKsPath"];

    protected DotNetRunnerBase(string command)
    {
        Command = command ?? throw new ArgumentNullException(nameof(command));
        Arguments = new List<string>();
        EnvironmentVariables = new Dictionary<string, string?>();
        Properties = new Dictionary<string, object>();
        WorkingDirectory = Environment.CurrentDirectory;
    }

    public string Command { get; }

    public List<string> Arguments { get; }

    public Dictionary<string, string?> EnvironmentVariables { get; }

    public Dictionary<string, object> Properties { get; }

    public string WorkingDirectory { get; set; }

    public Action<string>? LogStandardOutput { get; set; }

    public Action<string>? LogStandardError { get; set; }

    protected Action? RunAfterStart { get; set; }

    protected virtual IEnumerable<string> ComputeArguments() => Arguments;

    protected virtual IReadOnlyDictionary<string, object> ComputeProperties() => Properties;

    protected async Task<CommandResulExtended> RunImpl()
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

    private async Task<CommandResulExtended> Run(string command, IEnumerable<string> args, IReadOnlyDictionary<string, object>? properties = null, string? workingDirectory = null)
    {
        var stdOutAndErrorBuffer = new StringBuilder();

        var arguments = GetFullArguments(command, args, properties);

        //Console.WriteLine($"dotnet {arguments}");

        var commandLine = Cli.Wrap("dotnet")
            .WithArguments(arguments)
            .WithWorkingDirectory(workingDirectory ?? Environment.CurrentDirectory)
            .WithStandardOutputPipe(LogStandardOutput is not null ? PipeTarget.ToDelegate(LogStandardOutput): PipeTarget.ToStringBuilder(stdOutAndErrorBuffer))
            .WithStandardErrorPipe(LogStandardError is not null ? PipeTarget.ToDelegate(LogStandardError) : PipeTarget.ToStringBuilder(stdOutAndErrorBuffer))
            .WithValidation(CommandResultValidation.None);

        var environmentVariables = new Dictionary<string, string?>();
        foreach (var name in InheritedSdkEnvironmentVariables)
        {
            environmentVariables[name] = null;
        }
        foreach (var environmentVariable in EnvironmentVariables)
        {
            environmentVariables[environmentVariable.Key] = environmentVariable.Value;
        }
        commandLine = commandLine.WithEnvironmentVariables(environmentVariables);

        var wrap = commandLine
            .ExecuteAsync();
        
        RunAfterStart?.Invoke();

        var result = await wrap.ConfigureAwait(false);
        return new CommandResulExtended(result, $"dotnet {arguments}",stdOutAndErrorBuffer.ToString());
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
