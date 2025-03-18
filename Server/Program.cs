using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class Message
    {
        public string message;
    }

    class Program
    {
        static void Main(string[] args)
        {
            Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            IPEndPoint listenEndPoint = new IPEndPoint(IPAddress.Any, 4000);

            listenSocket.Bind(listenEndPoint);

            listenSocket.Listen(10);

            Socket clientSocket = listenSocket.Accept();

            byte[] buffer = new byte[1024];
            int RecvLength = clientSocket.Receive(buffer);

            string JsonString = Encoding.UTF8.GetString(buffer);

            JObject json = JObject.Parse(JsonString);
            //JsonConvert.DeserializeObject<Message>(JsonString);
            if (json.Value<string>("message").ToString().CompareTo("안녕하세요") == 0)
            {
                byte[] message;
                JObject result = new JObject();
                result.Add("message", "반가워");
                message =  Encoding.UTF8.GetBytes(result.ToString());
                int SendLength = clientSocket.Send(message);
            }


            clientSocket.Close();
            listenSocket.Close();
        }
    }
}
