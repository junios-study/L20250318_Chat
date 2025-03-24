using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

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


                //Polling
                //[listen]
                Socket.Select(checkRead, null, null, -1);

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

                                JObject clientData = JObject.Parse(JsonString);

                                string message = "{ \"message\" : \""  + clientData.Value<String>("message") +  "\"}";
                                byte[] messageBuffer = Encoding.UTF8.GetBytes(message);
                                ushort length = (ushort)IPAddress.HostToNetworkOrder((short)messageBuffer.Length);

                                headerBuffer = BitConverter.GetBytes(length);

                                byte[] packetBuffer = new byte[headerBuffer.Length + messageBuffer.Length];
                                Buffer.BlockCopy(headerBuffer, 0, packetBuffer, 0, headerBuffer.Length);
                                Buffer.BlockCopy(messageBuffer, 0, packetBuffer, headerBuffer.Length, messageBuffer.Length);
                                foreach (Socket sendSocket in clientSockets)
                                {
                                    int SendLength = sendSocket.Send(packetBuffer, packetBuffer.Length, SocketFlags.None);
                                }
                            }
                            else
                            {
                                string message = "{ \"message\" : \" Disconnect : " + findSocket.RemoteEndPoint + " \"}";
                                byte[] messageBuffer = Encoding.UTF8.GetBytes(message);
                                ushort length = (ushort)IPAddress.HostToNetworkOrder((short)messageBuffer.Length);

                                headerBuffer = BitConverter.GetBytes(length);

                                byte[] packetBuffer = new byte[headerBuffer.Length + messageBuffer.Length];
                                Buffer.BlockCopy(headerBuffer, 0, packetBuffer, 0, headerBuffer.Length);
                                Buffer.BlockCopy(messageBuffer, 0, packetBuffer, headerBuffer.Length, messageBuffer.Length);

                                findSocket.Close();
                                clientSockets.Remove(findSocket);

                                foreach (Socket sendSocket in clientSockets)
                                {
                                    int SendLength = sendSocket.Send(packetBuffer, packetBuffer.Length, SocketFlags.None);
                                }
                            }
                        }
                        catch(Exception e)
                        {
                            Console.WriteLine($"Error 낸 놈 : {e.Message} {findSocket.RemoteEndPoint}");

                            string message = "{ \"message\" : \" Disconnect : " + findSocket.RemoteEndPoint + " \"}";
                            byte[] messageBuffer = Encoding.UTF8.GetBytes(message);
                            ushort length = (ushort)IPAddress.HostToNetworkOrder((short)messageBuffer.Length);

                            byte[] headerBuffer = new byte[2];

                            headerBuffer = BitConverter.GetBytes(length);

                            byte[] packetBuffer = new byte[headerBuffer.Length + messageBuffer.Length];
                            Buffer.BlockCopy(headerBuffer, 0, packetBuffer, 0, headerBuffer.Length);
                            Buffer.BlockCopy(messageBuffer, 0, packetBuffer, headerBuffer.Length, messageBuffer.Length);

                            findSocket.Close();
                            clientSockets.Remove(findSocket);

                            foreach (Socket sendSocket in clientSockets)
                            {
                                int SendLength = sendSocket.Send(packetBuffer, packetBuffer.Length, SocketFlags.None);
                            }

                        }
                    }

                }

                //Server 작업
                {
                    Console.WriteLine("서버 작업");
                }

            }

            listenSocket.Close();
        }
    }
}
