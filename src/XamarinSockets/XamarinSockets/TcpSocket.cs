using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace XamarinSockets
{
    public sealed class TcpSocket : IDisposable
    {
        #region Constants
        //Maximum amount of data we can recieve
        private const int BUFFER_SIZE = 8192;
        //Buffer length (the header before the payload
        private const int SIZE_BUFFER_LENGTH = 4;
        #endregion

        #region Properties

        public bool Connected { get; private set; }

        public bool IsDisposed { get; private set; }

        #endregion

        #region Events

        public event EventHandler<TcpSocketConnectionStateEventArgs> AsynchConnectionResult;
        public event EventHandler<TcpSocketReceivedEventArgs> DataReceived;
        public event EventHandler Disconnected;

        #endregion

        #region Global Variables
        private byte[] buffer;
        private int payloadSize;
        private MemoryStream payloadStream;

        private object sendSync = new object();

        private Socket socket;
        #endregion

        #region Constructor(s)

        /// <summary>
        /// Use this constructor if you are on a client side and you wish to connect to a server
        /// </summary>
        public TcpSocket()
        {
            this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            initBuffer();
        }

        /// <summary>
        /// Use this constructor if you are on a server side and you have already accepted a connection
        /// </summary>
        /// <param name="s">The bare socket</param>
        public TcpSocket(Socket s)
        {
            this.socket = s;
            this.Connected = true;
            initBuffer();
            beginRead();
        }

        #endregion

        #region Deconstructor
        ~TcpSocket()
        {
            Dispose();
        }
        #endregion

        #region Methods
        private void initBuffer()
        {
            this.buffer = new byte[BUFFER_SIZE];
        }

        private void checkDisposed()
        {
            if(this.IsDisposed)
            {
                throw new ObjectDisposedException("this");
            }
        }
        #endregion

        #region Connection Logic

        #region Synchronous Version
        public void Connect(string host, int port)
        {
            checkDisposed();
            this.socket.Connect(host, port);
            OnSyncConnect();
        }

        public void Connect(IPEndPoint endPoint)
        {
            checkDisposed();
            this.socket.Connect(endPoint);
            OnSyncConnect();
        }

        public void Connect(IPAddress ipAddress, int port)
        {
            checkDisposed();
            this.socket.Connect(ipAddress, port);
            OnSyncConnect();
        }
        #endregion

        #region Asynchronous Version

        public void ConnectAsync(string host, int port)
        {
            checkDisposed();
            this.socket.BeginConnect(host, port, connectCallback, null);
        }

        public void ConnectAsync(IPEndPoint endPoint)
        {
            checkDisposed();
            this.socket.BeginConnect(endPoint, connectCallback, null);
        }

        public void ConnectAsync(IPAddress ipAddress, int port)
        {
            checkDisposed();
            this.socket.BeginConnect(ipAddress, port, connectCallback, null);
        }

        #endregion

        private void OnConnect(bool isConnected, Exception ex)
        {
            checkDisposed();

            if (isConnected)
                beginRead();

            AsynchConnectionResult?.Invoke(this, new TcpSocketConnectionStateEventArgs(isConnected, ex));            
        }

        private void OnSyncConnect()
        {
            this.Connected = true;
            beginRead();
        }

        #region CallBacks
        private void connectCallback(IAsyncResult ar)
        {
            checkDisposed();
            Exception connectEx = null;

            try
            {
                this.socket.EndConnect(ar);
                this.Connected = true;
            }
            catch (Exception ex)
            {
                connectEx = ex;
            }
            finally
            {
                OnConnect(Connected, connectEx);
            }
        }

        #endregion

        #endregion

        #region IO

        #region Receive

        private void beginRead()
        {
            try
            {
                this.socket.BeginReceive(this.buffer, 0, SIZE_BUFFER_LENGTH, 0, readSizeCallBack, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
        }

        #region Callbacks
        private void readSizeCallBack(IAsyncResult ar)
        {
            try
            {
                var read = this.socket.EndReceive(ar);

                if(read <= 0)
                {
                    throw new SocketException((int)SocketError.ConnectionAborted);
                }

                //If we still didn't recieve the full data
                if(read < SIZE_BUFFER_LENGTH)
                {
                    //Then get what is left
                    var left = SIZE_BUFFER_LENGTH - read;

                    //Now just wait for about 100 seconds
                    while (socket.Available < left)
                        Thread.Sleep(100);

                    //Now read the rest of the data to the buffer, start from where we left(read) and until the end(left)
                    this.socket.Receive(this.buffer, read, left, 0);
                }

                //Once we have recieved the data, get the payload size
                this.payloadSize = BitConverter.ToInt32(this.buffer, 0);

                //Get wehre to start reading data from
                var initialSize = this.payloadSize > BUFFER_SIZE ? BUFFER_SIZE : this.payloadSize;

                //Create the memory stream to hold the data which will be recieved in chunks
                this.payloadStream = new MemoryStream();

                //Start reading the data
                this.socket.BeginReceive(this.buffer, 0, initialSize, 0, receivePayLoadCallBack, null);
            }
            catch (Exception ex)
            {
                OnDisconnected();
                Debug.WriteLine(ex.StackTrace);
            }
        }

        private void receivePayLoadCallBack(IAsyncResult ar)
        {
            try
            {
                var read = this.socket.EndReceive(ar);

                if(read <= 0)
                {
                    throw new SocketException((int)SocketError.ConnectionAborted);
                }

                this.payloadSize -= read;

                this.payloadStream.Write(this.buffer, 0, read);

                if(this.payloadSize > 0)
                {
                    int receiveSize = this.payloadSize > BUFFER_SIZE ? BUFFER_SIZE : this.payloadSize;
                    this.socket.BeginReceive(this.buffer, 0, receiveSize, 0, receivePayLoadCallBack, null);
                }
                else
                {
                    this.payloadStream.Close();
                    byte[] payload = this.payloadStream.ToArray();
                    this.payloadStream = null;
                    beginRead();
                    //DataReceived?.Invoke(this, new TcpSocketReceivedEventArgs(payload));
                    OnDataReceived(payload);
                }
            }
            catch (Exception ex)
            {
                OnDisconnected();
                Debug.WriteLine(ex.StackTrace);
            }
        }

        private void OnDataReceived(byte[] payload)
        {
            DataReceived?.Invoke(this, new TcpSocketReceivedEventArgs(payload));
        }


        #endregion

        #endregion

        #region Send

        public void SendAsync(byte[] payload)
        {
            checkDisposed();
            //Create an array to hold the payload length
            byte[] sizeBuffer = BitConverter.GetBytes(payload.Length);

            //Create an array to hold the payload size and the payload itself
            byte[] fullBuffer = new byte[sizeBuffer.Length + payload.Length];

            /*
            *Copy the size buffer into the array, this will occupy the first 4 places in the array (0,1,2,3) since we are only sending 4 at the time
            */
            Buffer.BlockCopy(sizeBuffer, 0, fullBuffer, 0, sizeBuffer.Length);

            /* 
            * Now copy the payload itself starting at index 0 of the buffer and inserting it at whereever the previous buffer ended which will be 4 until the end
            */
            Buffer.BlockCopy(payload, 0, fullBuffer, sizeBuffer.Length, payload.Length);

            //Now send the whole buffer
            this.socket.BeginSend(fullBuffer, 0, fullBuffer.Length, 0, sendCallBack, null);
        }

        /// <summary>
        /// Use this method only if you will send data rapidly, back to back and you want to avoid memory leak
        /// This is only to avoid memory leak while sending data rapidly, if you are not sending data rapidly then use SendAsync
        /// </summary>
        /// <param name="payload">Your data</param>
        public void SendRapid(byte[] payload)
        {
            checkDisposed();
            lock (sendSync)
            {
                //Send the length
                this.socket.Send(BitConverter.GetBytes(payload.Length));

                //Now send the payload
                this.socket.Send(payload);
            }
        }

        #region Callbacks

        private void sendCallBack(IAsyncResult ar)
        {
            try
            {
                int sent = this.socket.EndSend(ar);

                //TODO: add event and raise it once the data have been sent
            }
            catch (Exception ex)
            {
                OnDisconnected();
                Debug.WriteLine(ex.StackTrace);
            }
        }

        #endregion

        #endregion

        #endregion

        #region Disconnection

        public void Disconnect()
        {
            checkDisposed();
            if (this.Connected)
            {
                this.socket.Disconnect(true);
                this.Connected = false;
            }
        }

        private void OnDisconnected()
        {
            this.Connected = false;
            this.Disconnected?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Cleanup
        public void Dispose()
        {
            checkDisposed();
            if (!this.IsDisposed)
            {
                this.socket.Close();
                this.socket = null;
                this.buffer = null;
                this.payloadSize = 0;
                this.Connected = false;
            }
        }
        #endregion

    }
}
