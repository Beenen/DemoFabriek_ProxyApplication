using System;

namespace SocketInterface
{
    class Program
    {
        static void Main(string[] args)
        {
            ApplicationClient.StartAcceptingClients();
            Console.ReadLine();
        }
    }
}
