using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CliWrap;
using DotNetReleaser.Logging;
using Microsoft.Build.Framework;

namespace DotNetReleaser.Runners;

public record MSBuildResult(CommandResult CommandResult, string CommandLine, string Output, Dictionary<string, List<ITaskItem>> TargetOutputs) : DotNetResult(CommandResult, CommandLine, Output);

public class MSBuildRunner : DotNetRunnerBase
{
    private readonly string _binlogPath;

    public MSBuildRunner() : base("msbuild")
    {
        _binlogPath = $"{Path.GetTempFileName()}.binlog";
        Arguments.AddRange(new List<string>()
        {
            "-nologo",
            $"-logger:{nameof(Microsoft.Build.Logging.BinaryLogger)},\"{typeof(Microsoft.Build.Logging.StructuredLogger.StructuredLogger).Assembly.Location}\";\"{_binlogPath}\"",
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

        var result = await base.RunImpl();
        if (result.CommandResult.ExitCode != 0)
        {
            logger.Error($"Failing to run {result.CommandLine}. Reason: {result.Output}");
            return new MSBuildResult(result.CommandResult, result.CommandLine, result.Output, new Dictionary<string, List<ITaskItem>>());
        }

        var targets = new HashSet<string>(Targets, StringComparer.OrdinalIgnoreCase);
        var targetOutputs = new Dictionary<string, List<ITaskItem>>(StringComparer.OrdinalIgnoreCase);
        var reader = new Microsoft.Build.Logging.StructuredLogger.BinLogReader();
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
        reader.Replay(_binlogPath);

        return new MSBuildResult(result.CommandResult, result.CommandLine, result.Output, targetOutputs);
    }

    protected override void Dispose(bool disposing)
    {
        if (!disposing) return;

        if (File.Exists(_binlogPath))
        {
            File.Delete(_binlogPath);
        }
    }
}