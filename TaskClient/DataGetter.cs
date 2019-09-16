using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcpLib;

namespace TaskClient
{
    public class DataGetter : IDataGetter
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
