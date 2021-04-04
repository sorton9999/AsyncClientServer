using CliServLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcpLib;

namespace TaskServer
{
    public class GetUserNameMessageImpl : IMessageImpl
    {
        MessageServer _server = null;

        public bool PerformAction(Client client, MessageData messageData)
        {
            bool retVal = true;

            try
            {
                HandleGetUserName(client, messageData);
            }
            catch (Exception e)
            {
                Console.WriteLine("Get User Names Message Exception: " + e.Message);
                retVal = false;
            }

            return retVal;
        }

        public void SetActionData(object data)
        {
            _server = data as MessageServer;
        }

        private void HandleGetUserName(Client client, MessageData messageData)
        {
            Console.WriteLine("Handle User Name Register.");

            try
            {
                // Register the name
                _server.ClientHandleToUserName.Add(client.ClientHandle, (string)messageData.name);

                MessageData msg = new MessageData();
                msg.handle = messageData.handle;
                msg.id = 1;
                msg.name = messageData.name;
                msg.response = false;
                msg.message = String.Format("[{0}] has joined and says \'{1}\'.", messageData.name, messageData.message);
                IMessageImpl impl = MessageImplFactory.Instance().MakeMessageImpl((int)MessageImplFactory.MessageFactoryTypesEnum.GLOBAL_MSG_TYPE, client.ClientHandle);
                if (impl != default(IMessageImpl))
                {
                    _server.MessageHandler.Handle(client, msg, impl, _server);
                    //HandleGlobalMessageSendAsync(client, msg);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Client already registered a Name?: " + e.Message);
            }
        }
    }
}
