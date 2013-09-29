using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace NamedPipeWrapper.IO
{
    public class PipeStreamWrapper<T> where T : class
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
            get { return BaseStream.IsConnected && _reader.IsConnected; }
        }

        /// <summary>
        ///     Gets a value indicating whether the current stream supports read operations.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if the stream supports read operations; otherwise, <c>false</c>.
        /// </returns>
        public bool CanRead
        {
            get { return BaseStream.CanRead; }
        }

        /// <summary>
        ///     Gets a value indicating whether the current stream supports write operations.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if the stream supports write operations; otherwise, <c>false</c>.
        /// </returns>
        public bool CanWrite
        {
            get { return BaseStream.CanWrite; }
        }

        private readonly PipeStreamReader<T> _reader;
        private readonly PipeStreamWriter<T> _writer;

        public PipeStreamWrapper(PipeStream stream)
        {
            BaseStream = stream;
            _reader = new PipeStreamReader<T>(BaseStream);
            _writer = new PipeStreamWriter<T>(BaseStream);
        }

        /// <exception cref="SerializationException">An object in the graph of type parameter <typeparamref name="T"/> is not marked as serializable.</exception>
        public T ReadObject()
        {
            return _reader.ReadObject();
        }

        /// <exception cref="SerializationException">An object in the graph of type parameter <typeparamref name="T"/> is not marked as serializable.</exception>
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

        /// <summary>
        ///     Closes the current stream and releases any resources (such as sockets and file handles) associated with the current stream.
        /// </summary>
        public void Close()
        {
            BaseStream.Close();
        }
    }
}
