using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;

namespace NamedPipeTest
{
    public class Client<T> where T : class
    {
        private Connection<T> _connection;

        public event ConnectionMessageEventHandler<T> ServerMessage;

        public Client()
        {
            ThreadPool.QueueUserWorkItem(ListenAsync);
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

        #region Private methods

        private void ListenAsync(object state)
        {
            ListenSync(Constants.PIPE_NAME);
        }

        private void ListenSync(string pipeName)
        {
            // Get the name of the data pipe that should be used from now on by this Client
            var handshake = PipeClientFactory.Connect<string>(pipeName);
            var dataPipeName = handshake.ReadObject();
            handshake.Close();

            // Connect to the actual data pipe
            var dataPipe = PipeClientFactory.CreateAndConnectPipe(dataPipeName);

            // Create a Connection object for the data pipe
            _connection = ConnectionFactory.CreateConnection<T>(dataPipe);
            _connection.ReceiveMessage += ClientOnReceiveMessage;
        }

        private void ClientOnReceiveMessage(Connection<T> updateServerClient, T message)
        {
            if (ServerMessage != null)
                ServerMessage(updateServerClient, message);
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
