using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;

namespace NamedPipeTest
{
    public class Connection
    {
        public readonly int Id;
        public readonly string Name;

        public event ConnectionMessageEventHandler ReceiveMessage;
        public event ConnectionEventHandler Disconnected;

        private readonly PipeStreamWrapper<string> _streamWrapper;

        private readonly AutoResetEvent _writeSignal = new AutoResetEvent(false);
        private readonly Queue<string> _writeQueue = new Queue<string>();

        private Connection(int id, string name, PipeStream serverStream)
        {
            Id = id;
            Name = name;

            _streamWrapper = new PipeStreamWrapper<string>(serverStream);

            Init();
        }

        private void Init()
        {
            ThreadPool.QueueUserWorkItem(ReadPipe, null);
            ThreadPool.QueueUserWorkItem(WritePipe, null);
        }

        private void OnDisconnected()
        {
            if (Disconnected != null)
                Disconnected(this);
        }

        private void ReadPipe(object state)
        {
            while (_streamWrapper.IsConnected && _streamWrapper.CanRead)
            {
                var str = _streamWrapper.ReadObject();
                if (str == null)
                {
                    Close();
                    OnDisconnected();
                    return;
                }
                if (ReceiveMessage != null)
                    ReceiveMessage(this, str);
            }
        }

        private void WritePipe(object state)
        {
            while (_streamWrapper.IsConnected && _streamWrapper.CanWrite)
            {
                _writeSignal.WaitOne();
                while (_writeQueue.Count > 0)
                {
                    _streamWrapper.WriteObject(_writeQueue.Dequeue());
                    _streamWrapper.WaitForPipeDrain();
                }
            }
        }

        public void PushMessage(string message)
        {
            _writeQueue.Enqueue(message);
            _writeSignal.Set();
        }

        public void Close()
        {
            _streamWrapper.Close();
            _writeSignal.Set();
        }

        #region Factory

        private static int _lastId;

        public static Connection CreateConnection(PipeStream pipeStream)
        {
            return new Connection(++_lastId, "Client " + _lastId, pipeStream);
        }

        #endregion
    }

    public delegate void ConnectionEventHandler(Connection connection);
    public delegate void ConnectionMessageEventHandler(Connection connection, string message);
}
