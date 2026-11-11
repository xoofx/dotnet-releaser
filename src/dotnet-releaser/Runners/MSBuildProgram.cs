using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CliWrap;
using DotNetReleaser.Logging;
using XenoAtom.MsBuildPipeLogger;

namespace DotNetReleaser.Runners;

public record MSBuildResult(CommandResult CommandResult, string CommandLine, string Output, Dictionary<string, List<PipeItem>> TargetOutputs) : CommandResulExtended(CommandResult, CommandLine, Output);

public class MSBuildRunner : DotNetRunnerBase
{
    private static readonly TimeSpan PipeLoggerReaderCompletionTimeout = TimeSpan.FromSeconds(5);

    private readonly string _artifactFolder;
    private string? _pipeName;

    public MSBuildRunner(string artifactFolder) : base("msbuild")
    {
        _artifactFolder = artifactFolder;
        Arguments.AddRange(new List<string>()
        {
            "-nologo",
            "-noconlog",
        });
        Targets = new List<string>();
        Project = string.Empty;
        Configuration = "Release";
    }

    public string Project { get; set; }

    public string Configuration { get; set; }

    public string? CustomBeforeMicrosoftCommonProps { get; set; }

    public string? CustomBeforeMicrosoftCommonTargets { get; set; }

    public string? CustomAfterMicrosoftCommonProps { get; set; }

    public string? CustomAfterMicrosoftCommonTargets { get; set; }

    public List<string> Targets { get; }

    protected override IEnumerable<string> ComputeArguments()
    {
        var arguments = new List<string>(base.ComputeArguments());

        // We always generate a binlog
        // (For some unknown reasons, it is the only workaround when .NET 10+ is installed to have dotnet-releaser working)
        var targetText = string.Join("-", Targets);
        var msbuildLogFolder = Path.Combine(_artifactFolder, "msbuild_logs");
        Directory.CreateDirectory(msbuildLogFolder);
        
        arguments.Add($"-bl:{Path.Combine(msbuildLogFolder, $"msbuild-{Process.GetCurrentProcess().Id}-{Guid.CreateVersion7():N}-{targetText}.binlog")}");

        if (_pipeName is not null)
        {
            arguments.Add($"-logger:{PipeLoggerServer.GetLoggerSpecification($"name={_pipeName}")}");
        }

        foreach (var target in Targets)
        {

            arguments.Add($"-t:{target}");
        }

        arguments.Add(Project);

        return arguments;
    }

    protected override IReadOnlyDictionary<string, object> ComputeProperties()
    {
        var properties = new Dictionary<string, object>(base.ComputeProperties())
        {
            ["Configuration"] = Configuration
        };
        if (CustomBeforeMicrosoftCommonProps is not null) properties[nameof(CustomBeforeMicrosoftCommonProps)] = CustomBeforeMicrosoftCommonProps;
        if (CustomBeforeMicrosoftCommonTargets is not null) properties[nameof(CustomBeforeMicrosoftCommonTargets)] = CustomBeforeMicrosoftCommonTargets;
        if (CustomAfterMicrosoftCommonProps is not null) properties[nameof(CustomAfterMicrosoftCommonProps)] = CustomAfterMicrosoftCommonProps;
        if (CustomAfterMicrosoftCommonTargets is not null) properties[nameof(CustomAfterMicrosoftCommonTargets)] = CustomAfterMicrosoftCommonTargets;

        return properties;
    }

    public async Task<MSBuildResult> Run(ISimpleLogger logger)
    {
        if (string.IsNullOrEmpty(Project)) throw new InvalidOperationException("MSBuildRunner.Project cannot be empty");

        // Create the server. A named pipe serves every build submission of the msbuild process,
        // while an anonymous pipe would only receive the events of the first one.
        var readerSource = new CancellationTokenSource();
        _pipeName = PipeLoggerServer.CreateUniquePipeName();
        var reader = new NamedPipeLoggerServer(_pipeName, readerSource.Token);

        Thread? readerThread = null;

        try
        {
            var targets = new HashSet<string>(Targets, StringComparer.OrdinalIgnoreCase);
            var targetOutputs = new Dictionary<string, List<PipeItem>>(StringComparer.OrdinalIgnoreCase);
            reader.WarningRaised += args => { logger.Warn($"{args.File}({args.LineNumber},{args.ColumnNumber}): warning {args.Code}: {args.Message} [{args.ProjectFile}]"); };
            reader.ErrorRaised += args => { logger.Error($"{args.File}({args.LineNumber},{args.ColumnNumber}): error {args.Code}: {args.Message} [{args.ProjectFile}]"); };
            reader.TargetFinished += args =>
            {
                if (args.TargetName is null || !targets.Contains(args.TargetName)) return;
                targetOutputs[args.TargetName] = args.TargetOutputs.ToList();
            };

            readerThread = new Thread(reader.ReadAll)
            {
                Name = "NamedPipeLoggerServer.ReadAll",
                IsBackground = true,
            };
            readerThread.Start();

            var result = await base.RunImpl();

            // The msbuild process has exited: no new client can connect, let the received events drain.
            reader.StopListening();
            if (!WaitForReaderThread(readerThread))
            {
                logger.Warn("Timed out waiting for the MSBuild pipe logger reader to finish. Some target outputs may be unavailable.");
                readerSource.Cancel();
            }

            if (result.CommandResult.ExitCode != 0)
            {
                logger.Error($"Failing to run {result.CommandLine}. Reason: {result.Output}");
                return new MSBuildResult(result.CommandResult, result.CommandLine, result.Output, new Dictionary<string, List<PipeItem>>());
            }

            return new MSBuildResult(result.CommandResult, result.CommandLine, result.Output, targetOutputs);
        }
        finally
        {
            // Keep disposal off the thread pool so a blocked pipe transport cannot block this command
            // or leave thread-pool workers occupied by zombie shutdown work.
            var disposeReader = new Thread(() =>
            {
                try
                {
                    reader.Dispose();
                }
                catch
                {
                    // ignore exceptions;
                }
            }) { IsBackground = true };
            disposeReader.Start();

            _pipeName = null;
        }

    }

    private static bool WaitForReaderThread(Thread? readerThread)
    {
        if (readerThread is null)
        {
            return true;
        }

        return readerThread.Join(PipeLoggerReaderCompletionTimeout);
    }
}
