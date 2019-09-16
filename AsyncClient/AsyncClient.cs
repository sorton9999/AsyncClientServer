using System;  
using System.Net;  
using System.Net.Sockets;  
using System.Threading;  
using System.Text;


namespace AsyncClient
{
    // State object for receiving data from remote device.  
    public class StateObject
    {
        // Size of receive buffer.  
        public const int BufferSize = 1024;

        // Client socket.  
        public Socket workSocket = null;

        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];

        // Received data string.  
        public StringBuilder sb = new StringBuilder();
    }

    public static class AsyncClient
    {
        // The port number for the remote device.  
        private const int DFLT_PORT = 11000;
        private const string DFLT_HOST = "10.241.129.208";

        // ManualResetEvent instances signal completion.  
        private static ManualResetEvent connectDone =
            new ManualResetEvent(false);
        private static ManualResetEvent sendDone =
            new ManualResetEvent(false);
        private static ManualResetEvent receiveDone =
            new ManualResetEvent(false);

        // The response from the remote device.  
        private static String response = String.Empty;

        // Signal done with processing
        private static bool done = false;

        // Are we conneted?
        private static bool isConnected = false;

        public static void StartClient()
        {
            // Connect to a remote device.  
            try
            {
                // Establish the remote endpoint for the socket.  
                IPHostEntry ipHostInfo = Dns.GetHostEntry(DFLT_HOST);
                IPAddress ipAddress = ipHostInfo.AddressList[0];
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, DFLT_PORT);

                // Create a TCP/IP socket.  
                Socket client = new Socket(ipAddress.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);

                // Send and Recv threads
                Thread sendThread = new Thread(SendLoop);
                Thread recvThread = new Thread(ReceiveLoop);

                // Connect to the remote endpoint.  
                client.BeginConnect(remoteEP,
                    new AsyncCallback(ConnectCallback),
                    client);
                connectDone.WaitOne();

                // Start timers
                sendThread.Start(client);
                recvThread.Start(client);

                // This loop checks for connection
                while (!done)
                {
                    Thread.Sleep(200);
                }

                // Close connection to server
                client.Shutdown(SocketShutdown.Both);
                client.Close();

                // Allow threads to return
                if (sendThread.IsAlive)
                {
                    sendThread.Join();
                }
                if (recvThread.IsAlive)
                {
                    recvThread.Join();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void SendLoop(object objArg)
        {
            if (!isConnected)
            {
                return;
            }

            // Grab the argument as a socket
            Socket clientSock = objArg as Socket;
            StringBuilder sb = new StringBuilder();
            while (isConnected && !done && (clientSock != null))
            {
                // reset the send wait
                //sendDone.Reset();

                Console.WriteLine("Enter Text to Send. Type >>> to End Entry...\n");
                while (sb.ToString().IndexOf(">>>") < 0)
                {
                    sb.Append(Console.ReadLine());
                }
                // Send test data to the remote device.  
                Send(clientSock, sb.ToString());
                sendDone.WaitOne();

                if (sb.ToString().IndexOf("exit>>>") > -1)
                {
                    done = true;
                }

                sb.Clear();         
            }
        }

        private static void ReceiveLoop(object objArg)
        {
            if (!isConnected)
            {
                return;
            }

            // Grab the argument as a socket
            Socket clientSock = objArg as Socket;
            // Create the state object.  
            StateObject state = new StateObject();
            state.workSocket = clientSock;
            while (isConnected && !done && (clientSock != null))
            {
                // Reset the receive wait
                //receiveDone.Reset();

                // Receive the response from the remote device.  
                Receive(state);
                receiveDone.WaitOne();

                // Write the response to the console.  
                Console.WriteLine("Response received : {0}", response);

                response = String.Empty;
            }
        }

        private static void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete the connection.  
                client.EndConnect(ar);

                Console.WriteLine("Socket connected to {0}",
                    client.RemoteEndPoint.ToString());

                // Signal that the connection has been made.  
                connectDone.Set();

                isConnected = true;

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void Receive(StateObject state)
        {
            if (!isConnected)
            {
                return;
            }

            try
            {
                Socket clientSock = state.workSocket;
                Console.WriteLine("Recv Connected? {0}", (clientSock.Connected ? "YES" : "NO"));
                // Begin receiving the data from the remote device.
                //if (clientSock.Connected)
                //{
                    clientSock.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReceiveCallback),
                        state);
                //}
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
                StateObject recvState = (StateObject)ar.AsyncState;
                Socket client = recvState.workSocket;
                SocketError errorCode;

                Console.WriteLine("Recv Callback Connected? {0}", (client.Connected ? "YES" : "NO"));
                if (!client.Connected)
                {
                    isConnected = false;
                    return;
                }

                // Read data from the remote device.  
                int bytesRead = client.EndReceive(ar, out errorCode);
                Console.WriteLine("Bytes Read: {0}", bytesRead);
                //if (errorCode == SocketError.Success)
                //{

                if (bytesRead > 0)
                    {
                        // There might be more data, so store the data received so far.  
                        recvState.sb.Append(Encoding.ASCII.GetString(recvState.buffer, 0, bytesRead));

                        // Get the rest of the data.  
                        client.BeginReceive(recvState.buffer, 0, StateObject.BufferSize, 0,
                            new AsyncCallback(ReceiveCallback),
                            recvState);

                    }
                    else
                    {
                        // All the data has arrived; put it in response.  
                        if (recvState.sb.Length > 1)
                        {
                            response = recvState.sb.ToString();
                            recvState.sb.Clear();
                            //recvState.buffer.SetValue(0, 0);
                        }
                        // Signal that all bytes have been received.  
                        receiveDone.Set();
                        receiveDone.Reset();
                }
                //}
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void Send(Socket clientSock, String data)
        {
            // Convert the string data to byte data using ASCII encoding.  
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            Console.WriteLine("Send Connected? {0}", (clientSock.Connected ? "YES" : "NO"));

            // Begin sending the data to the remote device.
            if (clientSock.Connected)
            {
                clientSock.BeginSend(byteData, 0, byteData.Length, 0,
                    new AsyncCallback(SendCallback),
                    clientSock);
            }
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                if (!client.Connected)
                {
                    isConnected = false;
                    return;
                }

                // Complete sending the data to the remote device.  
                int bytesSent = client.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to server.", bytesSent);

                // Signal that all bytes have been sent.  
                sendDone.Set();
                sendDone.Reset();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
