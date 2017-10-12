using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace XamarinSockets
{
    public class AcceptedTcpSocketEventArgs : EventArgs
    {
        public TcpSocket AcceptedSocket { get; private set; }
        public IPEndPoint RemoteEndPoint { get; private set; }

        public AcceptedTcpSocketEventArgs(Socket socket)
        {
            this.AcceptedSocket = new TcpSocket(socket);
            this.RemoteEndPoint = (IPEndPoint)socket.RemoteEndPoint;
        }
    }

    public class TcpSocketConnectionStateEventArgs : EventArgs
    {
        public Exception Exception;
        public bool Connected { get; private set; }

        public TcpSocketConnectionStateEventArgs(bool connected, Exception ex)
        {
            this.Exception = ex;
            this.Connected = connected;
        }
    }

    public class TcpSocketReceivedEventArgs : EventArgs
    {
        public byte[] Payload { get; private set; }
        public TcpSocketReceivedEventArgs(byte[] payload)
        {
            this.Payload = payload;
        }
    }

    public class TcpServerStarted : EventArgs
    {
        public TcpServerStarted()
        {
       
        }
    }

    public class TcpServerStopped : EventArgs
    {
        public TcpServerStopped()
        {
            
        }
    }

    public class TcpServerFailedToStart : EventArgs
    {
        public ServerFailedToStart Reason { get; private set; }
        public TcpServerFailedToStart(ServerFailedToStart reason)
        {
            this.Reason = reason;
        }
    }

    public class TcpServerFailedToStop : EventArgs
    {
        public ServerfailedToStop Reason { get; private set; }
        public TcpServerFailedToStop(ServerfailedToStop reason)
        {
            this.Reason = reason;
        }
    }

    public class ServerStatusChanged : EventArgs
    {
        public ServerStatus NewStatus { get; private set; }
        public ServerStatusChanged(ServerStatus newStatus)
        {
            this.NewStatus = newStatus;
        }
    }

}