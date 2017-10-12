using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XamarinSockets;

namespace XamarinSocketClientTEST
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Client";
            startClient();
            Console.ReadLine();
        }

        static void startClient()
        {
            TcpSocket socket = new TcpSocket();
            socket.AsynchConnectionResult += Socket_AsynchConnectionResult;
            socket.Disconnected += Socket_Disconnected;
            socket.ConnectAsync("127.0.0.1", 1234);
        }

        private static void Socket_Disconnected(object sender, EventArgs e)
        {
            Console.WriteLine("Connection Lost");
        }

        private static void Socket_AsynchConnectionResult(object sender, TcpSocketConnectionStateEventArgs e)
        {
            if(e.Connected)
            {
                Random rand = new Random();
                for (int i = 0; i < 10; i++)
                {
                    byte[] payload = new byte[rand.Next(8192,24000)];

                    Console.WriteLine("Sending: {0}", payload.Length);

                    var socket = (TcpSocket)sender;

                    socket.SendAsync(payload);
                }
            }
        }
    }
}
