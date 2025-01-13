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

        public void Connect(string ip, int port, string[] args)
        {
            if(socket == default(Socket) || socket == null)
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            }

            if(socket.Connected)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }

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
            Message(1, 0, ((IPEndPoint)socket.RemoteEndPoint).Address, ((IPEndPoint)socket.RemoteEndPoint).Port, new byte[1008]);
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();

            socket = null;

            Console.WriteLine("Disconnected");
        }

        public void Message(byte method, byte type, IPAddress destination, int destinationPort, byte[] message)
        {

            for (uint i = 0; i < message.Length; i += 1008)
            {
                byte[] index = BitConverter.GetBytes(i / 1008 + (message.Length > 1008 ? 1 : 0));
                byte[] destinationPortBytes = BitConverter.GetBytes(Convert.ToUInt16(destinationPort));

                if (i + 1008 > message.Length) index = new byte[] { 255, 255, 255, 255};

                byte[] data = {method};
                data = data.Concat(destination.GetAddressBytes()).ToArray();
                data = data.Concat(destinationPortBytes).ToArray();
                data = data.Concat(GetLocalIPAddress()).ToArray();
                data = data.Concat(index).ToArray();
                data = data.Concat(new byte[]{type}).ToArray();
                data = data.Concat(message).ToArray();

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

        private void ReceiveCallback(IAsyncResult AR)
        {
            if (socket == null) return;

            Socket socketAR = (Socket)AR.AsyncState;
            socketAR.EndReceive(AR);
            byte[] dataBuf = new byte[1024];
            Array.Copy(buffer, dataBuf, 1024);

            byte method = dataBuf[0];

            if(method == 1)
            {
                Disconnect();
                return;
            }

            IPAddress src = new IPAddress(dataBuf[7..11]);

            if (BitConverter.ToInt32(dataBuf[11..15]) > 0)
            {

            } else
            {
                if (dataBuf[15] == 0)
                {
                    Console.WriteLine("{0}: {1}", src, Encoding.ASCII.GetString(dataBuf[16..]));
                }
            }

            socketAR.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socketAR);
        }
    }
}
