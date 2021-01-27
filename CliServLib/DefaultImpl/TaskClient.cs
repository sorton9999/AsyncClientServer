using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcpLib;

namespace CliServLib.DefaultImpl
{
    public class TaskClient
    {
        MessageClient msgClient = null;

        bool _done = false;

        public TaskClient(string ip, int port, string name)
        {
            msgClient = new MessageClient(ip, port, name);
            msgClient.MessageFactory = new DefaultMessageFactory();
            msgClient.MyClient.DataGetter = new DefaultDataGetter();
        }

        public MessageClient InternMsgClient
        {
            get { return msgClient; }
            private set { msgClient = value; }
        }

        public void Start()
        {
            msgClient.Start();
            //StartLoop();
        }

        public void PrintMenu()
        {
            Console.WriteLine("Default Actions Menu");
            Console.WriteLine("[1]   Send Message to Server");
            Console.WriteLine("[Q|q] Quit");
            Console.Write("What Do You Want to Do? --> ");
        }

        private void StartLoop()
        {
            while (!_done)
            {
                DefaultDataGetter getter = new DefaultDataGetter();
                var eventData = GetDataAsync.GetMessageDataAsync(getter, InternMsgClient.MyClient.ClientHandle);

                if (eventData == null || eventData.Result == null || eventData.Result.id <= 0)
                {
                    Console.WriteLine("Invalid Event.  Nothing done.");
                }

                /*
                PrintMenu();
                string ans = Console.ReadLine();
                if (ans == "q")
                {
                    _done = true;
                }
                if (!_done)
                {
                    int id = Convert.ToInt32(ans);
                    Console.WriteLine("What do you want to say?");
                    string message = Console.ReadLine();
                    MessageData msg = new MessageData();
                    msg.id = id;
                    msg.message = message;
                    msg.handle = InternMsgClient.MyClient.ClientHandle;
                    msg.name = "Tootsie";

                }
                */
            }
        }
    }
}
