using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcpLib;

namespace CliServLib
{
    public class ClientStore
    {
        private static readonly Dictionary<long, Client> clientStore = new Dictionary<long, Client>();

        public static Action<MessageData> MessageAction;

        public ClientStore(Action<MessageData> action)
        {
            MessageAction = action;
        }

        public static void AddClient(Client client, long handle)
        {
            clientStore.Add(handle, client);
        }

        public static bool RemoveClient(long handle)
        {
            Client client = FindClient(handle);
            if (client != default(Client))
            {
                client.Dispose();
            }
            return clientStore.Remove(handle);
        }

        public static bool StopClient(long handle)
        {
            bool retVal = false;
            Client client = FindClient(handle);
            if (client != default(Client))
            {
                client.Stop();
                retVal = true;
            }
            return retVal;
        }

        public static bool StartClient(long handle)
        {
            bool retVal = false;
            Client client = FindClient(handle);
            if (client != default(Client))
            {
                client.Start();
                retVal = true;
            }
            return retVal;
        }

        public static void RemoveAllClients()
        {
            foreach (var client in clientStore.ToList())
            {
                client.Value.Dispose();
                clientStore.Remove(client.Key);
            }
        }

        public static bool StopAll()
        {
            bool retVal = false;
            foreach (var client in clientStore)
            {
                retVal |= client.Value.Stop();
            }
            return retVal;
        }

        public static bool ClientsAllDone()
        {
            bool retVal = false;
            foreach (var client in clientStore)
            {
                retVal |= client.Value.ClientDone;
            }
            return retVal;
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
                if (MessageAction != null)
                {
                    MessageAction.Invoke(message);
                }
            }
            //}
        }

        public static Client FindClient(long id)
        {
            return (clientStore.FirstOrDefault((t) => (t.Key == id)).Value);
        }

    }
}
