using Newtonsoft.Json;
using Opc.Ua;
using Opc.Ua.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SocketInterface
{
    class ApplicationClient
    {
        static TcpListener listener;
        TcpClient socket;

        List<MonitoredItem> allMonitoredItems = new List<MonitoredItem>();

        ApplicationClient(TcpClient socket)
        {
            this.socket = socket;
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
            TcpClient socket = listener.EndAcceptTcpClient(res);
            Console.WriteLine("Client Connected!");
            new ApplicationClient(socket).Listen(socket);
        }

        private void Listen(TcpClient socket)
        {
            try
            {
                while (socket.Connected)
                {
                    NetworkStream stream = socket.GetStream();
                    StreamReader reader = new StreamReader(stream);
                    string response = reader.ReadLine();
                    HandleCommand(this, response);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Server Error: " + e.Message);
            }
        }

        private void HandleCommand(Object targetObject, string data)
        {
            string[] args = data.Split('`');
            MethodInfo method = targetObject.GetType().GetMethod(args[0]);
            method.Invoke(targetObject, args.Skip(1).ToArray());
        }

        public async Task ConnectWithOpcClientAsync(string url)
        {
            if (!ConsoleClient.IsConnected)
                await ConsoleClient.HandleClient(url);

            socket.RPC("OnOpcConnectionState", ConsoleClient.IsConnected);
        }

        public void BrowseNode(string id = null)
        {
            try
            {
                ReferenceDescriptionCollection references = ConsoleClient.Session?.FetchReferences(String.IsNullOrEmpty(id) ? ObjectIds.ObjectsFolder : NodeId.Parse(id));
                string json = JsonConvert.SerializeObject(references);
                socket.RPC("BrowseNode", id, json);

            }
            catch (Exception e)
            {
                socket.RPC("OnError", "BrowseNode Error: " + e.Message);
            }
        }

        public void ReadNodeValue(string id = null)
        {
            try
            {
                DataValue value = ConsoleClient.Session?.ReadValue(NodeId.Parse(id));
                socket.RPC("OnReadNodeValue", id, value);
            }
            catch (Exception e)
            {
                socket.RPC("OnError", "ReadNodeValue Error: " + e.Message);
            }
        }

        public void WriteNodeValue(string id = null, string value = null)
        {
            try
            {
                Node node = ConsoleClient.Session?.ReadNode(id);
                node.Write(0, new DataValue(value));
                socket.RPC("OnWriteNodeValue", id, value);
            }
            catch (Exception e)
            {
                socket.RPC("OnError", "WriteNodeValue Error: " + e.Message);
            }
        }

        public void AddSubscription(string id = null, string displayName = null)
        {
            try
            {
                Console.WriteLine(String.Format("AddSubscription({0}, {1})", id, displayName));

                MonitoredItem item = new MonitoredItem()
                {
                    DisplayName = displayName,
                    StartNodeId = NodeId.Parse(id)
                };

                item.Notification += OnNotification;

                ConsoleClient.Session.DefaultSubscription.AddItem(item);
                ConsoleClient.Session.DefaultSubscription.ApplyChanges();
                allMonitoredItems.Add(item);
            }
            catch (Exception e)
            {
                socket.RPC("OnError", "AddSubscription Error: " + e.Message);
            }
        }

        public void RemoveSubscription(string id = null)
        {
            try
            {
                foreach (var item in allMonitoredItems)
                {
                    if (item.StartNodeId == id)
                    {
                        ConsoleClient.Session.DefaultSubscription.RemoveItem(item);
                        ConsoleClient.Session.DefaultSubscription.ApplyChanges();
                        allMonitoredItems.Remove(item);
                    }
                }
            }
            catch (Exception e)
            {
                socket.RPC("OnError", "RemoveSubscription Error: " + e.Message);
            }
        }

        public void ClearSubscription()
        {
            try
            {
                ConsoleClient.Session.DefaultSubscription.Delete(false);
                allMonitoredItems.Clear();
            }
            catch (Exception e)
            {
                socket.RPC("OnError", "ClearSubscription Error: " + e.Message);
            }
        }

        private void OnNotification(MonitoredItem item, MonitoredItemNotificationEventArgs e)
        {
            foreach (var value in item.DequeueValues())
            {
                Console.WriteLine("{0}: {1}, {2}, {3}", item.DisplayName, value.Value, value.SourceTimestamp, value.StatusCode);
                socket.RPC("OnNotification", item.StartNodeId, item.DisplayName, value.Value, value.SourceTimestamp, value.StatusCode);
            }
        }
    }
}
