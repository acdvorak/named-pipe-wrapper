﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using NamedPipeWrapper.IO;
using NamedPipeWrapper.Threading;

namespace NamedPipeWrapper
{
    /// <summary>
    /// Wraps a <see cref="NamedPipeClientStream"/>.
    /// </summary>
    /// <typeparam name="TReadWrite">Reference type to read from and write to the named pipe</typeparam>
    public class NamedPipeClient<TReadWrite> : NamedPipeClient<TReadWrite, TReadWrite> where TReadWrite : class
    {
        /// <summary>
        /// Constructs a new <c>NamedPipeClient</c> to connect to the <see cref="NamedPipeNamedPipeServer{TReadWrite}"/> specified by <paramref name="pipeName"/>.
        /// </summary>
        /// <param name="pipeName">Name of the server's pipe</param>
        public NamedPipeClient(string pipeName) : base(pipeName)
        {
        }
    }

    /// <summary>
    /// Wraps a <see cref="NamedPipeClientStream"/>.
    /// </summary>
    /// <typeparam name="TRead">Reference type to read from the named pipe</typeparam>
    /// <typeparam name="TWrite">Reference type to write to the named pipe</typeparam>
    public class NamedPipeClient<TRead, TWrite>
        where TRead : class
        where TWrite : class
    {
        /// <summary>
        /// Gets or sets whether the client should attempt to reconnect when the pipe breaks
        /// due to an error or the other end terminating the connection.
        /// Default value is <c>true</c>.
        /// </summary>
        public bool AutoReconnect { get; set; }

        /// <summary>
        /// Gets or sets how long the client waits between a reconnection attempt.
        /// Default value is <c>0</c>.
        /// </summary>
        public int AutoReconnectDelay { get; set; }



        /// <summary>
        /// Invoked whenever a message is received from the server.
        /// </summary>
        public event ConnectionMessageEventHandler<TRead, TWrite> ServerMessage;

        /// <summary>
        /// Invoked when the client disconnects from the server (e.g., the pipe is closed or broken).
        /// </summary>
        public event ConnectionEventHandler<TRead, TWrite> Disconnected;

        /// <summary>
        /// Invoked whenever an exception is thrown during a read or write operation on the named pipe.
        /// </summary>
        public event PipeExceptionEventHandler Error;

        private readonly string _pipeName;
        private NamedPipeConnection<TRead, TWrite> _connection;

        private readonly AutoResetEvent _connected = new AutoResetEvent(false);
        private readonly AutoResetEvent _disconnected = new AutoResetEvent(false);

        private volatile bool _closedExplicitly;

        /// <summary>
        /// Constructs a new <c>NamedPipeClient</c> to connect to the <see cref="NamedPipeServer{TRead, TWrite}"/> specified by <paramref name="pipeName"/>.
        /// </summary>
        /// <param name="pipeName">Name of the server's pipe</param>
        public NamedPipeClient(string pipeName)
        {
            _pipeName = pipeName;
            AutoReconnect = true;
        }

        /// <summary>
        /// Connects to the named pipe server asynchronously.
        /// This method returns immediately, possibly before the connection has been established.
        /// </summary>
        public void Start(bool waitForconnection = false)
        {
            _closedExplicitly = false;
            var worker = new Worker();
            worker.Error += OnError;
            worker.DoWork(ListenSync);
        }

        /// <summary>
        ///     Sends a message to the server over a named pipe.
        /// </summary>
        /// <param name="message">Message to send to the server.</param>
        public void PushMessage(TWrite message)
        {
            if (_connection != null)
                _connection.PushMessage(message);
        }

        /// <summary>
        /// Closes the named pipe.
        /// </summary>
        public void Stop()
        {
            _closedExplicitly = true;
            if (_connection != null)
                _connection.Close();
        }

        #region Wait for connection/disconnection

        public void WaitForConnection()
        {
            _connected.WaitOne();
        }

        public void WaitForConnection(int millisecondsTimeout)
        {
            _connected.WaitOne(millisecondsTimeout);
        }

        public void WaitForConnection(TimeSpan timeout)
        {
            _connected.WaitOne(timeout);
        }

        public void WaitForDisconnection()
        {
            _disconnected.WaitOne();
        }

        public void WaitForDisconnection(int millisecondsTimeout)
        {
            _disconnected.WaitOne(millisecondsTimeout);
        }

        public void WaitForDisconnection(TimeSpan timeout)
        {
            _disconnected.WaitOne(timeout);
        }

        #endregion

        #region Private methods



        private void ListenSync()
        {
            // Get the name of the data pipe that should be used from now on by this NamedPipeClient
            var handshake = PipeClientFactory.Connect<string, string>(_pipeName);
            var dataPipeName = handshake.ReadObject();
            handshake.Close();

            // Connect to the actual data pipe
            var dataPipe = PipeClientFactory.CreateAndConnectPipe(dataPipeName);

            // Create a Connection object for the data pipe
            _connection = ConnectionFactory.CreateConnection<TRead, TWrite>(dataPipe);
            _connection.Disconnected += OnDisconnected;
            _connection.ReceiveMessage += OnReceiveMessage;
            _connection.Error += ConnectionOnError;
            _connection.Open();

            _connected.Set();
        }

        private void OnDisconnected(NamedPipeConnection<TRead, TWrite> connection)
        {
            if (Disconnected != null)
                Disconnected(connection);

            _disconnected.Set();

            // Reconnect
            if (AutoReconnect && !_closedExplicitly)
            {
                Thread.Sleep(AutoReconnectDelay);
                Start();
            }
        }

        private void OnReceiveMessage(NamedPipeConnection<TRead, TWrite> connection, TRead message)
        {
            if (ServerMessage != null)
                ServerMessage(connection, message);
        }

        /// <summary>
        ///     Invoked on the UI thread.
        /// </summary>
        private void ConnectionOnError(NamedPipeConnection<TRead, TWrite> connection, Exception exception)
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

        #endregion
    }

    static class PipeClientFactory
    {
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool WaitNamedPipe(string name, int timeout);
        
        public static bool NamedPipeExists(string pipeName)
        {
            try
            {
                bool exists = WaitNamedPipe(pipeName, -1);
                if (!exists)
                {
                    int error = Marshal.GetLastWin32Error();
                    if (error == 0 || error == 2)
                    {
                        return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static PipeStreamWrapper<TRead, TWrite> Connect<TRead, TWrite>(string pipeName)
            where TRead : class
            where TWrite : class
        {
            return new PipeStreamWrapper<TRead, TWrite>(CreateAndConnectPipe(pipeName));
        }

        public static NamedPipeClientStream CreateAndConnectPipe(string pipeName, int timeout = 10)
        {
            string normalizedPath = Path.GetFullPath(string.Format(@"\\.\pipe\{0}", pipeName));
            while (!NamedPipeExists(normalizedPath))
            {
                Thread.Sleep(timeout);
            }
            var pipe = CreatePipe(pipeName);
            pipe.Connect(1000);
            return pipe;
        }

        private static NamedPipeClientStream CreatePipe(string pipeName)
        {
            return new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.WriteThrough);
        }
    }
}
