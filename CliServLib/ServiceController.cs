using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TcpLib;

namespace CliServLib
{
    public class ServiceController<T> : IDisposable
    {
        bool disposed = false;

        private ThreadedReceiver receiver;
        private ThreadedSender sender;
        private ClientData<T> clientData;

        public ServiceController(Socket clientSocket, int size)
        {
            clientData = new ClientData<T>(size);
            clientData.ClientSocket = clientSocket;
            if (clientSocket.Connected)
            {
                clientData.State = ClientData<T>.ClientState.CONNECTED;
            }
            sender = new ThreadedSender();
            receiver = new ThreadedReceiver();
            receiver.DataReceived += ClientStore.ClientReceiveThread;
        }

        public ServiceController(Socket cientSocket, int size, IDataGetter dataGetter)
        {
            sender = new ThreadedSender(dataGetter);
            receiver = new ThreadedReceiver();
        }

        public ServiceController(ClientData<T> client)
        {
            clientData = client;
        }

        public ClientData<T> ClientData()
        {
            return clientData;
        }

        public void Dispose()
        {
            Console.WriteLine("Disposing Controller for Client " + clientData.ClientSocket.Handle);
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                StopController();
            }
            //clientData.ClientSocket.Dispose();

            disposed = true;
        }

        public ThreadedReceiver Receiver
        {
            get { return receiver; }
            private set { receiver = value; }
        }

        public ThreadedSender Sender
        {
            get { return sender; }
            private set { sender = value; }
        }

        public void SetClient(ClientData<T> newClient)
        {
            clientData = newClient;
        }

        public void StartController(object obj)
        {
            Console.WriteLine("Starting client {0}.", ClientHandle);
            receiver.Run(obj);
            sender.Run(obj);
        }

        public bool StopController()
        {
            bool retVal = false;
            Console.WriteLine("Stopping services for Client {0}", ClientHandle);
            try
            {
                receiver.StopLoopAction.Invoke();
                sender.StopLoopAction.Invoke();
                //Result sendResponse = sender.StopLoopResult();
                clientData.ClearSocket();
                retVal = true;
            }
            catch (Exception e)
            {
                retVal = false;
                Console.WriteLine("Stopping Client Exception: ", e.Message);
            }
            return retVal;
        }

        public bool ClientDone()
        {
            return (Receiver.IsDone() && Sender.IsDone());
        }

        public long ClientHandle
        {
            get { return (long)clientData.ClientSocket.Handle; }
        }

        public Socket ClientSocket()
        {
            return clientData.ClientSocket;
        }

        public T ClientValue()
        {
            return clientData.DeserializeData();
        }

        public byte[] ClientBuffer()
        {
            return clientData.Data;
        }

    }
}
