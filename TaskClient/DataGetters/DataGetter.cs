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
        bool _greeting = false;

        public MessageData GetData()
        {
            Console.WriteLine("Enter a Message to Send: ");
            MessageData messageData = new MessageData();
            string message = Console.ReadLine();
            messageData.message = message;
            if (_greeting)
            {
                messageData.id = 10;
            }
            else
            {
                messageData.id = 1;
            }
            return messageData;
        }

        public MessageData GetData(long handle)
        {
            Console.WriteLine("Enter a Message to Send: ");
            MessageData messageData = new MessageData();
            string message = Console.ReadLine();
            messageData.message = message;
            if (_greeting)
            {
                messageData.id = 10;
            }
            else
            {
                messageData.id = 1;
            }
            messageData.handle = handle;
            return messageData;
        }

        public void SetData(object data)
        {
            _greeting = (bool)data;
        }
    }
}
