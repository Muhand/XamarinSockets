using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;

namespace XamarinSockets
{
    public sealed class TcpSocketListener
    {
        #region CONSTANTS
        private const int DEFAULT_MAX_NUMBER_OF_PENDING_CONNECTIONS = 100;
        #endregion

        #region Properties
        /// <summary>
        /// Check if the server is running or not
        /// </summary>
        public bool Running { get; private set; }

        /// <summary>
        /// The port is the server using
        /// </summary>
        public int Port { get; private set; }

        /// <summary>
        /// How many clients can be put on queue? 
        /// If max number is reached then the other connections will be refused until there is a space in the queue
        /// </summary>
        public int MaximumNumberOfPendingConnections { get; set; }
        #endregion

        #region Events

        public event EventHandler<AcceptedTcpSocketEventArgs> AcceptedConnection;
        public event EventHandler<TcpServerStarted> ServerStarted;
        public event EventHandler<TcpServerStopped> ServerStopped;
        public event EventHandler<ServerStatusChanged> ServerStatusChanged;
        public event EventHandler<TcpServerFailedToStart> ServerFailedToStart;
        public event EventHandler<TcpServerFailedToStop> ServerFailedToStop;

        #endregion

        #region Global Variables
        private Socket listener;
        #endregion

        #region Constructor(s)
        /// <summary>
        /// Initialize a new listener
        /// </summary>
        /// <param name="port">On which port should this listener work?</param>
        public TcpSocketListener(int port)
        {
            this.Port = port;
            MaximumNumberOfPendingConnections = DEFAULT_MAX_NUMBER_OF_PENDING_CONNECTIONS;
        }

        /// <summary>
        /// Initialize a new listener
        /// </summary>
        /// <param name="port">On which port should this listener work?</param>
        /// <param name="MaximumNumberOfPendingConnections">How many clients can be put on queue?
        /// If max number is reached then the other connections will be refused until there is a space in the queue
        /// </param>
        public TcpSocketListener(int port, int MaximumNumberOfPendingConnections)
        {
            this.Port = port;
            this.MaximumNumberOfPendingConnections = MaximumNumberOfPendingConnections;
        }

        #endregion

        #region Logic
        public void Start()
        {
            //If the server is not running then start it and start listening for incoming connections
            if (!Running)
            {
                this.listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                this.listener.Bind(new IPEndPoint(IPAddress.Any, this.Port));
                this.listener.Listen(this.MaximumNumberOfPendingConnections);
                this.listener.BeginAccept(acceptCallBack,null);
                this.Running = true;
                ServerStarted?.Invoke(this, new TcpServerStarted());
                ServerStatusChanged?.Invoke(this, new ServerStatusChanged(ServerStatus.Started));
            }
            else
            {
                //throw new InvalidOperationException("Server is already running");
                ServerFailedToStart?.Invoke(this, new TcpServerFailedToStart(XamarinSockets.ServerFailedToStart.ServerIsRunning));

            }
        }

        public void Stop()
        {
            //If the server is runnning then clear and stop the server
            if (Running)
            {
                this.listener.Close();
                this.listener = null;
                this.Running = false;
                ServerStopped?.Invoke(this, new TcpServerStopped());
                ServerStatusChanged?.Invoke(this, new ServerStatusChanged(ServerStatus.Stopped));
            }
            else
            {
                //throw new InvalidOperationException("Server is not running");
                ServerFailedToStop?.Invoke(this, new TcpServerFailedToStop(ServerfailedToStop.ServerIsNotRunning));
            }
        }

        #endregion

        #region CallBacks
        private void acceptCallBack(IAsyncResult ar)
        {
            try
            {
                //Get the accepted client
                var accepted = this.listener.EndAccept(ar);

                this.listener.BeginAccept(acceptCallBack,null);
                
                //Call the event that there is a new client have been accepted
                AcceptedConnection?.Invoke(this, new AcceptedTcpSocketEventArgs(accepted));             //If AccaptedConnection is not null then invoke the method

            }
            catch (Exception ex)
            {
                //If EndAccept fails then throw this acception, in case they stopped the server then the program shoulding crash
                Debug.Print(ex.StackTrace);
            }
        }
        #endregion
    }
}
