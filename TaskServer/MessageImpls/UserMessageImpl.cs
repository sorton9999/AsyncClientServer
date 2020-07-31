using CliServLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcpLib;

namespace TaskServer
{
    public class UserMessageImpl : IMessageImpl
    {
        TaskServer _server = null;

        public bool PerformAction(Client client, MessageData messageData)
        {
            bool retVal = true;

            try
            {
                HandleUserMessageSendAsync(client, messageData);
            }
            catch (Exception e)
            {
                Console.WriteLine("User Message Exception: " + e.Message);
                retVal = false;
            }

            return retVal;
        }

        public void SetActionData(object data)
        {
            _server = data as TaskServer;
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
                        if (!_server.ClientHandleToUserName.ContainsValue(name))
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
                            var res = TaskServer.SendMessageAsync(client, send);
                            if (res.Result.Failure)
                            {
                                Console.WriteLine("There is a problem sending data out to specific user.");
                            }
                        }
                        var myKey = _server.ClientHandleToUserName.FirstOrDefault(x => x.Value == name).Key;
                        Client found = ClientStore.FindClient(myKey);
                        if (found != null)
                        {
                            MessageData sendMsg = new MessageData();
                            sendMsg.handle = client.ClientHandle;
                            sendMsg.id = messageData.id;
                            sendMsg.message = String.Format("[{0}] says \'{1}\'", messageData.name, message);
                            sendMsg.name = name;
                            sendMsg.response = false;
                            var res = TaskServer.SendMessageAsync(found, sendMsg);
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
    }
}
