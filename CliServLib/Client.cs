using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TcpLib;

namespace CliServLib
{
    public class Client : IDisposable
    {
        private bool disposed = false;

        Socket clientSocket = null;
        ServiceController<MessageData> controller = null;


        public Client(Socket socket, int dataSize)
        {
            ClientSocket = socket;
            controller = new ServiceController<MessageData>(socket, dataSize);
        }

        public Client(Socket socket, int dataSize, IDataGetter dataGetter)
        {
            ClientSocket = socket;
            controller = new ServiceController<MessageData>(socket, dataSize, dataGetter);
        }

        public Socket ClientSocket
        {
            get { return clientSocket; }
            private set { clientSocket = value; }
        }

        public long ClientHandle
        {
            get { return (long)clientSocket.Handle; }
        }

        public bool ClientDone
        {
            get { return controller.ClientDone(); }
        }

        public int DataSize
        {
            get { return controller.ClientData().DataSize; }
            set { controller.ClientData().DataSize = value; }
        }

        public void Dispose()
        {
            Console.WriteLine("Disposing Controller for Client " + ClientSocket.Handle);
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
                controller.Dispose();
            }
            ClientSocket.Shutdown(SocketShutdown.Both);
            ClientSocket.Dispose();

            disposed = true;
        }

        public void Start()
        {
            controller.StartController(this);
        }

        public bool Stop()
        {
            return controller.StopController();
        }

        public void ClearData()
        {
            controller.ClientData().ClearData();
        }

        public byte[] ClientData()
        {
            return controller.ClientData().Data;
        }

    }
}
