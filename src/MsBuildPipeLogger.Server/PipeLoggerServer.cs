using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;

namespace MsBuildPipeLogger
{
    /// <summary>
    /// Receives MSBuild logging events over a pipe. This is the base class for <see cref="AnonymousPipeLoggerServer"/>
    /// and <see cref="NamedPipeLoggerServer"/>.
    /// </summary>
    public abstract class PipeLoggerServer<TPipeStream> : EventArgsDispatcher, IPipeLoggerServer
        where TPipeStream : PipeStream
    {
        private readonly BinaryReader _binaryReader;
        private readonly BuildEventArgsReaderProxy _buildEventArgsReader;

        internal PipeBuffer Buffer { get; } = new PipeBuffer();

        protected TPipeStream PipeStream { get; }

        protected CancellationToken CancellationToken { get; }

        /// <summary>
        /// Creates a server that receives MSBuild events over a specified pipe.
        /// </summary>
        /// <param name="pipeStream">The pipe to receive events from.</param>
        protected PipeLoggerServer(TPipeStream pipeStream)
            : this(pipeStream, CancellationToken.None)
        {
        }

        /// <summary>
        /// Creates a server that receives MSBuild events over a specified pipe.
        /// </summary>
        /// <param name="pipeStream">The pipe to receive events from.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that will cancel read operations if triggered.</param>
        protected PipeLoggerServer(TPipeStream pipeStream, CancellationToken cancellationToken)
        {
            PipeStream = pipeStream;
            _binaryReader = new BinaryReader(Buffer);
            _buildEventArgsReader = new BuildEventArgsReaderProxy(_binaryReader);
            CancellationToken = cancellationToken;

            Thread readerThread = new Thread(() =>
            {
                try
                {
                    Connect();
                    while (Buffer.FillFromStream(PipeStream, CancellationToken))
                    {
                    }
                }
                catch (IOException)
                {
                    // The client broke the stream so we're done
                }
                catch (ObjectDisposedException)
                {
                    // The pipe was disposed
                }

                // Add a final 0 (BinaryLogRecordKind.EndOfFile) into the stream in case the BuildEventArgsReader is waiting for a read
                Buffer.Write(new byte[1] { 0 }, 0, 1);

                Buffer.CompleteAdding();
            })
            {
                IsBackground = true
            };

            readerThread.Start();
        }

        protected abstract void Connect();

        /// <inheritdoc/>
        public BuildEventArgs Read()
        {
            if (Buffer.IsCompleted)
            {
                return null;
            }

            try
            {
                BuildEventArgs args = _buildEventArgsReader.Read();
                if (args != null)
                {
                    Dispatch(args);
                    return args;
                }
            }
            catch (EndOfStreamException)
            {
                // The stream may have been closed or otherwise stopped
            }

            return null;
        }

        /// <inheritdoc/>
        public void ReadAll()
        {
            BuildEventArgs args = Read();
            while (args != null)
            {
                if (args is BuildFinishedEventArgs)
                {
                    return;
                }
                args = Read();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _binaryReader.Dispose();
            Buffer.Dispose();
            PipeStream.Dispose();
        }
    }
}
