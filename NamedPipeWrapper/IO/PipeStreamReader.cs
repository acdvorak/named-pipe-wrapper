using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;

namespace NamedPipeWrapper.IO
{
    public class PipeStreamReader<T>
    {
        public PipeStream BaseStream { get; private set; }

        private readonly BinaryFormatter _binaryFormatter = new BinaryFormatter();

        public PipeStreamReader(PipeStream stream)
        {
            BaseStream = stream;
        }

        #region Private stream readers

        private int ReadLength()
        {
            const int lensize = sizeof (int);
            var lenbuf = new byte[lensize];
            BaseStream.Read(lenbuf, 0, lensize);
            return IPAddress.NetworkToHostOrder(BitConverter.ToInt32(lenbuf, 0));
        }

        private T ReadObject(int len)
        {
            var data = new byte[len];
            BaseStream.Read(data, 0, len);
            using (var memoryStream = new MemoryStream(data))
            {
                return (T) _binaryFormatter.Deserialize(memoryStream);
            }
        }

        #endregion

        public T ReadObject()
        {
            var len = ReadLength();
            return len == 0 ? default(T) : ReadObject(len);
        }
    }
}
