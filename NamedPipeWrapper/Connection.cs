using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using NamedPipeWrapper.IO;
using NamedPipeWrapper.Threading;

namespace NamedPipeWrapper
{
    /// <summary>
    /// Represents a connection between a named pipe client and server.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Connection<T> where T : class
    {
        /// <summary>
        /// Gets the connection's unique identifier.
        /// </summary>
        public readonly int Id;

        /// <summary>
        /// Gets the connection's name.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// Gets a value indicating whether the pipe is connected or not.
        /// </summary>
        public bool IsConnected { get { return _streamWrapper.IsConnected; } }

        /// <summary>
        /// Invoked whenever a message is received from the other end of the pipe.
        /// </summary>
        public event ConnectionMessageEventHandler<T> ReceiveMessage;

        /// <summary>
        /// Invoked when the named pipe connection terminates.
        /// </summary>
        public event ConnectionEventHandler<T> Disconnected;

        /// <summary>
        /// Invoked when an exception is thrown during any read/write operation over the named pipe.
        /// </summary>
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

        /// <summary>
        /// Begins reading from and writing to the named pipe on a background thread.
        /// This method returns immediately.
        /// </summary>
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

        /// <summary>
        /// Adds the specified <paramref name="message"/> to the write queue.
        /// The message will be written to the named pipe by the background thread
        /// at the next available opportunity.
        /// </summary>
        /// <param name="message"></param>
        public void PushMessage(T message)
        {
            _writeQueue.Enqueue(message);
            _writeSignal.Set();
        }

        /// <summary>
        /// Closes the named pipe connection and underlying <c>PipeStream</c>.
        /// </summary>
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
        /// <exception cref="SerializationException">An object in the graph of type parameter <typeparamref name="T"/> is not marked as serializable.</exception>
        private void ReadPipe()
        {
            while (IsConnected && _streamWrapper.CanRead)
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
        /// <exception cref="SerializationException">An object in the graph of type parameter <typeparamref name="T"/> is not marked as serializable.</exception>
        private void WritePipe()
        {
            while (IsConnected && _streamWrapper.CanWrite)
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

    /// <summary>
    /// Handles new connections.
    /// </summary>
    /// <param name="connection">The newly established connection</param>
    /// <typeparam name="T">Reference type</typeparam>
    public delegate void ConnectionEventHandler<T>(Connection<T> connection) where T : class;

    /// <summary>
    /// Handles messages received from a named pipe.
    /// </summary>
    /// <typeparam name="T">Reference type</typeparam>
    /// <param name="connection">Connection that received the message</param>
    /// <param name="message">Message sent by the other end of the pipe</param>
    public delegate void ConnectionMessageEventHandler<T>(Connection<T> connection, T message) where T : class;

    /// <summary>
    /// Handles exceptions thrown during read/write operations.
    /// </summary>
    /// <typeparam name="T">Reference type</typeparam>
    /// <param name="connection">Connection that threw the exception</param>
    /// <param name="exception">The exception that was thrown</param>
    public delegate void ConnectionExceptionEventHandler<T>(Connection<T> connection, Exception exception) where T : class;
}
