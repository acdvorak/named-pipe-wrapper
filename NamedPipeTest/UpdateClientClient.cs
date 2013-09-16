using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace NamedPipeTest
{
    public class UpdateClientClient : IDisposable
    {
        public readonly int Id;
        public readonly string Name;

        public event ClientMessageEventHandler ReceiveMessage;

        private readonly NamedPipeClientStream _clientStream;

        private readonly AutoResetEvent _writeSignal = new AutoResetEvent(false);
        private readonly Queue<string> _writeQueue = new Queue<string>();

        private UpdateClientClient(int id, string name, NamedPipeClientStream clientStream)
        {
            Id = id;
            Name = name;
            _clientStream = clientStream;

            Init();
        }

        #region IDisposable members

        ~UpdateClientClient()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool freeManagedResources)
        {
            if (freeManagedResources)
            {
                if (_clientStream != null)
                {
                    _clientStream.Dispose();
                }
            }
        }

        #endregion

        private void Init()
        {
            ThreadPool.QueueUserWorkItem(ReadPipe, null);
            ThreadPool.QueueUserWorkItem(WritePipe, null);
        }

        private void ReadPipe(object state)
        {
            const int lensize = sizeof(int);
            var lenbuf = new byte[lensize];
            while (_clientStream.IsConnected)
            {
                _clientStream.Read(lenbuf, 0, 4);
                var len = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(lenbuf, 0));
                if (len == 0)
                    return;
                var data = new byte[len];
                _clientStream.Read(data, 0, len);
                var str = Encoding.UTF8.GetString(data);
                if (ReceiveMessage != null)
                    ReceiveMessage(this, str);
            }
            MessageBox.Show("ReadPipe() - Disconnected");
        }

        private void WritePipe(object state)
        {
            while (_clientStream.IsConnected)
            {
                _writeSignal.WaitOne();
                while (_writeQueue.Count > 0)
                {
                    var str = _writeQueue.Dequeue();
                    var data = Encoding.UTF8.GetBytes(str);
                    var lenbuf = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(data.Length));
                    _clientStream.Write(lenbuf, 0, lenbuf.Length);
                    _clientStream.Write(data, 0, data.Length);
                    _clientStream.Flush();
                }
                _clientStream.WaitForPipeDrain();
            }
            MessageBox.Show("WritePipe() - Disconnected");
        }

        public void PushMessage(string message)
        {
            _writeQueue.Enqueue(message);
            _writeSignal.Set();
        }

        #region Factory

        private static int _lastId;

        public static UpdateClientClient CreateClient(NamedPipeClientStream clientStream)
        {
            return new UpdateClientClient(++_lastId, "Client " + _lastId, clientStream);
        }

        #endregion
    }

    public delegate void ClientMessageEventHandler(UpdateClientClient updateServerClient, string message);
}
