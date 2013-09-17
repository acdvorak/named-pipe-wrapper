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

        private int _nextPipeId = 0;

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
            NamedPipeServerStream instance = null;
            UpdateServerClient updateServerClient = null;

            try
            {
                server = CreateServer(pipeName);
                server.WaitForConnection();

                var instancePipeName = string.Format("{0}_{1}", pipeName, ++_nextPipeId);

                var serverWrapper = new PipeStreamWrapper<string>(server);
                serverWrapper.WriteObject(instancePipeName);
                serverWrapper.WaitForPipeDrain();
                serverWrapper.Close();

                instance = CreateServer(instancePipeName);
                instance.WaitForConnection();

                updateServerClient = UpdateServerClient.CreateClient(instance);
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

                if (server != null)
                {
                    using (var ps2 = server)
                    {
                        ps2.Close();
                    }
                }

                if (instance != null)
                {
                    using (var ps2 = instance)
                    {
                        ps2.Close();
                    }
                }

                if (ClientDisconnected != null)
                    ClientDisconnected(updateServerClient);
            }
        }

        private static NamedPipeServerStream CreateServer(string pipeName)
        {
            return new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.WriteThrough);
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
