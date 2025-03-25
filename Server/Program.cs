using MySqlConnector;
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
                                string userId = clientData.Value<String>("id");
                                string userPassword = clientData.Value<String>("password");

                                mySqlConnection.Open();
                                MySqlCommand mySqlCommand = new MySqlCommand();
                                mySqlCommand.Connection = mySqlConnection;

                                mySqlCommand.CommandText = "select * from users where user_id = @user_id and user_password = @user_password";
                                mySqlCommand.Prepare();
                                mySqlCommand.Parameters.AddWithValue("@user_id", userId);
                                mySqlCommand.Parameters.AddWithValue("@user_password", userPassword);

                                MySqlDataReader dataReader = mySqlCommand.ExecuteReader();
                                if ( dataReader.Read() )
                                {
                                    //로그인 성공
                                    JObject result = new JObject();
                                    result.Add("code", "loginresult");
                                    result.Add("messge", "success");
                                    result.Add("name", dataReader["name"].ToString());
                                    result.Add("email", dataReader["email"].ToString());
                                    SendPacket(clientSocket, result.ToString());
                                }
                                else
                                {
                                    //로그인 실패
                                    JObject result = new JObject();
                                    result.Add("code", "loginresult");
                                    result.Add("messge", "failed");
                                    SendPacket(clientSocket, result.ToString());
                                }


                            }
                            else if (code.CompareTo("Signup") == 0)
                            {
                                string userId = clientData.Value<String>("id");
                                string userPassword = clientData.Value<String>("password");
                                string name = clientData.Value<String>("name");
                                string email = clientData.Value<String>("email");

                                mySqlConnection.Open();
                                MySqlCommand mySqlCommand2 = new MySqlCommand();
                                mySqlCommand2.Connection = mySqlConnection;

                                mySqlCommand2.CommandText = "insert into users (user_id, user_password, name, email) values ( @user_id, @user_password, @name, @email)";
                                mySqlCommand2.Prepare();
                                mySqlCommand2.Parameters.AddWithValue("@user_id", userId);
                                mySqlCommand2.Parameters.AddWithValue("@user_password", userPassword);
                                mySqlCommand2.Parameters.AddWithValue("@name", name);
                                mySqlCommand2.Parameters.AddWithValue("@email", email);
                                mySqlCommand2.ExecuteNonQuery();

                                //가입 성공했습니다.
                                JObject result = new JObject();
                                result.Add("code", "signupresult");
                                result.Add("messge", "success");
                                SendPacket(clientSocket, result.ToString());
                            }
                        }
                        catch(Exception e)
                        {
                            Console.WriteLine(e.Message);
                            JObject result = new JObject();
                            result.Add("code", "signupresult");
                            result.Add("messge", "failed");
                            SendPacket(clientSocket, result.ToString());
                        }
                        finally
                        {
                            mySqlConnection.Close();
                        }
                    }
                    else
                    {
                        string message = "{ \"message\" : \" Disconnect : " + clientSocket.RemoteEndPoint + " \"}";

                        SendPacket(clientSocket, message);

                        lock (_lock)
                        {
                            clientSockets.Remove(clientSocket);
                        }

                        clientSocket.Close();




                        return;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error 낸 놈 : {e.Message} {clientSocket.RemoteEndPoint}");

                    string message = "{ \"message\" : \" Disconnect : " + clientSocket.RemoteEndPoint + " \"}";

                    SendPacket(clientSocket, message);

                    lock (_lock)
                    {
                        clientSockets.Remove(clientSocket);
                    }

                    clientSocket.Close();

                    return;
                }
            }
        }

        static void SendPacket(Socket toSocket, string message)
        {
            byte[] messageBuffer = Encoding.UTF8.GetBytes(message);
            ushort length = (ushort)IPAddress.HostToNetworkOrder((short)messageBuffer.Length);

            byte[] headerBuffer = BitConverter.GetBytes(length);

            byte[] packetBuffer = new byte[headerBuffer.Length + messageBuffer.Length];
            Buffer.BlockCopy(headerBuffer, 0, packetBuffer, 0, headerBuffer.Length);
            Buffer.BlockCopy(messageBuffer, 0, packetBuffer, headerBuffer.Length, messageBuffer.Length);

            int SendLength = toSocket.Send(packetBuffer, packetBuffer.Length, SocketFlags.None);

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
