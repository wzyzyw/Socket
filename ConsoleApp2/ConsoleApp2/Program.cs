using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace ConsoleApp2
{
    class Program
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
        public Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        public static ManualResetEvent allDone = new ManualResetEvent(false);
        private static ManualResetEvent sendDone =
        new ManualResetEvent(false);
        private static ManualResetEvent receiveDone =
        new ManualResetEvent(false);
        
        static void Main(string[] args)
        {
            Program test_server = new Program();
            test_server.start_accept();

        }
        void start_accept()
        {
            try
            {
                IPAddress local = IPAddress.Parse("127.0.0.1");
                IPEndPoint iep = new IPEndPoint(local, 50088);
                server.Bind(iep);
                server.Listen(100);
                while (true)
                {
                    allDone.Reset();
                    Console.WriteLine("Waiting for a connection...");
                    server.BeginAccept(new AsyncCallback(Acceptcallback), server);
                    allDone.WaitOne();
                   
                }
                    
                // Start an asynchronous socket to listen for connections.     

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            Console.WriteLine("\nPress ENTER to continue...");
            Console.Read();

        }

        public static void Acceptcallback(IAsyncResult iar)
        {
            // Signal the main thread to continue.     
            
            //还原传入的原始套接字
            Socket listener = (Socket)iar.AsyncState;
            //在原始套接字上调用EndAccept方法，返回新的套接字
            Socket handler = listener.EndAccept(iar);

            // Create the state object.     
            StateObject state = new StateObject();
            state.workSocket = handler;
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
            receiveDone.WaitOne();
            allDone.Set();

            // Create the state object.     
            //StateObject state = new StateObject();
            //state.workSocket = handler;
            //handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
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
                Console.WriteLine("Sent {0} bytes to client.", bytesSent);
               
                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
                sendDone.Set();
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
                Console.WriteLine("\read data from client!!!");
                String rx_content = String.Empty;
                StateObject state = (StateObject)ar.AsyncState;
                Socket client = state.workSocket;
                // Read data from the remote device.     
                int bytesRead = client.EndReceive(ar);
                if (bytesRead > 0)
                {
                    // There might be more data, so store the data received so far.     
                    state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));
                    // Get the rest of the data.     
                    rx_content=state.sb.ToString();
                    if (rx_content.IndexOf("location") > -1)
                    {
                        string content = "x=12.3,y=13.6";
                        Send(client, content);
                        sendDone.WaitOne();
                        receiveDone.Set();
                    }
                    
                }
                else
                {
                    // Not all data received. Get more.     
                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

    }
}
