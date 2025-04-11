using System.IO.Pipes;

namespace MsBuildPipeLogger
{
    public class NamedPipeWriter : PipeWriter
    {
        public string ServerName { get; }

        public string PipeName { get; }

        public NamedPipeWriter(string pipeName)
            : this(".", pipeName)
        {
        }

        public NamedPipeWriter(string serverName, string pipeName)
            : base(InitializePipe(serverName, pipeName))
        {
            ServerName = serverName;
            PipeName = pipeName;
        }

        private static PipeStream InitializePipe(string serverName, string pipeName)
        {
            NamedPipeClientStream pipeStream = new NamedPipeClientStream(serverName, pipeName, PipeDirection.Out);
            pipeStream.Connect();
            return pipeStream;
        }
    }
}
