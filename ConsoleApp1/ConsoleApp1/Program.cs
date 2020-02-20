using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace ConsoleApp1
{
    public class StateObject
    {
        // Client  socket.
        public Socket workSocket = null;
        // Size of receive buffer.
        public const int BufferSize = 1024;
        // Receive buffer.
        public byte[] buffer = new byte[BufferSize];
        // Received data string.
        public StringBuilder sb = new StringBuilder();
    }
    class Program
    {
        public Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        public static ManualResetEvent allDone = new ManualResetEvent(false);
        private static ManualResetEvent receiveDone =
        new ManualResetEvent(false);
        private static ManualResetEvent connectDone = new ManualResetEvent(false);
        private static ManualResetEvent sendDone =
        new ManualResetEvent(false);
        private static String response = String.Empty;
        static void Main(string[] args)
        {
            Program test = new Program();
            test.SendMessage();
        }
        public void SendMessage()
        {
            
            string address = "127.0.0.1";
            IPAddress hostIP = IPAddress.Parse(address);
            int port = 50088;
            try
            {
                //这是一个异步的建立连接，当连接建立成功时调用connectCallback方法

                clientSocket.BeginConnect(hostIP, port, new AsyncCallback(connectCallback), clientSocket);
                connectDone.WaitOne();
                string content = "location";
                Send(clientSocket, content);
                sendDone.WaitOne();
                Receive(clientSocket);
                receiveDone.WaitOne();
                Console.WriteLine("Response received : {0}", response);
                
                string[] location_str = response.Split(new char[4] {'x','y','=',','});
                List<string> list = new List<string>();
                foreach (string i in location_str)
                {
                    if (!string.IsNullOrEmpty(i))
                    {
                        list.Add(i);
                        Console.WriteLine(i.ToString());
                    }
                }
                location_str = list.ToArray();

                
                clientSocket.Shutdown(SocketShutdown.Both);
                clientSocket.Close();
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void connectCallback(IAsyncResult asyncConnect)
        {
            
            try
            {
                Socket client = (Socket)asyncConnect.AsyncState;
                client.EndConnect(asyncConnect);
                Console.WriteLine("Socket connected to {0}", client.RemoteEndPoint.ToString());
                // Signal that the connection has been made.     
                connectDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
           
        }

        private static void Receive(Socket client)
        {
            try
            {
                // Create the state object.     
                StateObject state = new StateObject();
                state.workSocket = client;
                // Begin receiving the data from the remote device.     
                Console.WriteLine("async receive location");
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        private static void ReceiveCallback(IAsyncResult ar)
        {

            try
            {
                // Retrieve the state object and the client socket     
                // from the asynchronous state object.     
                
                StateObject state = (StateObject)ar.AsyncState;
                Socket client = state.workSocket;
                // Read data from the remote device.     
                int bytesRead = client.EndReceive(ar);
                
                if (bytesRead > 0)
                {
                    // There might be more data, so store the data received so far.     

                    state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));
                    // Get the rest of the data.     
                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
                }
                else
                {
                    // All the data has arrived; put it in response.     
                    if (state.sb.Length > 1)
                    {
                        response = state.sb.ToString();
                        Console.WriteLine(response.ToString());
                    }
                    // Signal that all bytes have been received.     
                    receiveDone.Set();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void Send(Socket handler, String data)
        {
            // Convert the string data to byte data using ASCII encoding.     
            byte[] byteData = Encoding.ASCII.GetBytes(data);
        
            // Begin sending the data to the remote device.     
            handler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), handler);
        }
        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.     
                Socket handler = (Socket)ar.AsyncState;
                // Complete sending the data to the remote device.     
                int bytesSent = handler.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to server.", bytesSent);
                sendDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

    }
}
