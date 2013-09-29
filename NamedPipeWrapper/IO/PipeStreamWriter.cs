using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace NamedPipeWrapper.IO
{
    public class PipeStreamWriter<T>
    {
        public PipeStream BaseStream { get; private set; }

        private readonly BinaryFormatter _binaryFormatter = new BinaryFormatter();

        public PipeStreamWriter(PipeStream stream)
        {
            BaseStream = stream;
        }

        #region Private stream writers

        /// <exception cref="SerializationException">An object in the graph of type parameter <typeparamref name="T"/> is not marked as serializable.</exception>
        private byte[] Serialize(T obj)
        {
            using (var memoryStream = new MemoryStream())
            {
                _binaryFormatter.Serialize(memoryStream, obj);
                return memoryStream.ToArray();
            }
        }

        private void WriteLength(int len)
        {
            var lenbuf = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(len));
            BaseStream.Write(lenbuf, 0, lenbuf.Length);
        }

        private void WriteObject(byte[] data)
        {
            BaseStream.Write(data.ToArray(), 0, data.Length);
        }

        private void Flush()
        {
            BaseStream.Flush();
        }

        #endregion

        /// <exception cref="SerializationException">An object in the graph of type parameter <typeparamref name="T"/> is not marked as serializable.</exception>
        public void WriteObject(T obj)
        {
            var data = Serialize(obj);
            WriteLength(data.Length);
            WriteObject(data);
            Flush();
        }

        /// <summary>
        ///     Waits for the other end of the pipe to read all sent bytes.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The pipe is closed.</exception>
        /// <exception cref="NotSupportedException">The pipe does not support write operations.</exception>
        /// <exception cref="IOException">The pipe is broken or another I/O error occurred.</exception>
        public void WaitForPipeDrain()
        {
            BaseStream.WaitForPipeDrain();
        }
    }
}