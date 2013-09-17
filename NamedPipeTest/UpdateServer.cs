using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;

namespace NamedPipeTest
{
    public class UpdateServer
    {
        public const string PIPE_NAME = "bdhero_test_pipe";

        public event ServerConnectionEventHandler ClientConnected;
        public event ServerConnectionEventHandler ClientDisconnected;
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
                updateServerClient.Disconnected += ClientOnDisconnected;

                _clients.Add(updateServerClient);

                if (ClientConnected != null)
                    ClientConnected(updateServerClient);
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

                if (ClientDisconnected != null)
                    ClientDisconnected(updateServerClient);
            }
        }

        private void ClientOnReceiveMessage(UpdateServerClient updateServerClient, string message)
        {
            if (ClientMessage != null)
                ClientMessage(updateServerClient, message);
        }

        private void ClientOnDisconnected(UpdateServerClient updateServerClient)
        {
            if (ClientDisconnected != null)
                ClientDisconnected(updateServerClient);
        }

        public void PushMessage(string message)
        {
            foreach (var client in _clients)
            {
                client.PushMessage(message);
            }
        }
    }

    public delegate void ServerConnectionEventHandler(UpdateServerClient updateServerClient);
}
