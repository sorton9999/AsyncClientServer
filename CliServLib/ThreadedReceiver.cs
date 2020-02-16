using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TcpLib;

namespace CliServLib
{
    public class ThreadedReceiver : ThreadedBase, IReceive
    {
        /// <summary>
        /// The server data received parses out msgs by client ID so it can be static
        /// </summary>
        public static event AsyncCompletedEventHandler ServerDataReceived;

        /// <summary>
        /// This is a non-static client data received event so it can be a new copy
        /// for each client
        /// </summary>
        public event AsyncCompletedEventHandler ClientDataReceived;

        /// <summary>
        /// Receiving a file.  Set this to true until all contents received.
        /// </summary>
        private bool receivingFile = false;

        /// <summary>
        /// File size of the receiving file
        /// </summary>
        private long fileSize = 0;
        private long totalRcv = 0;
        MessageData fileMsg = new MessageData();


        /// <summary>
        /// The server's data receive event caller.
        /// </summary>
        /// <param name="e">Asynchronous event args</param>
        protected virtual void OnServerDataReceived(AsyncCompletedEventArgs e)
        {
            ServerDataReceived?.Invoke(this, e);
        }

        /// <summary>
        /// The client's data receive event caller
        /// </summary>
        /// <param name="e">Asynchronous event args</param>
        protected virtual void OnClientDataReceived(AsyncCompletedEventArgs e)
        {
            ClientDataReceived?.Invoke(this, e);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public ThreadedReceiver()
           : base()
        {
            StartParam(new ParameterizedThreadStart(ReceiveLoop));
        }

        /// <summary>
        /// Public access to the receiving thread
        /// </summary>
        public Thread ReceiveThread
        {
            get { return theThread; }
            private set { theThread = value; }
        }

        /// <summary>
        /// We are receiving a file when TRUE
        /// </summary>
        public bool ReceivingFile
        {
            get { return receivingFile; }
            private set { receivingFile = value; }
        }

        /// <summary>
        /// Main receive loop.  This loops until the LoopDone is TRUE.
        /// The work is done in ClientReceive.
        /// </summary>
        /// <param name="arg"></param>
        public void ReceiveLoop(object arg)
        {
            Client client = arg as Client;
            if (client == null)
            {
                return;
            }

            if ((client != null) && (client.ClientSocket != null))
            {
                ReceiveData rcvData = new ReceiveData();

                while (!looper.LoopDone)
                {
                    System.Diagnostics.Debug.WriteLine("Looper for Client: {0}", client.ClientSocket.Handle);

                    ClientReceive(client);

                }
                Console.WriteLine("Receive Loop Exiting...");
            }
        }

        /// <summary>
        /// The work done on the receive loop.  It assumes we are receiving MessageData types unless
        /// a MessageData received indicates a file to be received.  Then it switches over to receiving
        /// byte[].
        /// </summary>
        /// <param name="client">The client data is received from</param>
        private void ClientReceive(Client client)
        {
            if ((client == null) || (client.ClientSocket == null))
            {
                return;
            }

            ReceiveData rcvData = new ReceiveData();
            try
            {
                client.ClearData();

                var res = TcpLibExtensions.ReceiveAsync(client.ClientSocket, client.ClientData(), 0, client.DataSize, SocketFlags.None, client.CancelSource.Token);
                if (res.IsFaulted || (res.Result == null) || res.Result.Failure)
                {
                    // Just close the socket.  Look into trying to reconnect
                    Console.WriteLine("Receive Fault. Closing socket {0} to client.", client.ClientSocket.Handle);
                    ClientStore.RemoveClient((long)client.ClientSocket.Handle);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Receiving Data From Client: {0}", client.ClientSocket.Handle);
                    try
                    {
                        MessageData value = null;
                        if (receivingFile)
                        {
                            value = fileMsg;
                            value.message = client.ClientData();
                            value.length = res.Result.Value;
                            rcvData.clientData = fileMsg;
                            totalRcv += res.Result.Value;
                            System.Diagnostics.Debug.WriteLine("Received: Size:[{0}] - [{1}] out of [{2}]", res.Result.Value, totalRcv, fileSize);
                        }
                        else
                        {
                            value = ClientData<MessageData>.DeserializeFromByteArray<MessageData>(client.ClientData());
                            rcvData.clientData = value;
                        }
                        rcvData.clientHandle = (long)client.ClientSocket.Handle;

                        if (value != null)
                        {
                            // This indicates a response from the server to a client of which there could be many
                            if (value.response)
                            {
                                // Client received
                                OnClientDataReceived(new AsyncCompletedEventArgs(null, false, rcvData));
                            }
                            else
                            {
                                if (rcvData.clientData.id == 100)
                                {
                                    if (!receivingFile)
                                    {
                                        receivingFile = true;
                                        fileSize = rcvData.clientData.length;
                                        fileMsg.handle = rcvData.clientData.handle;
                                        fileMsg.id = rcvData.clientData.id;
                                        fileMsg.name = rcvData.clientData.name;
                                        fileMsg.response = rcvData.clientData.response;
                                    }
                                    else if (totalRcv >= fileSize)
                                    {
                                        receivingFile = false;
                                        totalRcv = 0;
                                        Console.WriteLine("Received all file.");
                                    }
                                }
                                // Server received
                                OnServerDataReceived(new AsyncCompletedEventArgs(null, false, rcvData));
                            }
                        }
                        else
                        {
                            // Send null message to client and server so they can detect disconnection
                            OnClientDataReceived(new AsyncCompletedEventArgs(null, false, rcvData));
                            OnServerDataReceived(new AsyncCompletedEventArgs(null, false, rcvData));
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }
            catch (TaskCanceledException tc)
            {
                looper.LoopDone = true;
                System.Diagnostics.Debug.WriteLine("receive Task Cancelled: " + tc.Message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Receive Exception: " + ex.Message);
            }
        }

    }

    public class ReceiveData
    {
        public long clientHandle;
        public MessageData clientData;
    }
}
