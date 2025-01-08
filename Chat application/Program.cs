using Chat_application;
using System;

public class chatApplication()
{
    static void Main()
    {
        string command = Console.ReadLine().ToUpper();
        string[] commandArray = command.Split(" ");

        switch (commandArray[0])
        {
            case "SPAWN":
                Server server = new Server(commandArray[1..]);
                break;
        }
    }
}