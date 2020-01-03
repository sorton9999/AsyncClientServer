using CliServLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TcpLib;


namespace TaskServer
{

    public class TaskServer
    {
        // Listener
        private CliServLib.ThreadedListener listenerThread = new CliServLib.ThreadedListener();

        // Container of Clients
        CliServLib.ClientStore clients;

        // Are we done?
        bool done = false;


        public TaskServer()
        {
            ThreadedReceiver.ServerDataReceived += ThreadedReceiver_ServerDataReceived;
            listenerThread.OnClientConnect += ListenerThread_OnClientConnect;
            clients = new CliServLib.ClientStore();
            listenerThread.Run(clients);
        }

        private void ListenerThread_OnClientConnect(long handle)
        {
            Console.WriteLine("Client " + handle + " connected.");
        }

        private void ThreadedReceiver_ServerDataReceived(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                ReceiveData data = e.UserState as ReceiveData;
                if (data != null)
                {
                    MessageData messageData = data.clientData;
                    Client client = ClientStore.FindClient(data.clientHandle);
                    if (messageData != null && messageData.id > 0)
                    {
                        Console.WriteLine("received Message Type: {0}", messageData.id);
                        if (client != null)
                        {
                            Console.WriteLine("\tFrom Client: {0}", data.clientHandle);

                            // It's a string message, so just print it out for now.
                            Console.WriteLine("[{0}]: {1} ", messageData.name, messageData.message);
                        }

                        switch (messageData.id)
                        {
                            case 1:
                                // Send message to all users
                                HandleGlobalMessageSend(client, messageData);
                                break;
                            case 2:
                                // Send message to specific user
                                HandleUserMessageSend(client, messageData);
                                break;
                            case 3:
                                // Get all user names and send to asking client
                                GetAllUsers(client, messageData);
                                break;
                            default:
                                Console.WriteLine("Unsupported Message Type: " + messageData.id);
                                break;
                        }
                    }
                }
            }
        }

        private void HandleGlobalMessageSend(Client client, MessageData messageData)
        {
            Console.WriteLine("Handling Global Send.");
            Client curClient = null;
            while ((curClient = ClientStore.NextClient()) != null)
            {
                if (curClient.ClientHandle == client.ClientHandle)
                {
                    continue;
                }
                var res = SendMessageAsync(curClient, messageData);
                if (res.Result.Failure)
                {
                    Console.WriteLine("There is a problem sending data out to the client.");
                }
            }
        }

        private void HandleUserMessageSend(Client client, MessageData messageData)
        {
            Console.WriteLine("Handling Specific User Send.");
        }

        private void GetAllUsers(Client client, MessageData messageData)
        {
            Console.WriteLine("Handling Get All Users.");
        }

        //public void ReceiveData(MessageData data)
        //{
        //    Console.WriteLine("Received Message of Type: {0}", data.id);
        //}
        public async Task<Result<string>> SendMessageAsync(Client client, object message)
        {
            // Encode a string message before sending it to the server
            var messageData = ClientData<MessageData>.SerializeToByteArray(message);

            // Send it away
            var sendResult =
                await client.ClientSocket.SendWithTimeoutAsync(
                    messageData,
                    0,
                    messageData.Length,
                    0,
                    SendTypeEnum.SendTypeCycle,
                    CliServDefaults.SendTimeoutMs
                )
                .ConfigureAwait(false);

            // If Task did not complete successfully, report the error
            if (sendResult.Failure)
            {
                return Result.Fail<string>("There was an error sending data to the server");
            }
            // Sent
            return Result.Ok("Message sent.");
        }

        public bool ServerIsDone
        {
            get { return done; }
            set
            {
                done = value;
                if (done)
                {
                    listenerThread.StopLoopAction.Invoke();
                }
            }
        }

        public static void ClientReceiveThread(object sender, AsyncCompletedEventArgs e)
        {
            // This is a problem without a GUI thread.  There is no easy way to check to
            // see if the Invoke is possible so it's commented out as a placeholder.
            //if (this.InvokeRequired)
            //{
            //    BeginInvoke(new AsyncCompletedEventHandler(ClientReceiveThread),
            //        new object[] { sender, e });
            //}
            //else
            //{
                if (e.Error == null)
                {
                    //string message = e.UserState as string;
                    MessageData message = e.UserState as MessageData;
                    if (message != null)
                    {
                        Console.WriteLine("[{0}]: {1}", message.name, message.message);
                    }
                }
            //}
        }

        public bool ClientsAllDone()
        {
            return CliServLib.ClientStore.ClientsAllDone();
        }

        public void RemoveAllClients()
        {
            CliServLib.ClientStore.RemoveAllClients();
        }
    }
}
