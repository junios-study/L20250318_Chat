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

            //[][] [][][][][][]

            //패킷 길이 받기(header)
            byte[] headerBuffer = new byte[2];
            int RecvLength = clientSocket.Receive(headerBuffer, 2, SocketFlags.None);
            ushort packetlength = BitConverter.ToUInt16(headerBuffer, 0);

            //[][][][][]
            //실제 패킷(header 길이 만큼)
            byte[] dataBuffer = new byte[4096];
            RecvLength = clientSocket.Receive(dataBuffer, packetlength, SocketFlags.None);

            string JsonString = Encoding.UTF8.GetString(dataBuffer);

            Console.WriteLine(JsonString);

            //Custom 패킷 만들기
            //다시 전송 메세지
            string message = "{ \"message\" : \"sdfdsfdsfsdfdsdsfds 잘 받았다.\"}";
            byte[] messageBuffer = Encoding.UTF8.GetBytes(message);
            ushort length = (ushort)messageBuffer.Length;
            //길이  자료
            //[][] [][][][][][][][]
            headerBuffer = BitConverter.GetBytes(length);

            //[][][][][][][][][][][]
            byte[] packetBuffer = new byte[headerBuffer.Length + messageBuffer.Length];

            Buffer.BlockCopy(headerBuffer, 0, packetBuffer, 0, headerBuffer.Length);
            Buffer.BlockCopy(messageBuffer, 0, packetBuffer, headerBuffer.Length, length);

            int SendLength = clientSocket.Send(packetBuffer, packetBuffer.Length, SocketFlags.None);


            clientSocket.Close();
            listenSocket.Close();
        }
    }
}
