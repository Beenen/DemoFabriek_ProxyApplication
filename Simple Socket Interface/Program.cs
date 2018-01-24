using System;

namespace SocketInterface
{
    class Program
    {
        static void Main(string[] args)
        {
            //Start listening for clients, start program process
            ApplicationClient.StartAcceptingClients();
            Console.ReadLine();
        }
    }
}
