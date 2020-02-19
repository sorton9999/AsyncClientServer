using CliServLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcpLib;

namespace TaskCommon
{
    public interface IMessageHandler
    {
        bool HandleMessage(Client client, MessageData messageData, IMessageImpl action);

        void SetHandlerData(object data, IMessageImpl action);
    }
}
