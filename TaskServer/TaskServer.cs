using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TcpLib;


namespace TaskServer
{

    public class TaskServer
    {
        // Listener
        private CliServLib.ThreadedListener listenerThread = new CliServLib.ThreadedListener();

        // Container of Clients
        CliServLib.ClientStore clients;

        // Are we done?
        bool done = false;


        public TaskServer()
        {
            clients = new CliServLib.ClientStore();
            listenerThread.Run(clients);
        }

        //public void ReceiveData(MessageData data)
        //{
        //    Console.WriteLine("Received Message of Type: {0}", data.id);
        //}

        public bool ServerIsDone
        {
            get { return done; }
            set
            {
                done = value;
                if (done)
                {
                    listenerThread.StopLoopAction.Invoke();
                }
            }
        }

        public static void ClientReceiveThread(object sender, AsyncCompletedEventArgs e)
        {
            // This is a problem without a GUI thread.  There is no easy way to check to
            // see if the Invoke is possible so it's commented out as a placeholder.
            //if (this.InvokeRequired)
            //{
            //    BeginInvoke(new AsyncCompletedEventHandler(ClientReceiveThread),
            //        new object[] { sender, e });
            //}
            //else
            //{
                if (e.Error == null)
                {
                    //string message = e.UserState as string;
                    MessageData message = e.UserState as MessageData;
                    if (message != null)
                    {
                        Console.WriteLine("[{0}]: {1}", message.name, message.message);
                    }
                }
            //}
        }

        public bool ClientsAllDone()
        {
            return CliServLib.ClientStore.ClientsAllDone();
        }

        public void RemoveAllClients()
        {
            CliServLib.ClientStore.RemoveAllClients();
        }
    }
}
