using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using NamedPipeWrapper.IO;
using NamedPipeWrapper.Threading;

namespace NamedPipeWrapper
{
    public class Connection<T> where T : class
    {
        public readonly int Id;
        public readonly string Name;

        public bool IsConnected { get { return _streamWrapper.IsConnected; } }

        public event ConnectionMessageEventHandler<T> ReceiveMessage;
        public event ConnectionEventHandler<T> Disconnected;
        public event ConnectionExceptionEventHandler<T> Error;

        private readonly PipeStreamWrapper<T> _streamWrapper;

        private readonly AutoResetEvent _writeSignal = new AutoResetEvent(false);
        private readonly Queue<T> _writeQueue = new Queue<T>();

        private bool _notifiedSucceeded;

        internal Connection(int id, string name, PipeStream serverStream)
        {
            Id = id;
            Name = name;
            _streamWrapper = new PipeStreamWrapper<T>(serverStream);
        }

        public void Open()
        {
            var readWorker = new Worker();
            readWorker.Succeeded += OnSucceeded;
            readWorker.Error += OnError;
            readWorker.DoWork(ReadPipe);

            var writeWorker = new Worker();
            writeWorker.Succeeded += OnSucceeded;
            writeWorker.Error += OnError;
            writeWorker.DoWork(WritePipe);
        }

        public void PushMessage(T message)
        {
            _writeQueue.Enqueue(message);
            _writeSignal.Set();
        }

        public void Close()
        {
            CloseImpl();
        }

        /// <summary>
        ///     Invoked on the background thread.
        /// </summary>
        private void CloseImpl()
        {
            _streamWrapper.Close();
            _writeSignal.Set();
        }

        /// <summary>
        ///     Invoked on the UI thread.
        /// </summary>
        private void OnSucceeded()
        {
            // Only notify observers once
            if (_notifiedSucceeded)
                return;

            _notifiedSucceeded = true;

            if (Disconnected != null)
                Disconnected(this);
        }

        /// <summary>
        ///     Invoked on the UI thread.
        /// </summary>
        /// <param name="exception"></param>
        private void OnError(Exception exception)
        {
            if (Error != null)
                Error(this, exception);
        }

        /// <summary>
        ///     Invoked on the background thread.
        /// </summary>
        private void ReadPipe()
        {
            while (_streamWrapper.IsConnected && _streamWrapper.CanRead)
            {
                var obj = _streamWrapper.ReadObject();
                if (obj == null)
                {
                    CloseImpl();
                    return;
                }
                if (ReceiveMessage != null)
                    ReceiveMessage(this, obj);
            }
        }

        /// <summary>
        ///     Invoked on the background thread.
        /// </summary>
        private void WritePipe()
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
    }

    static class ConnectionFactory
    {
        private static int _lastId;

        public static Connection<T> CreateConnection<T>(PipeStream pipeStream) where T : class
        {
            return new Connection<T>(++_lastId, "Client " + _lastId, pipeStream);
        }
    }

    public delegate void ConnectionEventHandler<T>(Connection<T> connection) where T : class;
    public delegate void ConnectionMessageEventHandler<T>(Connection<T> connection, T message) where T : class;
    public delegate void ConnectionExceptionEventHandler<T>(Connection<T> connection, Exception exception) where T : class;
}
