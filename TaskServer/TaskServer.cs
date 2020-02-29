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
        private readonly CliServLib.ThreadedListener listenerThread = new CliServLib.ThreadedListener();

        // Store User Names associated with its client handle
        private readonly Dictionary<long, string> clientHandleToUserName = new Dictionary<long, string>();

        // Container of Clients
        private CliServLib.ClientStore clients;

        // Are we done?
        bool done = false;

        // The message handler object used to perform actions using Impl objects
        private MessageHandler messageHandler = new MessageHandler();


        public TaskServer()
        {
            AllClientsRemoved = false;
            ThreadedReceiver.ServerDataReceived += ThreadedReceiver_ServerDataReceived;
            listenerThread.OnClientConnect += ListenerThread_OnClientConnect;
            clients = new CliServLib.ClientStore();
            listenerThread.Run(clients);
        }

        public bool AllClientsRemoved
        {
            get;
            private set;
        }

        public Dictionary<long, string> ClientHandleToUserName
        {
            get { return clientHandleToUserName; }
        }

        public MessageHandler MessageHandler
        {
            get { return messageHandler; }
            private set { messageHandler = value; }
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

                        MessageTypesEnum msgType;
                        bool success = Enum.TryParse(messageData.id.ToString(), out msgType);

                        if (success)
                        {
                            IMessageImpl msgImpl = MessageImplFactory.Instance().MakeMessageImpl(msgType, client.ClientHandle);

                            if (msgImpl != default(IMessageImpl))
                            {
                                messageHandler.Handle(client, messageData, msgImpl, this);
                            }
                        }
                    }
                }
            }
        }

        public static async Task<Result<string>> SendMessageAsync(Client client, object message)
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

        public bool ClientsAllDone()
        {
            return CliServLib.ClientStore.ClientsAllDone();
        }

        public void RemoveAllClients()
        {
            AllClientsRemoved = CliServLib.ClientStore.RemoveAllClients();
        }
    }
}
