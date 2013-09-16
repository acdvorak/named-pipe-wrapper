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
        private readonly List<UpdateClientClient> _clients = new List<UpdateClientClient>();

        public event ClientMessageEventHandler ServerMessage;

        public UpdateClient()
        {
            ThreadPool.QueueUserWorkItem(ListenAsync);
        }

        private void ListenAsync(object state)
        {
            Listen(UpdateServer.PIPE_NAME);
        }

        #region IDisposable

        ~UpdateClient()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected void Dispose(bool freeManagedObjectsToo)
        {
            if (freeManagedObjectsToo)
            {
                var queue = new Queue<UpdateClientClient>(_clients);
                foreach (var client in queue)
                {
                    client.Dispose();
                }
                _clients.Clear();
            }
        }

        #endregion

        private void Listen(string pipeName)
        {
            NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.WriteThrough);

            // Connect to the pipe or wait until the pipe is available.
            Console.Write("Attempting to connect to pipe...");
            pipeClient.Connect();

            Console.WriteLine("Connected to pipe.");
            Console.WriteLine("There are currently {0} pipe server instances open.", pipeClient.NumberOfServerInstances);

            var client = UpdateClientClient.CreateClient(pipeClient);
            client.ReceiveMessage += ClientOnReceiveMessage;
            _clients.Add(client);
        }

        private void ClientOnReceiveMessage(UpdateClientClient updateServerClient, string message)
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
}
