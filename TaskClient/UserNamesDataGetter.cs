using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcpLib;

namespace TaskClient
{
    public class UserNamesDataGetter : IDataGetter
    {
        public MessageData GetData()
        {
            MessageData messageData = new MessageData();
            messageData.message = "I want all the names.";
            messageData.id = 3;
            return messageData;
        }

        public MessageData GetData(long handle)
        {
            MessageData messageData = new MessageData();
            messageData.message = "I want all the names.";
            messageData.id = 3;
            messageData.handle = handle;
            return messageData;
        }

        public void SetData(object data)
        {

        }
    }
}
