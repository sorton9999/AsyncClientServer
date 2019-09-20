using System;  
using System.Net;  
using System.Net.Sockets;  
using System.Text;  
using System.Threading;


namespace AsyncServer
{
    // State object for reading client data asynchronously  
    public class StateObject
    {
        // Size of receive buffer.  
        public const int BufferSize = 1024;

        // Client  socket.  
        public Socket workSocket = null;

        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];

        // Received data string.  
        public StringBuilder sb = new StringBuilder();
    }

    public static class AsyncServer
    {
        // Thread signals  
        private static ManualResetEvent allDone = new ManualResetEvent(false);
        private static ManualResetEvent sendDone = new ManualResetEvent(false);
        private static ManualResetEvent recvDone = new ManualResetEvent(false);

        // The string sent from client
        private static String content = String.Empty;

        // Send and Recv Threads
        private static Thread sendThread = null;
        private static Thread recvThread = null;

        // Finish processing when value is true
        private static bool done = false;

        // Are we connected to a client?
        private static bool isConnected = false;

        public static void StartListening()
        {
            // Establish the local endpoint for the socket.  
            // The DNS name of the computer  
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);

            // Create a TCP/IP socket.  
            Socket listener = new Socket(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and listen for incoming connections.  
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(100);

                sendThread = new Thread(SendLoop);
                recvThread = new Thread(RecvLoop);

                while (!done)
                {
                    // Set the event to nonsignaled state.  
                    allDone.Reset();

                    // Start an asynchronous socket to listen for connections.  
                    Console.WriteLine("Waiting for a connection...");
                    listener.BeginAccept(
                        new AsyncCallback(AcceptCallback),
                        listener);

                    // Wait until a connection is made before continuing.  
                    allDone.WaitOne();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\nPress ENTER to continue...");
            Console.Read();

        }

        private static void RecvLoop(object objArg)
        {
            if (!isConnected)
            {
                return;
            }
            Socket clientSock = objArg as Socket;
            StateObject state = new StateObject();
            state.workSocket = clientSock;
            while (!done && (clientSock != null))
            {
                // Receive the response from the remote device.  
                Receive(state);
                recvDone.WaitOne();
            }
        }

        private static void SendLoop(object objArg)
        {
            if (!isConnected)
            {
                return;
            }
            Socket clientSock = objArg as Socket;
            while (!done && (clientSock != null))
            {
                // Send test data to the remote device.  
                Send(clientSock, content);
                sendDone.WaitOne();

                content = String.Empty;
            }
        }

        public static void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.  
            allDone.Set();

            // Get the socket that handles the client request.  
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            // Start the service threads
            sendThread.Start(handler);
            recvThread.Start(handler);

            if (handler.Connected)
            {
                isConnected = true;
            }

            // Create the state object.  
            //StateObject state = new StateObject();
            //state.workSocket = handler;
            //if (handler.Connected)
            //{
            //    handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
            //        new AsyncCallback(ReadCallback),
            //        state);
            //}
        }

        public static void Receive(StateObject state)
        {
            if (!isConnected)
            {
                return;
            }

            try
            {
                Socket clientSock = state.workSocket;

                // Begin receiving the data from the remote device.
                if (clientSock.Connected)
                {
                    clientSock.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReadCallback),
                        state);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public static void ReadCallback(IAsyncResult ar)
        {
            //String content = String.Empty;

            // Retrieve the state object and the handler socket  
            // from the asynchronous state object.  
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;

            try
            {
                // Read data from the client socket.   
                int bytesRead = handler.EndReceive(ar);

                if (bytesRead > 0)
                {
                    // There  might be more data, so store the data received so far.  
                    state.sb.Append(Encoding.ASCII.GetString(
                        state.buffer, 0, bytesRead));

                    // Check for end-of-file tag. If it is not there, read   
                    // more data.  
                    content = state.sb.ToString();
                    if (content.IndexOf(">>>") > -1)
                    {
                        // All the data has been read from the   
                        // client. Display it on the console.  
                        Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",
                            content.Length, content);
                        // Echo the data back to the client.  
                        Send(handler, content);
                        state.sb.Clear();
                    }
                    else if (content.IndexOf("exit") > -1)
                    {
                        Console.WriteLine("Client {0} is Exiting.", handler.Handle);
                        Send(handler, "Goodbye");
                        done = true;
                        // Disconnect socket
                        handler.Shutdown(SocketShutdown.Both);
                        handler.Close();
                        // Signal the thread wait to expire
                        allDone.Set();
                    }
                    else
                    {
                        // Not all data received. Get more.
                        if (handler.Connected)
                        {
                            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                                new AsyncCallback(ReadCallback),
                                state);
                        }
                    }

                    recvDone.Set();
                    recvDone.Reset();
                }
            }
            catch (SocketException se)
            {
                Console.WriteLine("{0} --  Code: [{1}]", se.Message, se.ErrorCode);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private static void Send(Socket handler, String data)
        {
            if (String.IsNullOrEmpty(data))
            {
                return;
            }

            // Convert the string data to byte data using ASCII encoding.  
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.
            if (handler.Connected)
            {
                handler.BeginSend(byteData, 0, byteData.Length, 0,
                    new AsyncCallback(SendCallback),
                    handler);
            }
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

                sendDone.Set();
                sendDone.Reset();
                //handler.Shutdown(SocketShutdown.Both);
                //handler.Close();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}