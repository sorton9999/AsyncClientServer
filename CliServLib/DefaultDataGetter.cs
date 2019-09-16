using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcpLib;

namespace CliServLib
{
    public class DefaultDataGetter : IDataGetter
    {
        public MessageData GetData(int msgType)
        {
            MessageData messageData = new MessageData();
            string message = Console.ReadLine();
            messageData.message = message;
            return messageData;
        }
    }
}
