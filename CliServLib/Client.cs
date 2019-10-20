using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TcpLib;

namespace CliServLib
{
    public class Client : IDisposable
    {
        private bool disposed = false;

        Socket clientSocket = null;
        ServiceController<MessageData> controller = null;
        CancellationTokenSource cancelSource = new CancellationTokenSource();

        IDataGetter dataGetter = null;

        public Client(Socket socket, int dataSize)
        {
            ClientSocket = socket;
            controller = new ServiceController<MessageData>(socket, dataSize);

            dataGetter = new DefaultDataGetter();
        }

        public Client(Socket socket, int dataSize, IDataGetter dataGetter)
        {
            ClientSocket = socket;
            controller = new ServiceController<MessageData>(socket, dataSize);

            this.dataGetter = dataGetter;
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

        public ThreadedReceiver Receiver
        {
            get { return controller.Receiver; }
        }

        public ThreadedSender Sender
        {
            get { return controller.Sender; }
        }

        public CancellationTokenSource CancelSource
        {
            get { return cancelSource; }
            set { cancelSource = value; }
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

        public IDataGetter DataGetter
        {
            get { return dataGetter; }
            set { dataGetter = value; }
        }

        public void Start()
        {
            controller.StartController(this);
        }

        public bool Stop()
        {
            CancelSource.Cancel();
            SetMeFreeAsync();
            return controller.StopController();
        }

        /// <summary>
        /// This method should only be called when exiting.
        /// Send 1 byte of data to the SendAsync method with the cancellation token so it
        /// will return from the blocked call and cancel.  This will make the send loop
        /// exit for a graceful stop.  
        /// </summary>
        private async void SetMeFreeAsync()
        {
            // This method is blocked in the ThreadedSender loop so call this
            // to allow it to return and then cancel.
            await TcpLib.TcpLibExtensions.SendBufferAsync(ClientSocket, new byte[1], 0, 1, SocketFlags.None, CancelSource.Token);
        }

        public void ClearData()
        {
            controller.ClientData().ClearData();
        }

        public byte[] ClientData()
        {
            return controller.ClientData().Data;
        }

        public void SetData(object data)
        {
            DataGetter.SetData(data);
        }

        public void SetData(object data, IDataGetter getter)
        {
            DataGetter = getter;
            DataGetter.SetData(data);
        }

    }
}
