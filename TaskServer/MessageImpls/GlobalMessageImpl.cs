using CliServLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcpLib;

namespace TaskServer
{
    public class GlobalMessageImpl : IMessageImpl
    {
        MessageServer _server;

        public bool PerformAction(Client client, MessageData messageData)
        {
            bool retVal = true;

            try
            {
                string temp = (string)messageData.message;
                messageData.message = String.Format("[{0}] says \'{1}\'.", messageData.name, temp);
                HandleGlobalMessageSendAsync(client, messageData);
            }
            catch (Exception e)
            {
                Console.WriteLine("Global Message Exception: " + e.Message);
                retVal = false;
            }

            return retVal;
        }

        public void SetActionData(object data)
        {
            _server = data as MessageServer;
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
                    var res = CliServLib.MessageServer.SendMessageAsync(curClient, messageData);
                    if (res.Result.Failure)
                    {
                        Console.WriteLine("There is a problem sending data out to the client.");
                    }
                }
            });
        }

    }
}
