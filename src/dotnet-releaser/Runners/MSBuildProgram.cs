using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CliWrap;
using DotNetReleaser.Logging;
using Microsoft.Build.Framework;
using MsBuildPipeLogger;

namespace DotNetReleaser.Runners;

public record MSBuildResult(CommandResult CommandResult, string CommandLine, string Output, Dictionary<string, List<ITaskItem>> TargetOutputs) : DotNetResult(CommandResult, CommandLine, Output);

public class MSBuildRunner : DotNetRunnerBase
{
    private string? _pipeHandle;

    public MSBuildRunner() : base("msbuild")
    {
        Arguments.AddRange(new List<string>()
        {
            "-nologo",
            "-noconlog"
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

        if (_pipeHandle is not null)
        {
            arguments.Add($"-logger:MsBuildPipeLogger.PipeLogger,{typeof(AnonymousPipeWriter).Assembly.Location};{_pipeHandle}");
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

        // Create the server
        var reader = new AnonymousPipeLoggerServer();

        // Get the pipe handle
        _pipeHandle = reader.GetClientHandle();

        //logger.Info($"MSBuild handle {_pipeHandle}");

        //var taskReader = Task.Factory.StartNew(() =>
        //{
        //    Console.WriteLine($"ReadingAll {Thread.CurrentThread.ManagedThreadId}");
        //    reader.ReadAll();
        //    Console.WriteLine($"Finished ReadingAll {Thread.CurrentThread.ManagedThreadId}");
        //});

        try
        {
            var targets = new HashSet<string>(Targets, StringComparer.OrdinalIgnoreCase);
            var targetOutputs = new Dictionary<string, List<ITaskItem>>(StringComparer.OrdinalIgnoreCase);
            //reader.MessageRaised += (sender, args) =>
            //{
            //    if (args.Importance == MessageImportance.High)
            //    {
            //        logger.Info($"{args.Message} [{args.ProjectFile}]");
            //    }
            //};
            reader.WarningRaised += (sender, args) => { logger.Warn($"{args.File}({args.LineNumber},{args.ColumnNumber}): warning {args.Code}: {args.Message} [{args.ProjectFile}]"); };
            reader.ErrorRaised += (sender, args) => { logger.Error($"{args.File}({args.LineNumber},{args.ColumnNumber}): error {args.Code}: {args.Message} [{args.ProjectFile}]"); };
            reader.TargetFinished += (sender, args) =>
            {
                var outputs = args.TargetOutputs?.OfType<ITaskItem>().ToList();
                if (!targets.Contains(args.TargetName)) return;
                if (outputs is not null)
                {
                    targetOutputs[args.TargetName] = outputs;
                }
            };

            RunAfterStart = () =>
            {
                //Console.WriteLine($"Start ReadAll {Thread.CurrentThread.ManagedThreadId}");
                reader.ReadAll();
                //Console.WriteLine($"End ReadAll {Thread.CurrentThread.ManagedThreadId}");
            };
            var result = await base.RunImpl();

            if (result.CommandResult.ExitCode != 0)
            {
                logger.Error($"Failing to run {result.CommandLine}. Reason: {result.Output}");
                return new MSBuildResult(result.CommandResult, result.CommandLine, result.Output, new Dictionary<string, List<ITaskItem>>());
            }

            return new MSBuildResult(result.CommandResult, result.CommandLine, result.Output, targetOutputs);
        }
        finally
        {
            // Make the disposing non blocking
            // we still have a case where the Dispose is stuck while trying to dispose the underlying socket
            // I haven't found a discussion about this problem, so I don't know if it is a bug in the code in MsBuildPipeLogger
            // or an a problem in .NET socket/pipe implementation on Linux for this particular case.
            // We don't use thread pooling to avoid filling it with zombie threads that would block it later.
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

            _pipeHandle = null;
            RunAfterStart = null;
        }

    }
}