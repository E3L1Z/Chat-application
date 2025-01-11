using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;

namespace Chat_application
{
    public class Server
    {
        public Socket connectionsListener { get; private set; }

        private List<Socket> connectedClients = new List<Socket>();
        private byte[] buffer = new byte[1024];

        public Server(string[] args, out bool success)
        {
            int port = 25000;
            success = false;

            try
            {
                for(int i = 0; i < args.Length; i++)
                {
                    string arg = args[i];
                    string nextArg = "";

                    if(i +1 < args.Length) nextArg = args[i+1];

                    if (arg.StartsWith("-P"))
                    {
                        if (arg.Any(char.IsDigit)) int.TryParse(arg[2..], out port);
                        else if(nextArg.Any(char.IsDigit)) int.TryParse(nextArg, out port);
                    }
                }
            } catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            try
            {
                connectionsListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                IPEndPoint ep = new IPEndPoint(IPAddress.Any, port);
                connectionsListener.Bind(ep);

                Console.WriteLine("Server online on port {0}", ((IPEndPoint)connectionsListener.LocalEndPoint).Port);

                Task startListeningTask = new Task(() => { StartListening(); });

                startListeningTask.Start();
                success = true;
            }catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void StartListening()
        {
            connectionsListener.Listen(2);

            connectionsListener.BeginAccept(AcceptCallback, null);
        }

        private void AcceptCallback(IAsyncResult AR)
        {
            Socket socket = connectionsListener.EndAccept(AR);
            Console.WriteLine("{0} joined on port {1}", ((IPEndPoint)socket.RemoteEndPoint).Address, ((IPEndPoint)socket.RemoteEndPoint).Port);
            connectedClients.Add(socket);
            socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
            connectionsListener.BeginAccept(AcceptCallback, null);
        }

        private void ReceiveCallback(IAsyncResult AR)
        {
            Socket socket = (Socket)AR.AsyncState;
            socket.EndReceive(AR);
            byte[] dataBuf = new byte[1024];
            Array.Copy(buffer, dataBuf, 1024);

            IPAddress destination = new IPAddress(dataBuf[1..5]);
            ushort destinationPort = BitConverter.ToUInt16(dataBuf[5..7]);
            Task forwardMessage = new Task(() => { ForwardMessage(dataBuf, destination, destinationPort); });
            forwardMessage.Start();

            socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
        }

        private void ForwardMessage(byte[] packet, IPAddress destination, ushort port)
        {
            bool forwardToEveryone = false;

            ushort ownPort = Convert.ToUInt16(((IPEndPoint)connectionsListener.LocalEndPoint).Port);
            byte[] ownIP = GetLocalIPAddress();
            if (destination.GetAddressBytes().SequenceEqual(ownIP) && port == ownPort) forwardToEveryone = true;

            foreach (Socket socket in connectedClients)
            {
                if(forwardToEveryone || (((IPEndPoint)socket.RemoteEndPoint).Address == destination && Convert.ToUInt16(((IPEndPoint)connectionsListener.LocalEndPoint).Port) == port) || (((IPEndPoint)socket.RemoteEndPoint).Address == destination && Convert.ToUInt16(((IPEndPoint)socket.RemoteEndPoint).Port) == port))
                {
                    socket.BeginSend(packet, 0, packet.Length, SocketFlags.None, new AsyncCallback(SendCallBack), socket);
                }
            }
        }

        private void SendCallBack(IAsyncResult AR)
        {
            Socket socket = (Socket)AR.AsyncState;
            socket.EndSend(AR);
        }

        public void CloseServer()
        {
            foreach(Socket socket in connectedClients)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                connectedClients.Remove(socket);
            }

            connectionsListener.Close();
            Console.WriteLine("Server closed");
        }

        private byte[] GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.GetAddressBytes();
                }
            }

            return Encoding.Default.GetBytes(string.Format("127.0.0.1:{0}", ((IPEndPoint)connectionsListener.RemoteEndPoint).Port));
        }
    }
}
