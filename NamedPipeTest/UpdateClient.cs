using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;

namespace NamedPipeTest
{
    public class UpdateClient<T> where T : class
    {
        private readonly List<Connection<T>> _clients = new List<Connection<T>>();

        public event ConnectionMessageEventHandler<T> ServerMessage;

        public UpdateClient()
        {
            ThreadPool.QueueUserWorkItem(ListenAsync);
        }

        private void ListenAsync(object state)
        {
            Listen(UpdateServer<T>.PIPE_NAME);
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

            var client = Connection<T>.CreateConnection(instance);
            client.ReceiveMessage += ClientOnReceiveMessage;
            _clients.Add(client);
        }

        private static NamedPipeClientStream CreatePipe(string pipeName)
        {
            return new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.WriteThrough);
        }

        private void ClientOnReceiveMessage(Connection<T> updateServerClient, T message)
        {
            if (ServerMessage != null)
                ServerMessage(updateServerClient, message);
        }

        public void PushMessage(T message)
        {
            foreach (var client in _clients)
            {
                client.PushMessage(message);
            }
        }
    }
}
