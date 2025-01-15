using System;
using System.Linq;
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

        //Client is trying to join server
        private void AcceptCallback(IAsyncResult AR)
        {
            try
            {
                Socket socket = connectionsListener.EndAccept(AR);
                Console.WriteLine("{0} joined on port {1}", ((IPEndPoint)socket.RemoteEndPoint).Address, ((IPEndPoint)socket.RemoteEndPoint).Port);
                connectedClients.Add(socket);
                socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
                connectionsListener.BeginAccept(AcceptCallback, null);
            } catch (Exception ex) { }
            
        }

        //Data received from one of the clients
        private void ReceiveCallback(IAsyncResult AR)
        {
            Socket socket = (Socket)AR.AsyncState;
            socket.EndReceive(AR);
            byte[] dataBuf = new byte[1024];
            Array.Copy(buffer, dataBuf, 1024);
            buffer = new byte[1024];

            byte method = dataBuf[0];

            switch (method)
            {
                //Quit
                case 1:
                    Console.WriteLine("{0} left on port {1}", ((IPEndPoint)socket.RemoteEndPoint).Address, ((IPEndPoint)socket.RemoteEndPoint).Port);
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                    connectedClients.Remove(socket);
                    return;
                //Fetch users
                case 2:
                    Console.WriteLine("Sent users to {0} on port {1}", ((IPEndPoint)socket.RemoteEndPoint).Address, ((IPEndPoint)socket.RemoteEndPoint).Port);

                    dataBuf = dataBuf[..16].Concat(GetUsers()).ToArray();
                    break;
            }

            IPAddress destination = new IPAddress(dataBuf[1..5]);
            ushort destinationPort = BitConverter.ToUInt16(dataBuf[5..7]);

            Task forwardMessage = new Task(() => { ForwardMessage(dataBuf, destination, destinationPort); });
            forwardMessage.Start();

            socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
        }

        //Gets all of the users on current session and returs bytes that contains users ip and port
        private byte[] GetUsers()
        {
            byte[] users = new byte[0];

            foreach(Socket client in connectedClients)
            {
                IPAddress ip = ((IPEndPoint)client.RemoteEndPoint).Address;
                ushort port = Convert.ToUInt16(((IPEndPoint)client.RemoteEndPoint).Port);

                if (ip.Equals(IPAddress.Parse("127.0.0.1")))
                {
                    ip = new IPAddress(GetLocalIPAddress());
                }

                byte[] combinedData = ip.GetAddressBytes().Concat(BitConverter.GetBytes(port)).ToArray();

                users = users.Concat(combinedData).ToArray();
            }

            return users;
        }

        //Send packet to every user with ip=destination and port=port
        //If destionaion is users ip and port is servers then send message to everyone with the ip
        //If destination and port is servers send message to everyone
        private void ForwardMessage(byte[] packet, IPAddress destination, ushort port)
        {
            bool forwardToEveryone = false;

            ushort ownPort = Convert.ToUInt16(((IPEndPoint)connectionsListener.LocalEndPoint).Port);
            byte[] ownIP = GetLocalIPAddress();
            //ip and port is servers
            if (destination.GetAddressBytes().SequenceEqual(ownIP) && port == ownPort) forwardToEveryone = true;

            foreach (Socket socket in connectedClients)
            {
                IPAddress clientIP = ((IPEndPoint)socket.RemoteEndPoint).Address;

                //Make sure that ip cannot be that of a localhost
                if (clientIP.Equals(IPAddress.Parse("127.0.0.1"))) clientIP = new IPAddress(GetLocalIPAddress());

                if (forwardToEveryone || (clientIP.Equals(destination) && Convert.ToUInt16(((IPEndPoint)connectionsListener.LocalEndPoint).Port) == port) || (clientIP.Equals(destination) && Convert.ToUInt16(((IPEndPoint)socket.RemoteEndPoint).Port) == port))
                {
                    socket.BeginSend(packet, 0, packet.Length, SocketFlags.None, new AsyncCallback(SendCallBack), socket);
                }
            }
        }

        //Stop sending message to client
        private void SendCallBack(IAsyncResult AR)
        {
            Socket socket = (Socket)AR.AsyncState;
            socket.EndSend(AR);
        }

        public void CloseServer()
        {
            //Send message to every user that te server is closing and they need to close the connection
            ForwardMessage(new byte[] { 1 }.Concat(new byte[1023]).ToArray(), new IPAddress(GetLocalIPAddress()), Convert.ToUInt16(((IPEndPoint)connectionsListener.LocalEndPoint).Port));

            connectedClients.Clear();

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
