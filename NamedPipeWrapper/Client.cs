using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using NamedPipeWrapper.IO;
using NamedPipeWrapper.Threading;

namespace NamedPipeWrapper
{
    public class Client<T> where T : class
    {
        private readonly string _pipeName;
        private Connection<T> _connection;

        public event ConnectionMessageEventHandler<T> ServerMessage;
        public event PipeExceptionEventHandler Error;

        public Client(string pipeName)
        {
            _pipeName = pipeName;
        }

        public void Start()
        {
            var worker = new Worker();
            worker.Error += OnError;
            worker.DoWork(ListenSync);
        }

        /// <summary>
        ///     Sends a message to the server over a named pipe.
        /// </summary>
        /// <param name="message">Message to send to the server.</param>
        public void PushMessage(T message)
        {
            if (_connection != null)
                _connection.PushMessage(message);
        }

        public void Stop()
        {
            if (_connection != null)
                _connection.Close();
        }

        #region Private methods

        private void ListenSync()
        {
            // Get the name of the data pipe that should be used from now on by this Client
            var handshake = PipeClientFactory.Connect<string>(_pipeName);
            var dataPipeName = handshake.ReadObject();
            handshake.Close();

            // Connect to the actual data pipe
            var dataPipe = PipeClientFactory.CreateAndConnectPipe(dataPipeName);

            // Create a Connection object for the data pipe
            _connection = ConnectionFactory.CreateConnection<T>(dataPipe);
            _connection.ReceiveMessage += ClientOnReceiveMessage;
            _connection.Open();
        }

        private void ClientOnReceiveMessage(Connection<T> connection, T message)
        {
            if (ServerMessage != null)
                ServerMessage(connection, message);
        }

        /// <summary>
        ///     Invoked on the UI thread.
        /// </summary>
        /// <param name="exception"></param>
        private void OnError(Exception exception)
        {
            if (Error != null)
                Error(exception);
        }

        #endregion
    }

    static class PipeClientFactory
    {
        public static PipeStreamWrapper<T> Connect<T>(string pipeName)
        {
            return new PipeStreamWrapper<T>(CreateAndConnectPipe(pipeName));
        }

        public static NamedPipeClientStream CreateAndConnectPipe(string pipeName)
        {
            var pipe = CreatePipe(pipeName);
            pipe.Connect();
            return pipe;
        }

        private static NamedPipeClientStream CreatePipe(string pipeName)
        {
            return new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.WriteThrough);
        }
    }
}
