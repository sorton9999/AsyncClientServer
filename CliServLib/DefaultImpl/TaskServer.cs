using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TcpLib;


namespace CliServLib.DefaultImpl
{

    public class TaskServer
    {
        // Are we done?
        bool done = false;

        bool useLocalhost = false;

        private DefaultMessageFactory msgFactory = new DefaultMessageFactory();

        private MessageServer msgServer = null;


        public TaskServer(bool localhost)
        {
            useLocalhost = localhost;
            msgServer = new MessageServer(useLocalhost);
            msgServer.MessageFactory = msgFactory;
        }

        public MessageServer InternMsgServer
        {
            get { return msgServer; }
            private set { msgServer = value; }
        }

        public bool ClientsAllDone()
        {
            return msgServer.ClientsAllDone();
        }

        public void RemoveAllClients()
        {
            msgServer.RemoveAllClients();
        }
    }
}
