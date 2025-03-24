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

            IPEndPoint listenEndPoint = new IPEndPoint(IPAddress.Parse("192.168.0.22"), 4000);

            listenSocket.Bind(listenEndPoint);

            listenSocket.Listen(10);

            List<Socket> clientSockets = new List<Socket>();
            List<Socket> checkRead = new List<Socket>();

            while (true)
            {
                checkRead.Clear();
                checkRead = new List<Socket>(clientSockets);
                checkRead.Add(listenSocket);


                //[listen]
                Socket.Select(checkRead, null, null, 10);

                foreach (Socket findSocket in checkRead)
                {
                    if (findSocket == listenSocket)
                    {
                        Socket clientSocket = listenSocket.Accept();
                        clientSockets.Add(clientSocket);
                        Console.WriteLine($"Connect client : {clientSocket.RemoteEndPoint}");
                    }
                    else
                    {
                        try
                        {
                            byte[] headerBuffer = new byte[2];
                            int RecvLength = findSocket.Receive(headerBuffer, 2, SocketFlags.None);
                            if (RecvLength > 0)
                            {
                                short packetlength = BitConverter.ToInt16(headerBuffer, 0);
                                packetlength = IPAddress.NetworkToHostOrder(packetlength);

                                byte[] dataBuffer = new byte[4096];
                                RecvLength = findSocket.Receive(dataBuffer, packetlength, SocketFlags.None);
                                string JsonString = Encoding.UTF8.GetString(dataBuffer);
                                Console.WriteLine(JsonString);

                                string message = "{ \"message\" : \"클라이언트 받고 서버꺼 추가.\"}";
                                byte[] messageBuffer = Encoding.UTF8.GetBytes(message);
                                ushort length = (ushort)IPAddress.HostToNetworkOrder((short)messageBuffer.Length);

                                headerBuffer = BitConverter.GetBytes(length);

                                byte[] packetBuffer = new byte[headerBuffer.Length + messageBuffer.Length];
                                Buffer.BlockCopy(headerBuffer, 0, packetBuffer, 0, headerBuffer.Length);
                                Buffer.BlockCopy(messageBuffer, 0, packetBuffer, headerBuffer.Length, messageBuffer.Length);
                                int SendLength = findSocket.Send(packetBuffer, packetBuffer.Length, SocketFlags.None);
                            }
                            else
                            {
                                findSocket.Close();
                                clientSockets.Remove(findSocket);
                            }
                        }
                        catch(Exception e)
                        {
                            Console.WriteLine($"Error 낸 놈 : {findSocket.RemoteEndPoint}");

                            findSocket.Close();
                            clientSockets.Remove(findSocket);
                        }
                    }

                }
            }

            listenSocket.Close();
        }
    }
}
