using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;

namespace NamedPipeTest
{
    public class UpdateServer : IDisposable
    {
        public const string PIPE_NAME = "bdhero_test_pipe";

        public event ConnectionEventHandler Connection;
        public event ConnectionEventHandler Disconnection;
        public event ServerMessageEventHandler ClientMessage;

        private readonly List<UpdateServerClient> _clients = new List<UpdateServerClient>();

        public UpdateServer()
        {
            ThreadPool.QueueUserWorkItem(ListenAsync);
        }

        private void ListenAsync(object state)
        {
            Listen(PIPE_NAME);
        }

        #region IDisposable

        ~UpdateServer()
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
                var queue = new Queue<UpdateServerClient>(_clients);
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
            //
            while (true)
            {
                WaitForConnection(pipeName);
            }
        }

        private void WaitForConnection(string pipeName)
        {
            NamedPipeServerStream server = null;
            UpdateServerClient updateServerClient = null;

            try
            {
                server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.WriteThrough);
                server.WaitForConnection();

                updateServerClient = UpdateServerClient.CreateClient(server);
                updateServerClient.ReceiveMessage += ClientOnReceiveMessage;

                _clients.Add(updateServerClient);

                if (Connection != null)
                    Connection(updateServerClient);
            }
            // Catch the IOException that is raised if the pipe is broken or disconnected.
            catch (Exception e)
            {
                Console.Error.WriteLine("Named pipe is broken or disconnected: {0}", e);

                if (server == null) return;

                using (var ps2 = server)
                {
                    ps2.Close();
                }

                if (Disconnection != null)
                    Disconnection(updateServerClient);
            }
        }

        private void ClientOnReceiveMessage(UpdateServerClient updateServerClient, string message)
        {
            if (ClientMessage != null)
                ClientMessage(updateServerClient, message);
        }

        public void PushMessage(string message)
        {
            foreach (var client in _clients)
            {
                client.PushMessage(message);
            }
        }
    }

    public delegate void ConnectionEventHandler(UpdateServerClient updateServerClient);
}
