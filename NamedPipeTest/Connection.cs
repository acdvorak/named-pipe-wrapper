using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;

namespace NamedPipeTest
{
    public class Connection<T> where T : class
    {
        public readonly int Id;
        public readonly string Name;

        public event ConnectionMessageEventHandler<T> ReceiveMessage;
        public event ConnectionEventHandler<T> Disconnected;

        private readonly PipeStreamWrapper<T> _streamWrapper;

        private readonly AutoResetEvent _writeSignal = new AutoResetEvent(false);
        private readonly Queue<T> _writeQueue = new Queue<T>();

        private Connection(int id, string name, PipeStream serverStream)
        {
            Id = id;
            Name = name;

            _streamWrapper = new PipeStreamWrapper<T>(serverStream);

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
                var obj = _streamWrapper.ReadObject();
                if (obj == null)
                {
                    Close();
                    OnDisconnected();
                    return;
                }
                if (ReceiveMessage != null)
                    ReceiveMessage(this, obj);
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

        public void PushMessage(T message)
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

        public static Connection<T> CreateConnection(PipeStream pipeStream)
        {
            return new Connection<T>(++_lastId, "Client " + _lastId, pipeStream);
        }

        #endregion
    }

    public delegate void ConnectionEventHandler<T>(Connection<T> connection) where T : class;
    public delegate void ConnectionMessageEventHandler<T>(Connection<T> connection, T message) where T : class;
}
