using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    class Program
    {
        static Socket listenSocket;

        static List<Socket> clientSockets = new List<Socket>();
        //static List<Thread> threadManager = new List<Thread>();

        static object _lock = new object();


        static void AcceptThread()
        {
            while (true)
            {
                Socket clientSocket = listenSocket.Accept();

                lock (_lock)
                {
                    clientSockets.Add(clientSocket);
                }
                Console.WriteLine($"Connect client : {clientSocket.RemoteEndPoint}");

                Thread workThread = new Thread(new ParameterizedThreadStart(WorkThread));

                workThread.IsBackground = true;
                workThread.Start(clientSocket);
                //threadManager.Add(workThread);
            }
        }

        static void WorkThread(Object clientObjectSocket)
        {
         
            Socket clientSocket = clientObjectSocket as Socket;

            while (true)
            {
                try
                {
                    byte[] headerBuffer = new byte[2];
                    int RecvLength = clientSocket.Receive(headerBuffer, 2, SocketFlags.None);
                    if (RecvLength > 0)
                    {
                        short packetlength = BitConverter.ToInt16(headerBuffer, 0);
                        packetlength = IPAddress.NetworkToHostOrder(packetlength);

                        byte[] dataBuffer = new byte[4096];
                        RecvLength = clientSocket.Receive(dataBuffer, packetlength, SocketFlags.None);
                        string JsonString = Encoding.UTF8.GetString(dataBuffer);
                        Console.WriteLine(JsonString);

                        string connectionString = "server=localhost;user=root;database=users;password=qweasd123";
                        MySqlConnection mySqlConnection = new MySqlConnection(connectionString);

                        JObject clientData = JObject.Parse(JsonString);
                        string code = clientData.Value<String>("code");

                        try
                        {
                            if (code.CompareTo("Login") == 0)
                            {
                                mySqlConnection.Open();
                                MySqlCommand mySqlCommand = new MySqlCommand();
                                mySqlCommand.Connection = mySqlConnection;

                                mySqlCommand.CommandText = "select * from users where user_id = @user_id and user_password = @user_password";
                                mySqlCommand.Prepare();
                                mySqlCommand.Parameters.AddWithValue("@user_id", "htk008");
                                mySqlCommand.Parameters.AddWithValue("@user_password", "1234");

                                MySqlDataReader dataReader = mySqlCommand.ExecuteReader();
                            }
                            else if (code.CompareTo("Signup") == 0)
                            {
                                MySqlCommand mySqlCommand2 = new MySqlCommand();
                                mySqlCommand2.Connection = mySqlConnection;

                                mySqlCommand2.CommandText = "insert into users (user_id, user_password, name, email) values ( @user_id, @user_password, @name, @email)";
                                mySqlCommand2.Prepare();
                                mySqlCommand2.Parameters.AddWithValue("@user_id", "abc002");
                                mySqlCommand2.Parameters.AddWithValue("@user_password", "333");
                                mySqlCommand2.Parameters.AddWithValue("@name", "노형민");
                                mySqlCommand2.Parameters.AddWithValue("@email", "abc001@abc001.com");
                                mySqlCommand2.ExecuteNonQuery();
                            }
                        }
                        catch(Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    }
                    else
                    {
                        string message = "{ \"message\" : \" Disconnect : " + clientSocket.RemoteEndPoint + " \"}";
                        byte[] messageBuffer = Encoding.UTF8.GetBytes(message);
                        ushort length = (ushort)IPAddress.HostToNetworkOrder((short)messageBuffer.Length);

                        headerBuffer = BitConverter.GetBytes(length);

                        byte[] packetBuffer = new byte[headerBuffer.Length + messageBuffer.Length];
                        Buffer.BlockCopy(headerBuffer, 0, packetBuffer, 0, headerBuffer.Length);
                        Buffer.BlockCopy(messageBuffer, 0, packetBuffer, headerBuffer.Length, messageBuffer.Length);

                        clientSocket.Close();
                        lock (_lock)
                        {
                            clientSockets.Remove(clientSocket);

                            foreach (Socket sendSocket in clientSockets)
                            {
                                int SendLength = sendSocket.Send(packetBuffer, packetBuffer.Length, SocketFlags.None);
                            }
                        }

                        return;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error 낸 놈 : {e.Message} {clientSocket.RemoteEndPoint}");

                    string message = "{ \"message\" : \" Disconnect : " + clientSocket.RemoteEndPoint + " \"}";
                    byte[] messageBuffer = Encoding.UTF8.GetBytes(message);
                    ushort length = (ushort)IPAddress.HostToNetworkOrder((short)messageBuffer.Length);

                    byte[] headerBuffer = new byte[2];

                    headerBuffer = BitConverter.GetBytes(length);

                    byte[] packetBuffer = new byte[headerBuffer.Length + messageBuffer.Length];
                    Buffer.BlockCopy(headerBuffer, 0, packetBuffer, 0, headerBuffer.Length);
                    Buffer.BlockCopy(messageBuffer, 0, packetBuffer, headerBuffer.Length, messageBuffer.Length);

                    clientSocket.Close();
                    lock (_lock)
                    {
                        clientSockets.Remove(clientSocket);

                        foreach (Socket sendSocket in clientSockets)
                        {
                            int SendLength = sendSocket.Send(packetBuffer, packetBuffer.Length, SocketFlags.None);
                        }
                    }

                    return;
                }
            }
        }


        static void Main(string[] args)
        {
            listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            IPEndPoint listenEndPoint = new IPEndPoint(IPAddress.Parse("192.168.0.22"), 4000);

            listenSocket.Bind(listenEndPoint);

            listenSocket.Listen(10);

            Task acceptTask = new Task(AcceptThread);
            //Thread acceptThread = new Thread(new ThreadStart(AcceptThread));
            //acceptThread.IsBackground = true;
            //acceptThread.Start();
            acceptTask.Start();

            //acceptThread.Join();
            acceptTask.Wait();

            listenSocket.Close();

            //FileStream fs = new FileStream("test.txt", FileMode.Open);
            //byte[] buffer = new byte[1024];
            //Task<int> result = fs.ReadAsync(buffer, 0, 1024);
        }
    }
}
