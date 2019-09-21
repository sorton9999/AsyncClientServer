using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
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
        
        public ThreadedReceiver()
           : base()
        {
            StartParam(new ParameterizedThreadStart(ReceiveLoop));
        }

        public Thread ReceiveThread
        {
            get { return theThread; }
            private set { theThread = value; }
        }

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
                    // Clear out buffer before receiving new data
                    client.ClearData();

                    var res = TcpLibExtensions.ReceiveAsync(client.ClientSocket, client.ClientData(), 0, client.DataSize, SocketFlags.None);
                    if (res.IsFaulted || (res.Result == null) || res.Result.Failure)
                    {
                        // Just close the socket.  Look into trying to reconnect
                        Console.WriteLine("Receive Fault. Closing socket {0} to client.", client.ClientSocket.Handle);
                        ClientStore.RemoveClient((long)client.ClientSocket.Handle);
                    }
                    try
                    {
                        var value = ClientData<MessageData>.DeserializeFromByteArray<MessageData>(client.ClientData());
                        rcvData.clientData = value;
                        rcvData.clientHandle = (long)client.ClientSocket.Handle;

                        // This indicates a response from the server to a client of which there could be many
                        if (value.response)
                        {
                            // Client received
                            OnClientDataReceived(new AsyncCompletedEventArgs(null, false, rcvData));
                        }
                        else
                        {
                            // Server received
                            OnServerDataReceived(new AsyncCompletedEventArgs(null, false, rcvData));
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
                Console.WriteLine("Receive Loop Exiting...");
            }
        }
    }

    public class ReceiveData
    {
        public long clientHandle;
        public MessageData clientData;
    }
}
