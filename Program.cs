using System;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Server";
            Server server = new Server();
            server.SetupServer();
            Console.ReadLine();
        }
    }
}
