using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace SocketInterface
{
    static class Extensions
    {
        public static void RPC(this TcpClient socket, string method, params object[] args)
        {
            string data = method;
            foreach (Object o in args)
                data += "`" + o.ToString();
            socket.WriteString(data);
        }

        public static void WriteString(this TcpClient socket, string data)
        {
            NetworkStream stream = socket.GetStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.WriteLine(data);
            writer.Flush();
            Console.WriteLine("RPC: " + data);
        }
    }
}
