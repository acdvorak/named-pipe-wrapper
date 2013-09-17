using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;

namespace NamedPipeTest
{
    public class PipeStreamWrapper<T>
    {
        public PipeStream BaseStream { get; private set; }

        /// <summary>
        ///     Gets or sets a value indicating whether the <see cref="BaseStream"/> object is connected.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if the <see cref="BaseStream"/> object is connected; otherwise, <c>false</c>.
        /// </returns>
        public bool IsConnected
        {
            get { return BaseStream.IsConnected; }
        }

        private readonly PipeStreamReader<T> _reader;
        private readonly PipeStreamWriter<T> _writer;

        public PipeStreamWrapper(PipeStream stream)
        {
            BaseStream = stream;
            _reader = new PipeStreamReader<T>(BaseStream);
            _writer = new PipeStreamWriter<T>(BaseStream);
        }

        public T ReadObject()
        {
            return _reader.ReadObject();
        }

        public void WriteObject(T obj)
        {
            _writer.WriteObject(obj);
        }

        /// <summary>
        ///     Waits for the other end of the pipe to read all sent bytes.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The pipe is closed.</exception>
        /// <exception cref="NotSupportedException">The pipe does not support write operations.</exception>
        /// <exception cref="IOException">The pipe is broken or another I/O error occurred.</exception>
        public void WaitForPipeDrain()
        {
            _writer.WaitForPipeDrain();
        }
    }
}
