using System.IO.Pipes;

namespace MsBuildPipeLogger
{
    public class AnonymousPipeWriter : PipeWriter
    {
        public string Handle { get; }

        public AnonymousPipeWriter(string pipeHandleAsString)
            : base(new AnonymousPipeClientStream(PipeDirection.Out, pipeHandleAsString))
        {
            Handle = pipeHandleAsString;
        }
    }
}
