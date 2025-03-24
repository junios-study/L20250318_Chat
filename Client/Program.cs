using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Threading;

namespace Client
{
    class Program
    {

        // 정수형 숫자
        //short //htons
        //int,  //htonl
        //long  //htonll
        //[1][2]

        //[][]
        static void Main(string[] args)
        {

            Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            IPEndPoint listenEndPoint = new IPEndPoint(IPAddress.Parse("192.168.0.22"), 4000);

            clientSocket.Connect(listenEndPoint);

           
            while(true)
            {
                string InputChat;
                Console.Write("채팅 : ");
                InputChat = Console.ReadLine();

                string jsonString = "{\"message\" : \"" + InputChat + ".\"}";
                byte[] message = Encoding.UTF8.GetBytes(jsonString);
                ushort length = (ushort)message.Length;

                //길이  자료
                //[][] [][][][][][][][]
                byte[] lengthBuffer = new byte[2];
                lengthBuffer = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)length));

                //[][][][][][][][][][][]
                byte[] buffer = new byte[2 + length];

                Buffer.BlockCopy(lengthBuffer, 0, buffer, 0, 2);
                Buffer.BlockCopy(message, 0, buffer, 2, length);

                int SendLength = clientSocket.Send(buffer, buffer.Length, SocketFlags.None);



                int RecvLength = clientSocket.Receive(lengthBuffer, 2, SocketFlags.None);
                length = BitConverter.ToUInt16(lengthBuffer, 0);
                length = (ushort)IPAddress.NetworkToHostOrder((short)length);
                byte[] recvBuffer = new byte[4096];
                RecvLength = clientSocket.Receive(recvBuffer, length, SocketFlags.None);

                string JsonString = Encoding.UTF8.GetString(recvBuffer);

                Console.WriteLine(JsonString);
            }

            clientSocket.Close();
         }
    }
}
