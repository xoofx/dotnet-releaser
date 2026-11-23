using System;
using System.Collections.Generic;
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
    private string? _binLogPath;
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

    internal bool RequireTargetOutputs { get; set; }

    protected override IEnumerable<string> ComputeArguments()
    {
        var arguments = new List<string>(base.ComputeArguments());

        if (_binLogPath is not null)
        {
            arguments.Add($"-bl:{_binLogPath}");
        }

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

        // Keep the independent MSBuild log for diagnostics. The binary logger also enables target output logging.
        var msbuildLogFolder = Path.GetFullPath(Path.Combine(_artifactFolder, "msbuild_logs"));
        Directory.CreateDirectory(msbuildLogFolder);
        var targetText = string.Join("-", Targets);
        var binLogPath = Path.Combine(msbuildLogFolder, $"msbuild-{Environment.ProcessId}-{Guid.CreateVersion7():N}-{targetText}.binlog");

        // Create the server. A named pipe serves every build submission of the msbuild process,
        // while an anonymous pipe would only receive the events of the first one.
        var readerSource = new CancellationTokenSource();
        _pipeName = PipeLoggerServer.CreateUniquePipeName();
        var reader = new NamedPipeLoggerServer(_pipeName, readerSource.Token);
        _binLogPath = binLogPath;

        try
        {
            var targets = new HashSet<string>(Targets, StringComparer.OrdinalIgnoreCase);
            var receivedTargetOutputs = new Dictionary<string, List<PipeItem>>(StringComparer.OrdinalIgnoreCase);
            var buildStarted = 0;
            var buildFinished = 0;
            reader.BuildStarted += _ => Interlocked.Increment(ref buildStarted);
            reader.BuildFinished += _ => Interlocked.Increment(ref buildFinished);
            reader.WarningRaised += args => { logger.Warn($"{args.File}({args.LineNumber},{args.ColumnNumber}): warning {args.Code}: {args.Message} [{args.ProjectFile}]"); };
            reader.ErrorRaised += args => { logger.Error($"{args.File}({args.LineNumber},{args.ColumnNumber}): error {args.Code}: {args.Message} [{args.ProjectFile}]"); };
            reader.TargetFinished += args =>
            {
                if (args.TargetName is null || !targets.Contains(args.TargetName)) return;
                lock (receivedTargetOutputs)
                {
                    receivedTargetOutputs[args.TargetName] = args.TargetOutputs.ToList();
                }
            };

            var readerThread = new Thread(reader.ReadAll)
            {
                Name = "NamedPipeLoggerServer.ReadAll",
                IsBackground = true,
            };
            readerThread.Start();

            var result = await base.RunImpl().ConfigureAwait(false);

            // The msbuild process has exited: no new client can connect, let the received events drain.
            reader.StopListening();
            var readerCompleted = readerThread.Join(PipeLoggerReaderCompletionTimeout);
            var targetOutputs = receivedTargetOutputs;
            if (!readerCompleted)
            {
                logger.Warn($"Timed out waiting for the MSBuild pipe logger for `{Project}` (targets: `{targetText}`). " +
                            $"Some target outputs may be unavailable. MSBuild binlog: `{binLogPath}`.");
                readerSource.Cancel();

                // The reader may still dispatch events. Return a snapshot rather than the live dictionary.
                lock (receivedTargetOutputs)
                {
                    targetOutputs = new Dictionary<string, List<PipeItem>>(receivedTargetOutputs, receivedTargetOutputs.Comparer);
                }
            }

            if (result.HasErrors)
            {
                logger.Error($"Failing to run {result.CommandLine}. Reason: {result.Output}");
                return new MSBuildResult(result.CommandResult, result.CommandLine, result.Output, new Dictionary<string, List<PipeItem>>());
            }

            if (RequireTargetOutputs)
            {
                var targetFramework = Properties.TryGetValue("TargetFramework", out var framework) ? $", target framework `{framework}`" : "";
                foreach (var target in targets)
                {
                    var received = targetOutputs.TryGetValue(target, out var outputs);
                    if (outputs is { Count: > 0 }) continue;

                    var issue = received ? "the TargetFinished event contained no output items" : "no TargetFinished event was received";
                    logger.Error($"Unable to read required outputs for MSBuild target `{target}` in `{Project}` " +
                                 $"(configuration `{Configuration}`{targetFramework}). MSBuild exited with code 0, but {issue}. " +
                                 $"Pipe events (BuildStarted: {Volatile.Read(ref buildStarted)}, BuildFinished: {Volatile.Read(ref buildFinished)}). " +
                                 $"MSBuild binlog: `{binLogPath}`.");
                }
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
            _binLogPath = null;
        }
    }
}
