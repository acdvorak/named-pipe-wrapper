using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;

namespace NamedPipeTest
{
    public class UpdateClient
    {
        private readonly List<Connection> _clients = new List<Connection>();

        public event ConnectionMessageEventHandler ServerMessage;

        public UpdateClient()
        {
            ThreadPool.QueueUserWorkItem(ListenAsync);
        }

        private void ListenAsync(object state)
        {
            Listen(UpdateServer.PIPE_NAME);
        }

        private void Listen(string pipeName)
        {
            NamedPipeClientStream @default = CreatePipe(pipeName);

            // Connect to the pipe or wait until the pipe is available.
            @default.Connect();

            var defaultWrapper = new PipeStreamWrapper<string>(@default);
            var instancePipeName = defaultWrapper.ReadObject();
            defaultWrapper.Close();

            var instance = CreatePipe(instancePipeName);
            instance.Connect();

            var client = Connection.CreateConnection(instance);
            client.ReceiveMessage += ClientOnReceiveMessage;
            _clients.Add(client);
        }

        private static NamedPipeClientStream CreatePipe(string pipeName)
        {
            return new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.WriteThrough);
        }

        private void ClientOnReceiveMessage(Connection updateServerClient, string message)
        {
            if (ServerMessage != null)
                ServerMessage(updateServerClient, message);
        }

        public void PushMessage(string message)
        {
            foreach (var client in _clients)
            {
                client.PushMessage(message);
            }
        }
    }

    public delegate void ClientConnectionEventHandler(Connection updateClientClient);
}
