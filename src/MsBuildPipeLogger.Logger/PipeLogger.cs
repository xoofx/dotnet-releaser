using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace MsBuildPipeLogger
{
    /// <summary>
    /// Logger to send messages from the MSBuild logging system over an anonymous pipe.
    /// </summary>
    /// <remarks>
    /// Heavily based on the work of Kirill Osenkov and the MSBuildStructuredLog project.
    /// </remarks>
    public class PipeLogger : Logger
    {
        protected IPipeWriter Pipe { get; private set; }

        public override void Initialize(IEventSource eventSource)
        {
            InitializeEnvironmentVariables();
            Pipe = InitializePipeWriter();
            InitializeEvents(eventSource);
        }

        protected virtual void InitializeEnvironmentVariables()
        {
            Environment.SetEnvironmentVariable("MSBUILDTARGETOUTPUTLOGGING", "true");
            Environment.SetEnvironmentVariable("MSBUILDLOGIMPORTS", "1");
        }

        protected virtual IPipeWriter InitializePipeWriter() => ParameterParser.GetPipeFromParameters(Parameters);

        protected virtual void InitializeEvents(IEventSource eventSource)
        {
            eventSource.AnyEventRaised += (_, e) => Pipe.Write(e);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            if (Pipe != null)
            {
                Pipe.Dispose();
                Pipe = null;
            }
        }
    }
}
