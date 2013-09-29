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
    public class Server<T> where T : class
    {
        public event ConnectionEventHandler<T> ClientConnected;
        public event ConnectionEventHandler<T> ClientDisconnected;
        public event ConnectionMessageEventHandler<T> ClientMessage;
        public event PipeExceptionEventHandler Error;

        private readonly string _pipeName;
        private readonly List<Connection<T>> _connections = new List<Connection<T>>();

        private int _nextPipeId;

        public Server(string pipeName)
        {
            _pipeName = pipeName;
        }

        public void Start()
        {
            var worker = new Worker();
            worker.Error += OnError;
            worker.DoWork(ListenSync);
        }

        public void PushMessage(T message)
        {
            foreach (var client in _connections)
            {
                client.PushMessage(message);
            }
        }

        public void Stop()
        {
            foreach (var client in _connections)
            {
                client.Close();
            }
        }

        #region Private methods

        private void ListenSync()
        {
            //
            while (true)
            {
                WaitForConnection(_pipeName);
            }
        }

        private void WaitForConnection(string pipeName)
        {
            NamedPipeServerStream handshakePipe = null;
            NamedPipeServerStream dataPipe = null;
            Connection<T> connection = null;

            var connectionPipeName = GetNextConnectionPipeName(pipeName);

            try
            {
                // Send the client the name of the data pipe to use
                handshakePipe = PipeServerFactory.CreateAndConnectPipe(pipeName);
                var handshakeWrapper = new PipeStreamWrapper<string>(handshakePipe);
                handshakeWrapper.WriteObject(connectionPipeName);
                handshakeWrapper.WaitForPipeDrain();
                handshakeWrapper.Close();

                // Wait for the client to connect to the data pipe
                dataPipe = PipeServerFactory.CreatePipe(connectionPipeName);
                dataPipe.WaitForConnection();

                // Add the client's connection to the list of connections
                connection = ConnectionFactory.CreateConnection<T>(dataPipe);
                connection.ReceiveMessage += ClientOnReceiveMessage;
                connection.Disconnected += ClientOnDisconnected;
                connection.Error += ConnectionOnError;
                connection.Open();
                _connections.Add(connection);

                ClientOnConnected(connection);
            }
            // Catch the IOException that is raised if the pipe is broken or disconnected.
            catch (Exception e)
            {
                Console.Error.WriteLine("Named pipe is broken or disconnected: {0}", e);

                Cleanup(handshakePipe);
                Cleanup(dataPipe);

                ClientOnDisconnected(connection);
            }
        }

        private void ClientOnConnected(Connection<T> connection)
        {
            if (ClientConnected != null)
                ClientConnected(connection);
        }

        private void ClientOnReceiveMessage(Connection<T> connection, T message)
        {
            if (ClientMessage != null)
                ClientMessage(connection, message);
        }

        private void ClientOnDisconnected(Connection<T> connection)
        {
            if (connection == null)
                return;

            _connections.Remove(connection);

            if (ClientDisconnected != null)
                ClientDisconnected(connection);
        }

        /// <summary>
        ///     Invoked on the UI thread.
        /// </summary>
        private void ConnectionOnError(Connection<T> connection, Exception exception)
        {
            OnError(exception);
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

        private string GetNextConnectionPipeName(string pipeName)
        {
            return string.Format("{0}_{1}", pipeName, ++_nextPipeId);
        }

        private static void Cleanup(NamedPipeServerStream pipe)
        {
            if (pipe == null) return;
            using (var x = pipe)
            {
                x.Close();
            }
        }

        #endregion
    }

    static class PipeServerFactory
    {
        public static NamedPipeServerStream CreateAndConnectPipe(string pipeName)
        {
            var pipe = CreatePipe(pipeName);
            pipe.WaitForConnection();
            return pipe;
        }

        public static NamedPipeServerStream CreatePipe(string pipeName)
        {
            return new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.WriteThrough);
        }
    }
}
