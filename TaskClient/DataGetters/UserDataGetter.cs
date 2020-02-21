using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcpLib;

namespace TaskClient
{
    public class UserDataGetter : IDataGetter
    {
        public MessageData GetData()
        {
            Console.Write("Who Do You Want to Send a Message To? ");
            string who = Console.ReadLine();
            Console.WriteLine("Enter a Message to Send: ");
            MessageData messageData = new MessageData();
            string message = Console.ReadLine();
            messageData.message = who + ":" + message;
            messageData.id = 2;
            return messageData;
        }

        public MessageData GetData(long handle)
        {
            Console.Write("Who Do You Want to Send a Message To? ");
            string who = Console.ReadLine();
            Console.WriteLine("Enter a Message to Send: ");
            MessageData messageData = new MessageData();
            string message = Console.ReadLine();
            messageData.message = who + ":" + message;
            messageData.id = 2;
            messageData.handle = handle;
            return messageData;
        }

        public void SetData(object data)
        {

        }
    }
}
