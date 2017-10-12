using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XamarinSockets;

namespace XamarinSocketsServerTEST_
{
    class Program
    {
        static int connectionCounter = 0;
        static void Main(string[] args)
        {
            Console.Title = "Server";
            TcpSocketListener server = new TcpSocketListener(1234);
            server.AcceptedConnection += Server_AcceptedConnection;

            if (server.Running)
            {
                Console.WriteLine("Server is stoping....");
                server.Stop();
                Console.WriteLine("Server stopped!");

            }
            else
            {
                Console.WriteLine("Server is starting....");
                server.Start();
                Console.WriteLine("Server started!");
            }

            Console.ReadLine();
        }

        private static void Server_AcceptedConnection(object sender, AcceptedTcpSocketEventArgs e)
        {
            Console.WriteLine("Connection #{0} is established from: {1}", connectionCounter + 1, e.RemoteEndPoint);
            connectionCounter++;

            TcpSocket client = e.AcceptedSocket;

            client.DataReceived += Client_DataReceived;
            client.Disconnected += Client_Disconnected;
        }

        private static void Client_Disconnected(object sender, EventArgs e)
        {
            Console.WriteLine("Connection lost");
        }

        private static void Client_DataReceived(object sender, TcpSocketReceivedEventArgs e)
        {
            Console.WriteLine("Data Received: {0}", e.Payload.Length);
        }


    }
}
