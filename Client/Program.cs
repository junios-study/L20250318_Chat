using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            IPEndPoint listenEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 4000);

            clientSocket.Connect(listenEndPoint);

            string jsonString = "{\"message\" : \"안녕하세요2\"}";
            byte[] message = Encoding.UTF8.GetBytes(jsonString);
            int SendLength = clientSocket.Send(message);

            byte[] buffer = new byte[1024];
            int RecvLength = clientSocket.Receive(buffer);
            string JsonString = Encoding.UTF8.GetString(buffer);

            Console.WriteLine(JsonString);

            clientSocket.Close();
        }
    }
}
