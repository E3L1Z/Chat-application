﻿using Chat_application;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

/*
 * TODO
 * Encryption
 * ACK?
 * UDP connection?
 * Switch to ReceiveAsync and SendAsync?
*/

public class chatApplication()
{
    static void Main()
    {
        Server server = null;

        Client client = new Client();


        string[] localNames = ["127.0.0.1", "localhost"];

        Console.WriteLine("Type \"help\" to get list of commands");

        while (true)
        {
            string command = Console.ReadLine();
            string[] commandArray = command.Split(" ");

            switch (commandArray[0])
            {
                default:
                    break;

                case "spawn":
                    //Check that no server is being hosted
                    if (server == null)
                    {
                        bool serverSpawnedSuccesfully;
                        server = new Server(commandArray[1..], out serverSpawnedSuccesfully);
                        if (!serverSpawnedSuccesfully) server = null;
                        //If server was succesfully spawned join it
                        else client.Connect("127.0.0.1", ((IPEndPoint)server.connectionsListener.LocalEndPoint).Port, []);
                    }
                    else Console.WriteLine("Server already running on port {0}", ((IPEndPoint)server.connectionsListener.LocalEndPoint).Port);

                    break;
                case "close":
                    if (server != null)
                    {
                        server.CloseServer();
                        server = null;
                    }
                    else Console.WriteLine("No server running use command \"spawn\" to spawn one");

                    break;
                case "join":
                    if(commandArray.Length < 2)
                    {
                        Console.WriteLine("Specify a target to join a server");
                        continue;
                    }

                    int port = 25000;

                    for (int i = 0; i < commandArray.Length; i++)
                    {
                        string arg = commandArray[i];
                        string nextArg = "";

                        if (i + 1 < commandArray.Length) nextArg = commandArray[i + 1];

                        if (arg.StartsWith("-P"))
                        {
                            //Check whether port was specified on the same arg as -P like -P4444 or -P 4444
                            if (arg.Any(char.IsDigit)) int.TryParse(arg[2..], out port);
                            else if (nextArg.Any(char.IsDigit)) int.TryParse(nextArg, out port);
                        }
                    }

                    //Check if user is trying to join their own server 
                    if (server != null && localNames.Contains(commandArray[1]) && port == ((IPEndPoint)server.connectionsListener.LocalEndPoint).Port) Console.WriteLine("You are already connected to your own network");
                    else client.Connect(commandArray[1], port, commandArray[2..]);

                    break;
                case "quit":
                    //Check if user is trying to leave his own server
                    if (server != null && SocketConnected(server.connectionsListener)) Console.WriteLine("You cannot leave your own server");
                    //Leave server if not your own
                    else if (SocketConnected(client.socket)) client.Disconnect();
                    else Console.WriteLine("You are currently not connected to any server");

                    break;
                case "msg":
                    if (!SocketConnected(client.socket))
                    {
                        Console.WriteLine("Client not connected to anything");
                        continue;
                    }

                    int messageIndex = 1;
                    IPAddress destination = ((IPEndPoint)client.socket.RemoteEndPoint).Address;
                    int destinationPort = ((IPEndPoint)client.socket.RemoteEndPoint).Port;

                    for (int i = 1; i < commandArray.Length; i++)
                    {
                        string arg = commandArray[i];
                        string nextArg = "";

                        if (i + 1 < commandArray.Length) nextArg = commandArray[i + 1];

                        if (arg.StartsWith("-d"))
                        {
                            if (IPAddress.TryParse(arg[2..], out var result))
                            {
                                destination = IPAddress.Parse(arg[2..]);
                                messageIndex = i + 1;
                            }
                            else if (IPAddress.TryParse(nextArg[2..], out result))
                            {
                                destination = IPAddress.Parse(nextArg[2..]);
                                messageIndex = i + 2;
                            }

                            if (arg.Contains("localhost"))
                            {
                                destination = GetLocalIPAddress();
                                messageIndex = i + 1;
                            }
                            if (nextArg.Contains("localhost"))
                            {
                                destination = GetLocalIPAddress();
                                messageIndex = i + 2;
                            }
                        }

                        if (arg.StartsWith("-P"))
                        {
                            if (arg.Any(char.IsDigit))
                            {
                                int.TryParse(arg[2..], out destinationPort);
                                messageIndex = i + 1;
                            }
                            else if (nextArg.Any(char.IsDigit))
                            {
                                int.TryParse(nextArg, out destinationPort);
                                messageIndex = i + 2;
                            }
                        }
                    }

                    if (destination.Equals(IPAddress.Parse("127.0.0.1"))) destination = GetLocalIPAddress();

                    Task sendMsg = new Task(() =>
                    {
                        client.Message(0, 0, destination, destinationPort, Encoding.Default.GetBytes(string.Join(" ", commandArray[messageIndex..])));
                    });

                    sendMsg.Start();

                    break;
                case "info":
                    Console.WriteLine("Own ip: {0}", GetLocalIPAddress());
                    if(server != null && SocketConnected(server.connectionsListener)) Console.WriteLine("Server running on port {0}", ((IPEndPoint)server.connectionsListener.LocalEndPoint).Port);
                    if (SocketConnected(client.socket)) Console.WriteLine("Client connected to {0} on port {1}", ((IPEndPoint)client.socket.RemoteEndPoint).Address, ((IPEndPoint)client.socket.LocalEndPoint).Port);

                    break;
                case "users":
                    if (!SocketConnected(client.socket))
                    {
                        Console.WriteLine("Client not connected to anything");
                        continue;
                    }


                    Task usersTask = new Task(() =>
                    {
                        client.Message(2, 0, GetLocalIPAddress(), ((IPEndPoint)client.socket.LocalEndPoint).Port, [0]);
                    });

                    usersTask.Start();

                    break;
                case "kick":
                    if (commandArray.Length < 2 && commandArray[1].Contains(":")) Console.WriteLine("Specify destination");
                    string[] destArray = commandArray[1].Split(":");

                    if (!IPAddress.TryParse(destArray[0], out IPAddress destinationIp) || !ushort.TryParse(destArray[1], out ushort uShortDestinationPort))
                    {
                        Console.WriteLine("No valid user specified");
                        continue;
                    }

                    if (server != null && SocketConnected(server.connectionsListener)) 
                    {
                        if (destinationIp.Equals(GetLocalIPAddress()) && (ushort)((IPEndPoint)client.socket.LocalEndPoint).Port == uShortDestinationPort)
                        {
                            Console.WriteLine("You cannot kick yourself");
                            continue;
                        }

                        server.KickUser(destinationIp, uShortDestinationPort);
                    } 
                    else Console.WriteLine("You are not hosting a server");

                    break;
                case "help":
                    Console.WriteLine(@"spawn - Used to spawn a server
    -P (-P*port* or -P *port*) - Specify port (default 25000)
close - Close running server
join *destIP* - Join server on ip
    -P (-P*port* or -P *port*) - Specify port (default 25000)
quit - Leave connected server
msg *message* - Send message to every person on connected server
    -P (-P*port* or -P *port*) - Specify users port who you want to message (default servers own port)
    -d (-d*destIP* or -d *destIP*) - Specify user who you want to message (default servers ip)
    If -P matches servers port then every user with ip -d will get message
    Specify both users port and ip to message only that user
info - Show information about your ip, your connection to server and the server you are hosting
users - List every user except for you online on the server
kick *ip*:*port* - Kick user with given ip and port off the server
help - Shows this message");
                    break;
            }
        } 
    }

    private static IPAddress GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip;
            }
        }

        return IPAddress.Parse("127.0.0.1");
    }

    public static bool SocketConnected(Socket s)
    {
        if(s == null) return false;

        bool part1 = s.Poll(1000, SelectMode.SelectRead);
        bool part2 = (s.Available == 0);
        if (part1 && part2)
            return false;
        else
            return true;
    }
}