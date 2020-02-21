using CliServLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskCommon;
using TcpLib;

namespace TaskServer
{
    public class ExitMessageImpl : IMessageImpl
    {
        TaskServer _server = null;

        public bool PerformAction(Client client, MessageData messageData)
        {
            bool retVal = true;

            try
            {
                HandleClientExit(client, messageData);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exit Message Exception: " + e.Message);
                retVal = false;
            }

            return retVal;
        }

        public void SetActionData(object data)
        {
            _server = data as TaskServer;
        }

        private void HandleClientExit(Client client, MessageData messageData)
        {
            try
            {
                _server.ClientHandleToUserName.Remove(client.ClientHandle);
                MessageData msg = new MessageData();
                msg.handle = messageData.handle;
                msg.id = 1;
                msg.name = messageData.name;
                msg.response = false;
                msg.message = String.Format("[{0}] has left.", messageData.name);
                IMessageImpl impl = MessageImplFactory.Instance().MakeMessageImpl(MessageTypesEnum.GLOBAL_MSG_TYPE);
                if (impl != default(IMessageImpl))
                {
                    _server.MessageHandler.Handle(client, msg, impl, _server);
                    //HandleGlobalMessageSendAsync(client, msg);
                }
                ClientStore.RemoveClient(client.ClientHandle);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Client Handle Remove Exception: " + ex.Message);
            }
        }
    }
}
