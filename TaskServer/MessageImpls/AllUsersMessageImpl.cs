using CliServLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcpLib;

namespace TaskServer
{
    public class AllUsersMessageImpl : IMessageImpl
    {
        MessageServer _server = null;

        public bool PerformAction(Client client, MessageData messageData)
        {
            bool retVal = true;

            try
            {
                GetAllUsersAsync(client, messageData);
            }
            catch (Exception e)
            {
                Console.WriteLine("All Users Message Exception: " + e.Message);
                retVal = false;
            }

            return retVal;
        }

        public void SetActionData(object data)
        {
            _server = data as MessageServer;
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
                foreach (var item in _server.ClientHandleToUserName)
                {
                    if (item.Key == client.ClientHandle)
                    {
                        buffer.AppendFormat("[{0}*] ", item.Value);
                    }
                    else
                    {
                        buffer.AppendFormat("[{0}] ", item.Value);
                    }
                }
                sendMsg.message = buffer.ToString();

                var res = CliServLib.MessageServer.SendMessageAsync(client, sendMsg);
                if (res.Result.Failure)
                {
                    Console.WriteLine("There is a problem sending data out to specific user.");
                }
            });
        }
    }
}
