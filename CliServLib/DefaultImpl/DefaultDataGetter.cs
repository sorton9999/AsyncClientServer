using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcpLib;

namespace CliServLib.DefaultImpl
{
    public class DefaultDataGetter : IDataGetter
    {
        public MessageData GetData()
        {
            Console.WriteLine("Enter a Message to Send: ");
            MessageData messageData = new MessageData();
            string message = Console.ReadLine();
            messageData.message = message;
            messageData.id = 1;
            messageData.name = "Client";

            return messageData;
        }

        public MessageData GetData(long id)
        {
            MessageData messageData = new MessageData();
            PrintMenu();
            string ans = Console.ReadLine();
            switch (ans)
            {
                case "q":
                case "quit":
                    messageData.message = "Exiting";
                    messageData.handle = id;
                    messageData.exitCmd = true;
                    messageData.name = "Client";
                    break;
                case "1":
                    int num = Convert.ToInt32(ans);
                    Console.WriteLine("Enter a Message to Send: ");
                    string message = Console.ReadLine();
                    messageData.message = message;
                    messageData.id = num;
                    messageData.name = "Client";
                    messageData.handle = id;
                    break;
                default:
                    Console.WriteLine("Unexpected entry.  Nothing done.");
                    break;
            }
            return messageData;
        }

        public void SetData(object data)
        {
            
        }

        public void PrintMenu()
        {
            Console.WriteLine("Default Actions Menu");
            Console.WriteLine("[1]   Send Message to Server");
            Console.WriteLine("[Q|q] Quit");
            Console.Write("What Do You Want to Do? --> ");
        }
    }
}
