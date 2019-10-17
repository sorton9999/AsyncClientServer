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

        private static long[] keys;
        private static int curIdx = -1;

        public static void AddClient(Client client, long handle)
        {
            clientStore.Add(handle, client);
            keys = clientStore.Keys.ToArray();
        }

        public static bool RemoveClient(long handle)
        {
            Client client = FindClient(handle);
            if (client != default(Client))
            {
                client.Stop();
                //client.Dispose();
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

        public static Client NextClient()
        {
            ++curIdx;
            if (curIdx >= keys.Count())
            {
                curIdx = -1;
                return null;
            }
            return clientStore[keys[curIdx]];
        }

        public static void RemoveAllClients()
        {
            foreach (var client in clientStore.ToList())
            {
                client.Value.Stop();
                //client.Value.Dispose();
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

        public static Client FindClient(long id)
        {
            return (clientStore.FirstOrDefault((t) => (t.Key == id)).Value);
        }

    }
}
