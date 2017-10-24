using Opc.Ua;
using Opc.Ua.Client;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SocketInterface
{
    class ApplicationClient
    {
        static TcpListener listener;
        TcpClient client;

        ApplicationClient(TcpClient client)
        {
            this.client = client;
        }

        public static void StartAcceptingClients()
        {
            if (listener == null)
            {
                listener = new TcpListener(IPAddress.Any, 1337);
                listener.Start();
                Console.WriteLine("Started Listening...");
            }

            listener.BeginAcceptTcpClient(HandleAsyncConnection, listener);
        }

        private static void HandleAsyncConnection(IAsyncResult res)
        {
            StartAcceptingClients(); //listen for new connections again
            TcpClient client = listener.EndAcceptTcpClient(res);
            Console.WriteLine("Client Connected!");
            new ApplicationClient(client).Listen(client);
        }

        private void Listen(TcpClient client)
        {
            try
            {
                while (client.Connected)
                {
                    Byte[] data = new Byte[256];
                    String responseData = String.Empty;
                    NetworkStream stream = client.GetStream();

                    Int32 bytes = stream.Read(data, 0, data.Length);
                    responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                    stream.HandleCommand(this, responseData);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Client Disconnected! Error: " + e.Message);
            }
        }

        public async Task ConnectWithOpcClientAsync(string url)
        {
            if (!ConsoleClient.IsConnected)
                await ConsoleClient.HandleClient(url);

            client.RPC("OnOpcConnectionState", ConsoleClient.IsConnected);
        }

        public void BrowseNode(string[] args)
        {
            var references = ConsoleClient.session?.FetchReferences(args.Length > 1 ? NodeId.Parse(args[1]) : ObjectIds.ObjectsFolder);
        }
    }
}
