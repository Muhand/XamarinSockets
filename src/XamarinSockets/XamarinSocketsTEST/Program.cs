using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using XamarinSockets;
namespace XamarinSocketsTEST
{
    class Program
    {

        #region OLD TEST
        //static int connectionCounter = 0;
        //static Socket cli;
        //static void Main(string[] args)
        //{

        //    //TcpSocketListener server = new TcpSocketListener(1234);
        //    //server.AcceptedConnection += Server_AcceptedConnection;
        //    //server.Start();
        //    //for (int i = 0; i < 10; i++)
        //    //{
        //    //    Console.WriteLine(i);
        //    //    Socket cli = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        //    //    //cli.Connect(new IPEndPoint(IPAddress.Loopback, 1234));
        //    //    cli.Connect(new IPEndPoint(IPAddress.Parse("192.168.1.168"), 1234));

        //    //    cli.Close();
        //    //}
        //    //Socket cli = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        //    //cli.Connect(new IPEndPoint(IPAddress.Parse("192.168.1.168"), 1234));
        //    ////cli.Connect(new IPEndPoint(IPAddress.Parse("74.101.42.6"), 1234));
        //    //cli.Close();

        //    string input;

        //    Console.WriteLine("Enter q to end program\nEnter c to make a new connection\nEnter d to disconnect current connection");
        //    do
        //    {   
        //        Console.Write("Your input: ");
        //        input = Console.ReadLine();
        //        switch (input)
        //        {
        //            case "Q":
        //            case "q":
        //                return;
        //            case "C":
        //            case "c":
        //                Console.WriteLine("\nTrying to connect...");

        //                if (cli == null)
        //                {
        //                    try
        //                    {
        //                        cli = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        //                        cli.Connect(new IPEndPoint(IPAddress.Parse("192.168.1.156"), 1234));
        //                        Console.WriteLine("Connected to server\n");
        //                    }
        //                    catch (SocketException)
        //                    {
        //                        Console.WriteLine("The server is either refusing your connection or it's not running,\nPlease try again at some other time.\n");
        //                    }
        //                }
        //                else
        //                { 
        //                    if (!cli.Connected)
        //                    {
        //                        try
        //                        {
        //                            cli = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        //                            cli.Connect(new IPEndPoint(IPAddress.Parse("192.168.1.156"), 1234));
        //                            Console.WriteLine("Connected to server\n");
        //                        }
        //                        catch (SocketException)
        //                        {
        //                            Console.WriteLine("The server is either refusing your connection or it's not running,\nPlease try again at some other time.\n");
        //                        }
        //                    }
        //                    else
        //                        Console.WriteLine("You are already connected, please disconnect first and then reconect, you can disconnect by inputing d\n");
        //                }

        //                break;
        //            case "D":
        //            case "d":
        //                Console.WriteLine("\nDisconnecting...");

        //                if (cli.Connected)
        //                {

        //                    cli.Close();
        //                    Console.WriteLine("Disconnected");
        //                }
        //                else
        //                    Console.WriteLine("Disconnection failed because you are not connected to any server yet.\n");

        //                break;
        //            default:
        //                Console.WriteLine("\nIncorrect input.\n");
        //                Console.WriteLine("Enter q to end program\nEnter c to make a new connection\nEnter d to disconnect current connection");
        //                break;
        //        }



        //    } while (true);
        //}
        #endregion

        static void Main(string[] args)
        {
            Console.Write("Select Type (s = server, c = client): ");
            var type = Console.ReadLine().ToLower();

            switch (type)
            {
                case "c":
                    client();
                    break;
                case "s":
                    Server();
                    break;
            }

            Process.GetCurrentProcess().WaitForExit();
            //Console.WriteLine("Press any key to continue...");
            //Console.ReadLine();
        }

        static void client()
        {
            Console.Title = "Client";
            TcpSocket socket = new TcpSocket();

            socket.AsynchConnectionResult += (s, e) =>
            {
                if (e.Connected)
                {
                    Console.WriteLine("Client Connected");
                    beginChat(false, socket);

                    while (true)
                    {
                        string msg = Console.ReadLine();
                        if (socket.Connected)
                        {
                            if (msg.ToLower() != "disconnect")
                            {
                                byte[] msgPayload = Encoding.ASCII.GetBytes(msg);
                                socket.SendAsync(msgPayload);
                            }
                            else
                            {
                                socket.Disconnect();
                                break;
                            }
                        }
                        else
                            break;
                    }


                }
                else
                {
                    Console.WriteLine("Connection could not be made... Retrying in 2 seconds...");
                    client();
                }
            };
            socket.ConnectAsync("127.0.0.1", 1234);
            
        }

        #region Server
        static void Server()
        {
            Console.Title = "Server";
            Console.WriteLine("Waiting for a client...");

            TcpSocketListener server = new TcpSocketListener(1234);
            TcpSocket client = null;
            server.AcceptedConnection += (s, e)=>
            {
                Console.WriteLine("Client accepted!");
                client = e.AcceptedSocket;
                beginChat(true, client);
                server.Stop();
            };
            server.Start();

            while (true)
            {
                string msg = Console.ReadLine();
                if (client.Connected)
                {
                    if (msg.ToLower() != "disconnect")
                    {
                        byte[] msgPayload = Encoding.ASCII.GetBytes(msg);
                        client.SendAsync(msgPayload);
                    }
                    else
                    {
                        client.Disconnect();
                        break;
                    }
                }
                else
                    break;
            }
            Server();
        }

        private static void beginChat(bool isServer, TcpSocket client)
        {
            Console.WriteLine("Chat ready!");
            string name = isServer ? "Client" : "Server";
            client.DataReceived += (ds, de) =>
            {
                Console.WriteLine("{0}: {1}",name, Encoding.ASCII.GetString(de.Payload));
            };

            client.Disconnected += (ds, de) =>
            {
                Console.WriteLine("{0} has disconnected!",name);
            };
        }

        #endregion
    }
}
