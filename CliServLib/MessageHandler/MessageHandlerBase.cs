using TcpLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CliServLib
{
    public abstract class MessageHandlerBase : IMessageHandler
    {
        //private object lockObj = new object();

        protected MessageHandlerBase()
        {
        }

        public bool Handle(Client client, MessageData message, IMessageImpl action, object handlerArgs)
        {
            if (client == null || message == null)
            {
                return false;
            }
            bool retVal = false;
            //lock (lockObj)
            //{
                //IMessageHandler handler = GetMessageHandler();
                //if (handler != default(IMessageHandler))
                //{
                    SetHandlerData(handlerArgs, action);
                    retVal = HandleMessage(client, message, action);
                //}
            //}
            return retVal;
        }

        abstract public bool HandleMessage(Client client, MessageData messageData, IMessageImpl action);

        abstract public void SetHandlerData(object data, IMessageImpl action);


        private IMessageHandler GetMessageHandler()
        {
            return new MessageHandler();
        }
    }
}
