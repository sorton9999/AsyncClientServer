﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcpLib;

namespace TaskClient
{
    public class DataGetter : IDataGetter
    {
        public MessageData GetData()
        {
            Console.WriteLine("Enter a Message to Send: ");
            MessageData messageData = new MessageData();
            string message = Console.ReadLine();
            messageData.message = message;
            messageData.id = 1;
            return messageData;
        }

        public MessageData GetData(long handle)
        {
            Console.WriteLine("Enter a Message to Send: ");
            MessageData messageData = new MessageData();
            string message = Console.ReadLine();
            messageData.message = message;
            messageData.id = 1;
            messageData.handle = handle;
            return messageData;
        }

        public void SetData(object data)
        {

        }
    }
}
