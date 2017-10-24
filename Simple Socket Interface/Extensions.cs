using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace SocketInterface
{
    static class Extensions
    {
        public static void HandleCommand(this NetworkStream stream, Object targetObject, string data)
        {
            string[] args = data.Split('`');
            MethodInfo method = targetObject.GetType().GetMethod(args[0]);
            method.Invoke(targetObject, args.Skip(1).ToArray());
        }

        public static void RPC(this TcpClient client, string method, params object[] args)
        {
            string data = method;
            foreach (Object o in args)
                data += "`" + o.ToString();
            client.WriteString(data);
        }

        public static void WriteString(this TcpClient client, string data)
        {
            NetworkStream stream = client.GetStream();
            Byte[] bytes = Encoding.ASCII.GetBytes(data);
            stream.Write(bytes, 0, bytes.Length);
        }
    }
}
