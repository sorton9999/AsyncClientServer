using CliServLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcpLib;

namespace TaskServer
{
    public class ExitMessageImpl : IMessageImpl
    {
        TaskServer _server = null;

        public bool PerformAction(Client client, MessageData messageData)
        {
            bool retVal = false;

            try
            {
                retVal = HandleClientExit(client, messageData);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exit Message Exception: " + e.Message);
                retVal = false;
            }
            Console.WriteLine("Client [{0}] has exited [{1}]", client.ClientHandle, (retVal ? "cleanly" : "with errors"));

            return retVal;
        }

        public void SetActionData(object data)
        {
            _server = data as TaskServer;
        }

        private bool HandleClientExit(Client client, MessageData messageData)
        {
            bool handleExit = false;
            try
            {
                MessageData msg = new MessageData();
                msg.handle = messageData.handle;
                msg.id = 1;
                msg.name = messageData.name;
                msg.response = false;
                msg.message = String.Format("[{0}] has left.", messageData.name);
                IMessageImpl impl = MessageImplFactory.Instance().MakeMessageImpl(MessageTypesEnum.GLOBAL_MSG_TYPE, client.ClientHandle);
                if (impl != default(IMessageImpl))
                {
                    handleExit = _server.MessageHandler.Handle(client, msg, impl, _server);
                }
                if (handleExit)
                {
                    // Remove client handle record keeping
                    var remHandle = _server.ClientHandleToUserName.Remove(client.ClientHandle);
                    // Remove the client impls
                    var remImpl = MessageImplFactory.Instance().RemoveClient(client.ClientHandle);
                    // Remove the client object
                    var remClient = ClientStore.RemoveClient(client.ClientHandle);

                    return (remHandle && remImpl && remClient);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Client Handle Remove Exception: " + ex.Message);
                handleExit = false;
            }
            return handleExit;
        }
    }
}
