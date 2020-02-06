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

        // Store User Names associated with its client handle
        private Dictionary<long, string> ClientHandleToUserName = new Dictionary<long, string>();

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

        private void ListenerThread_OnClientConnect(Client client)
        {
            Console.WriteLine("Client " + client.ClientHandle + " connected.");
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
                            case 99:
                                // Client Exit
                                HandleClientExit(client, messageData);
                                break;
                            case 1:
                                // Send message to all users
                                string temp = (string)messageData.message;
                                messageData.message = String.Format("[{0}] says \'{1}\'.", messageData.name, temp);
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
                            case 10:
                                HandleGetUserName(client, messageData);
                                break;
                            default:
                                Console.WriteLine("Unsupported Message Type: " + messageData.id);
                                break;
                        }
                    }
                }
            }
        }

        private void HandleClientExit(Client client, MessageData messageData)
        {
            try
            {
                ClientHandleToUserName.Remove(client.ClientHandle);
                MessageData msg = new MessageData();
                msg.handle = messageData.handle;
                msg.id = 1;
                msg.name = messageData.name;
                msg.response = false;
                msg.message = String.Format("[{0}] has left.", messageData.name);
                HandleGlobalMessageSend(client, msg);
                ClientStore.RemoveClient(client.ClientHandle);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Client Handle Remove Exception: " + ex.Message);
            }
        }

        private void HandleGetUserName(Client client, MessageData messageData)
        {
            Console.WriteLine("Handle User Name Register.");

            try
            {
                // Register the name
                ClientHandleToUserName.Add(client.ClientHandle, (string)messageData.name);

                MessageData msg = new MessageData();
                msg.handle = messageData.handle;
                msg.id = 1;
                msg.name = messageData.name;
                msg.response = false;
                msg.message = String.Format("[{0}] has joined and says \'{1}\'.", messageData.name, messageData.message);
                HandleGlobalMessageSend(client, msg);
            }
            catch (Exception e)
            {
                Console.WriteLine("Client already registered a Name?: " + e.Message);
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
            // Do a reverse lookup for name of client to send message to
            string name = String.Empty;
            string message = String.Empty;
            string[] parts = (messageData.message as string).Split(':');
            if (parts.Length > 1)
            {
                name = parts[0].Trim();
                message = parts[1].Trim();
            }
            if (!String.IsNullOrEmpty(name))
            {
                try
                {
                    if (!ClientHandleToUserName.ContainsValue(name))
                    {
                        Console.WriteLine("This name [{0}] is not registered", name);
                    }
                    var myKey = ClientHandleToUserName.FirstOrDefault(x => x.Value == name).Key;
                    Client found = ClientStore.FindClient(myKey);
                    if (found != null)
                    {
                        MessageData sendMsg = new MessageData();
                        sendMsg.handle = client.ClientHandle;
                        sendMsg.id = messageData.id;
                        sendMsg.message = String.Format("[{0}] says \'{1}\'", messageData.name, message);
                        sendMsg.name = name;
                        sendMsg.response = false;
                        var res = SendMessageAsync(found, sendMsg);
                        if (res.Result.Failure)
                        {
                            Console.WriteLine("There is a problem sending data out to specific user.");
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Message Send Exception: " + e.Message);
                }
            }
        }

        private void GetAllUsers(Client client, MessageData messageData)
        {
            Console.WriteLine("Handling Get All Users.");
            MessageData sendMsg = new MessageData();
            sendMsg.handle = client.ClientHandle;
            sendMsg.id = messageData.id;
            sendMsg.name = messageData.name;
            sendMsg.response = false;
            StringBuilder buffer = new StringBuilder();
            buffer.AppendLine("Users:");
            var keys = ClientHandleToUserName.Keys;
            foreach (var item in ClientHandleToUserName)
            {
                buffer.AppendFormat("[{0}] ", item.Value);
            }
            sendMsg.message = buffer.ToString();

            var res = SendMessageAsync(client, sendMsg);
            if (res.Result.Failure)
            {
                Console.WriteLine("There is a problem sending data out to specific user.");
            }
        }

        private void GetUserName(Client client)
        {
            MessageData data = new MessageData();
            data.id = 10;
            data.message = "Get User Name";
            data.response = false;
            data.handle = client.ClientHandle;

            var res = SendMessageAsync(client, data);
            if (res.IsFaulted || res.IsCanceled || res.Result.Failure)
            {
                Console.WriteLine("Send Name Message Failed: " + res.Result.Error);
            }
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
