using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcpLib;

namespace CliServLib
{
    public class MessageHandler : MessageHandlerBase
    {

        public MessageHandler()
        {
        }

        public override bool HandleMessage(Client client, MessageData messageData, IMessageImpl action)
        {
            return (action.PerformAction(client, messageData));
        }

        public override void SetHandlerData(object data, IMessageImpl action)
        {
            action.SetActionData(data);
        }
    }
}
