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
        /// <summary>
        /// Execute RPC
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="method">Method Name</param>
        /// <param name="args">String Data or objects that can be parsed by ToString</param>
        public static void RPC(this TcpClient socket, string method, params object[] args)
        {
            string data = method;
            foreach (Object o in args)
                data += "`" + o.ToString();
            socket.WriteString(data);
        }

        /// <summary>
        /// Write StringData to the Client
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="data"></param>
        public static void WriteString(this TcpClient socket, string data)
        {
            NetworkStream stream = socket.GetStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.WriteLine(data);
            writer.Flush();
        }
    }
}
