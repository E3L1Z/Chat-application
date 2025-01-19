using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;


namespace Chat_application
{
    internal class Client
    {
        public Socket socket { get; private set; }

        private byte[] buffer = new byte[1024];

        SortedDictionary<string, LongMessage> longMessages = new SortedDictionary<string, LongMessage>();
        public void Connect(string ip, int port, string[] args)
        {
            if(socket != null && chatApplication.SocketConnected(socket))
            {
                Disconnect();
            }

            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                socket.Connect(ip, port);
                socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);

                Console.WriteLine("Sucessfully joined {0} on port {1}", ip, port);
            } catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void Disconnect()
        {
            //Tell server that user is disconnecting so the server can close connection on its side
            Message(1, 0, ((IPEndPoint)socket.RemoteEndPoint).Address, ((IPEndPoint)socket.RemoteEndPoint).Port, new byte[1008]);
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();

            socket = null;

            Console.WriteLine("Disconnected");
        }

        //Compile a message packet from given parameters and then send it to server
        public void Message(byte method, byte type, IPAddress destination, int destinationPort, byte[] message)
        {
            //If message is larger than 1008 bytes which is max for the packet then send it in multiple packets
            for (uint i = 0; i < message.Length; i += 1008)
            {
                //If message is larger than 1008 bytes then set index to be 1 so server knows that there will be more packets to message
                //Othervice set index to 0
                byte[] index = BitConverter.GetBytes((int)(i / 1008 + (message.Length > 1008 ? 1 : 0)));
                byte[] destinationPortBytes = BitConverter.GetBytes(Convert.ToUInt16(destinationPort));

                int maxIndex = (int)i + 1008;
                //If this is the last packet of message set index to max value so server knows no other packages will be coming to fill the message
                if (i + 1008 > message.Length)
                {
                    index = new byte[] { 255, 255, 255, 255 };
                    maxIndex = message.Length;
                }

                byte[] data = {method};
                data = data.Concat(destination.GetAddressBytes()).ToArray();
                data = data.Concat(destinationPortBytes).ToArray();
                data = data.Concat(GetLocalIPAddress()).ToArray();
                data = data.Concat(index).ToArray();
                data = data.Concat(new byte[]{type}).ToArray();
                data = data.Concat(message[(int)i..maxIndex]).ToArray();

                socket.Send(data);
            }
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

            return (IPAddress.Parse("127.0.0.1")).GetAddressBytes();
        }

        //Message received from server
        private void ReceiveCallback(IAsyncResult AR)
        {
            if (socket == null) return;

            Socket socketAR = (Socket)AR.AsyncState;
            socketAR.EndReceive(AR);
            byte[] dataBuf = new byte[1024];
            Array.Copy(buffer, dataBuf, 1024);
            buffer = new byte[1024];

            byte method = dataBuf[0];

            switch(method)
            {
                //Message
                case 0:
                    IPAddress src = new IPAddress(dataBuf[7..11]);
                    string srcStr = src.ToString();

                    uint index = BitConverter.ToUInt32(dataBuf[11..15]);
                    //If index is 1 or higher expect there to be continuation to current packet
                    if (index > 0)
                    {
                        if (index == uint.MaxValue)
                        {
                            string message = "";
                            if (longMessages.ContainsKey(srcStr))
                            {
                                message = longMessages[srcStr].message;
                                longMessages.Remove(srcStr);
                            }

                            message += Encoding.UTF8.GetString(dataBuf[16..]);

                            //If message type is text
                            if (dataBuf[15] == 0) Console.WriteLine("{0}: {1}", src, message);
                        } else
                        {
                            if (longMessages.ContainsKey(srcStr))
                            {
                                LongMessage newMsg = new LongMessage(index, longMessages[srcStr].message + Encoding.UTF8.GetString(dataBuf[16..]));
                                longMessages.Remove(srcStr);

                                longMessages.Add(srcStr, newMsg);
                            } else
                            {
                                longMessages.Add(srcStr, new LongMessage(index, Encoding.UTF8.GetString(dataBuf[16..])));
                            }
                        }
                    }
                    else
                    {
                        //If message type is text
                        if (dataBuf[15] == 0)
                        {
                            Console.WriteLine("{0}: {1}", src, Encoding.UTF8.GetString(dataBuf[16..]));
                        }
                    }
                    break;
                //Disconnect
                case 1:
                    Disconnect();
                    return;
                //Fetch users
                case 2:
                    string users = "";
                    for(int i = 16; i < dataBuf.Length; i += 6)
                    {
                        IPAddress userIP = new IPAddress(dataBuf[i..(i + 4)]);
                        ushort userPort = BitConverter.ToUInt16(dataBuf[(i + 4)..(i + 6)]);

                        //Stop message from showing user
                        if (userIP.Equals(new IPAddress(GetLocalIPAddress())) && userPort == Convert.ToUInt16(((IPEndPoint)socket.LocalEndPoint).Port)) continue;

                        //If left unchecked would continue to get users even if none were left
                        if (userIP.Equals(IPAddress.Parse("0.0.0.0"))) break;

                        users += string.Format("{0}:{1}, ", userIP, userPort);
                    }

                    //Remove ", " at the end of users
                    if (users.Length > 0) users = users[..(users.Length - 2)];
                    //If only users is connected to server
                    else users = "You are the only user online";

                    Console.WriteLine(users);
                    break;
            }

            socketAR.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socketAR);
        }

        private struct LongMessage
        {
            public LongMessage(uint _index, string _message)
            {
                index = _index;
                message = _message;
            }
            public uint index = 0;
            public string message = "";
        }
    }
}
