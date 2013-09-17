using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;

namespace NamedPipeTest
{
    public class UpdateServer<T>
    {
        public const string PIPE_NAME = "bdhero_test_pipe";

        public event ConnectionEventHandler<T> ClientConnected;
        public event ConnectionEventHandler<T> ClientDisconnected;
        public event ConnectionMessageEventHandler<T> ClientMessage;

        private readonly List<Connection<T>> _clients = new List<Connection<T>>();

        private int _nextPipeId;

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
            Connection<T> updateServerClient = null;

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

                updateServerClient = Connection<T>.CreateConnection(instance);
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

        private void ClientOnReceiveMessage(Connection<T> updateServerClient, T message)
        {
            if (ClientMessage != null)
                ClientMessage(updateServerClient, message);
        }

        private void ClientOnDisconnected(Connection<T> updateServerClient)
        {
            if (ClientDisconnected != null)
                ClientDisconnected(updateServerClient);
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
