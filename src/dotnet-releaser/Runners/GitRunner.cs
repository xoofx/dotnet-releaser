// Copyright (c) Alexandre Mutel. All rights reserved.
// Licensed under the BSD-Clause 2 license.
// See license.txt file in the project root for full license information.

using CliWrap;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System;
using CliWrap.Builders;

namespace DotNetReleaser.Runners;

public static class GitRunner
{
    public static async Task<CommandResulExtended> Run(string command, IEnumerable<string> args, string? workingDirectory = null)
    {
        var stdOutAndErrorBuffer = new StringBuilder();

        var argsBuilder = new ArgumentsBuilder();
        argsBuilder.Add(command);
        foreach (var arg in args)
        {
            argsBuilder.Add(arg);
        }
        var arguments = argsBuilder.Build();

        var wrap = Cli.Wrap("git")
            .WithArguments(arguments)
            .WithWorkingDirectory(workingDirectory ?? Environment.CurrentDirectory)
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutAndErrorBuffer))
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdOutAndErrorBuffer))
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync();

        var result = await wrap.ConfigureAwait(false);
        return new CommandResulExtended(result, $"git {arguments}", stdOutAndErrorBuffer.ToString());
    }
}
