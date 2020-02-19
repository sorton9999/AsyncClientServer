using CliServLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TcpLib;
using TaskCommon;


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

        // We are receiving a file
        bool receivingFile = false;

        // Byte array copy offset
        int offset = 0;

        // The size of the received file
        long fileSize = 0;

        // Total bytes received
        private long totalRcv = 0;

        // The byte stream of file data
        private byte[] fileData = null;

        // The name of the received file
        private string fileName = String.Empty;

        // Receive file data as a byte stream
        private MemoryStream memStream = null;
        private BinaryWriter binWriter = null;

        private const string FILE_DIR = "TempFiles";

        private string filesPath = String.Empty;

        private MessageHandler messageHandler = new MessageHandler();
        private FileMessageImpl fileImp = new FileMessageImpl();


        public TaskServer()
        {
            // For any file transfers, put them in a known default location
            try
            {
                filesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                filesPath += "\\" + FILE_DIR + "\\";
                System.IO.Directory.CreateDirectory(filesPath);
            }
            catch (Exception e)
            {
                throw e;
            }
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
                            Console.WriteLine("[{0}]: {1} ", messageData.name, ((messageData.message is string) ? messageData.message : ((messageData.message as byte[]).Length + " bytes")));
                        }

                        MessageTypesEnum msgType = MessageTypesEnum.MSG_TYPE_UNINIT;
                        bool success = Enum.TryParse(messageData.id.ToString(), out msgType);

                        if (success)
                        {

                            switch (msgType)
                            {
                                case MessageTypesEnum.CLIENT_EXIT_MSG_TYPE:
                                    // Client Exit
                                    HandleClientExit(client, messageData);
                                    break;
                                case MessageTypesEnum.GLOBAL_MSG_TYPE:
                                    // Send message to all users
                                    string temp = (string)messageData.message;
                                    messageData.message = String.Format("[{0}] says \'{1}\'.", messageData.name, temp);
                                    HandleGlobalMessageSendAsync(client, messageData);
                                    break;
                                case MessageTypesEnum.USER_MSG_TYPE:
                                    // Send message to specific user
                                    HandleUserMessageSendAsync(client, messageData);
                                    break;
                                case MessageTypesEnum.ALL_USERS_MSG_TYPE:
                                    // Get all user names and send to asking client
                                    GetAllUsersAsync(client, messageData);
                                    break;
                                case MessageTypesEnum.GET_USERS_MSG_TYPE:
                                    HandleGetUserName(client, messageData);
                                    break;
                                case MessageTypesEnum.FILE_MSG_TYPE:
                                    messageHandler.Handle(client, messageData, fileImp, null);
                                    //HandleFile(client, messageData);
                                    break;
                                default:
                                    Console.WriteLine("Unsupported Message Type: " + messageData.id);
                                    break;
                            }
                        }
                    }
                }
            }
        }

        private void HandleFile(Client client, MessageData messageData)
        {
            if (receivingFile)
            {
                byte[] fData = new byte[messageData.length];
                Buffer.BlockCopy((byte[])messageData.message, offset, fData, 0, (int)messageData.length);
                totalRcv += fData.Length;
                binWriter.Write(fData);
                if (totalRcv >= fileSize)
                {
                    receivingFile = false;
                    if (fileSize == (fileData.Length - 28))
                    {
                        Console.WriteLine("Received File: " + fileName);
                    }
                    else
                    {
                        Console.WriteLine("Send File Mismatch - Expect: [{0}], Actual: [{1}]", fileSize, totalRcv);
                    }
                    try
                    {
                        //string path = "C:\\Users\\steve\\test\\";
                        string path = filesPath;
                        using (FileStream fsStream = new FileStream(path + fileName, FileMode.Create))
                        using (BinaryWriter writer = new BinaryWriter(fsStream, Encoding.UTF8))
                        {
                            writer.Write(fileData, 27, fileData.Length - 28);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Exception caught in process: {0}", ex);
                    }

                }
                else
                {
                    Console.WriteLine("Total Received [{0}] out of [{1}]", totalRcv, fileSize);
                }
            }
            else
            {
                Console.WriteLine("Receiving File: " + messageData.message);
                Console.WriteLine("Size: " + messageData.length);
                receivingFile = true;
                fileName = (string)messageData.message;
                fileSize = messageData.length;
                totalRcv = 0;
                fileData = new byte[fileSize + 28];
                memStream = new MemoryStream(fileData);
                binWriter = new BinaryWriter(memStream);
                memStream.Position = 0;
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
                HandleGlobalMessageSendAsync(client, msg);
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
                HandleGlobalMessageSendAsync(client, msg);
            }
            catch (Exception e)
            {
                Console.WriteLine("Client already registered a Name?: " + e.Message);
            }
        }

        private async void HandleGlobalMessageSendAsync(Client client, MessageData messageData)
        {
            Console.WriteLine("Handling Global Send.");
            Client curClient = null;
            // Must do the operation async because an action is performed on each
            // client.  This may take a long time if there are many clients.
            await Task.Factory.StartNew(() =>
           {
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
           });
        }

        private async void HandleUserMessageSendAsync(Client client, MessageData messageData)
        {
            Console.WriteLine("Handling Specific User Send.");
            // Must do the operation async because a search is done for a specific
            // client. This may take a long time if there are many clients.
            await Task.Factory.StartNew(() =>
           {
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
                           string msg = String.Format("This name [{0}] is not registered", name);
                           Console.WriteLine(msg);

                           // Send back to original sender
                           MessageData send = new MessageData();
                           send.handle = client.ClientHandle;
                           send.id = messageData.id;
                           send.message = msg;
                           send.name = messageData.name;
                           send.response = true;
                           var res = SendMessageAsync(client, send);
                           if (res.Result.Failure)
                           {
                               Console.WriteLine("There is a problem sending data out to specific user.");
                           }
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
           });
        }

        private async void GetAllUsersAsync(Client client, MessageData messageData)
        {
            Console.WriteLine("Handling Get All Users.");
            // This is async since it may take some time to gather all the
            // client names if the list is large.
            await Task.Factory.StartNew(() =>
           {
               MessageData sendMsg = new MessageData();
               sendMsg.handle = client.ClientHandle;
               sendMsg.id = messageData.id;
               sendMsg.name = messageData.name;
               sendMsg.response = false;
               StringBuilder buffer = new StringBuilder();
               buffer.AppendLine("Users:");
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
           });
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
