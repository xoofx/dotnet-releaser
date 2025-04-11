using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;

namespace MsBuildPipeLogger
{
    public abstract class PipeWriter : IPipeWriter
    {
        private readonly BlockingCollection<BuildEventArgs> _queue =
            new BlockingCollection<BuildEventArgs>(new ConcurrentQueue<BuildEventArgs>());

        private readonly AutoResetEvent _doneProcessing = new AutoResetEvent(false);

        private readonly PipeStream _pipeStream;
        private readonly BinaryWriter _binaryWriter;
        private readonly BuildEventArgsWriter _argsWriter;

        // Buffer writes through a memory stream since the args writer does a bunch of small writes
        private readonly MemoryStream _memoryStream = new MemoryStream();

        protected PipeWriter(PipeStream pipeStream)
        {
            _pipeStream = pipeStream ?? throw new ArgumentNullException(nameof(pipeStream));
            _binaryWriter = new BinaryWriter(_memoryStream);
            _argsWriter = new BuildEventArgsWriter(_binaryWriter);

            Thread writerThread = new Thread(() =>
            {
                BuildEventArgs eventArgs;
                while ((eventArgs = TakeEventArgs()) != null)
                {
                    // Reset the memory stream (but reuse the memory)
                    _memoryStream.Seek(0, SeekOrigin.Begin);
                    _memoryStream.SetLength(0);

                    // Buffer to the memory stream
                    _argsWriter.Write(eventArgs);
                    _binaryWriter.Flush();

                    // ...then write that to the pipe
                    _memoryStream.WriteTo(_pipeStream);
                    _pipeStream.Flush();
                }
                _doneProcessing.Set();
            })
            {
                IsBackground = true,
            };
            writerThread.Start();
        }

        private BuildEventArgs TakeEventArgs()
        {
            if (!_queue.IsCompleted)
            {
                try
                {
                    return _queue.Take();
                }
                catch (InvalidOperationException)
                {
                }
            }
            return null;
        }

        public void Dispose()
        {
            if (!_queue.IsAddingCompleted)
            {
                try
                {
                    _queue.CompleteAdding();
                    _doneProcessing.WaitOne();
                    if (IsWindows)
                    {
                        _pipeStream.WaitForPipeDrain();
                    }
                    _pipeStream.Dispose();
                }
                catch
                {
                }
            }
        }

        public void Write(BuildEventArgs e) => _queue.Add(e);

        private static readonly bool IsWindows = Environment.OSVersion.Platform == PlatformID.Win32NT ||
                                                 Environment.OSVersion.Platform == PlatformID.Win32S ||
                                                 Environment.OSVersion.Platform == PlatformID.Win32Windows ||
                                                 Environment.OSVersion.Platform == PlatformID.WinCE ||
                                                 Environment.OSVersion.Platform == PlatformID.Xbox;
    }
}
