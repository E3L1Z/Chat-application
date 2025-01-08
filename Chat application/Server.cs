using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Chat_application
{
    public class Server
    {
        public Server(string[] args)
        {
            int port = 25000;

            try
            {
                foreach (string arg in args)
                {
                    if (arg.StartsWith("-P") && arg.Any(char.IsDigit)) int.TryParse(arg, out port);
                }
            } catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            

            Socket connectionsListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            IPEndPoint ep = new IPEndPoint(IPAddress.Any, port);
            connectionsListener.Bind(ep);

            connectionsListener.Listen(2);

            Socket socket = connectionsListener.Accept();
            IPAddress clientIP = ((IPEndPoint)socket.RemoteEndPoint).Address;
            int clientPort = ((IPEndPoint)socket.RemoteEndPoint).Port;

            while (socket.Connected)
            {
                byte[] dataRaw = new byte[1018];

                socket.Receive(dataRaw);

                byte method = dataRaw[0];
                switch(method)
                {
                    default:
                        ForwardMessage(dataRaw);
                        break;
                    case 0:
                        ForwardMessage(dataRaw[999..1018]);
                        break;
                    case 1:
                        socket.Close();
                        Console.WriteLine("Connection closed with {0} on port {1}", clientIP, clientPort);
                        break;
                }
            }
        }

        void ForwardMessage(byte[] message)
        {
            Console.WriteLine(message);
        }
    }
}
