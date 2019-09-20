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
    public class ThreadedSender : ThreadedBase, ISend
    {
        IDataGetter dataGetter = null;

        public ThreadedSender()
            : base()
        {
            dataGetter = new DefaultDataGetter();
            StartParam(new ParameterizedThreadStart(SendLoop));
        }

        public ThreadedSender(IDataGetter getter)
        {
            dataGetter = getter;
            StartParam(new ParameterizedThreadStart(SendLoop));
        }

        public ThreadedSender(Func<object, Result> func)
            : base()
        {
            InitStart(func);
        }

        public Thread SendThread
        {
            get { return theThread; }
            private set { theThread = value; }
        }

        public IDataGetter DataGetter
        {
            get { return dataGetter; }
            private set { dataGetter = value; }
        }

        public Result ResultLoop(object arg)
        {
            Result response = Result.Ok();
            SendLoop(arg);
            return response;
        }

        public void SendLoop(object arg)
        {
            Client client = arg as Client;
            if ((client != null) && (client.ClientSocket != null))
            {
                while (!looper.LoopDone)
                {
                    try
                    {
                        var eventData = GetDataAsync.GetMessageDataAsync(dataGetter, client.ClientHandle);

                        if (eventData != null)
                        {
                            if ((eventData.Result.id > 0) && (eventData.Result.message != null))
                            {
                                byte[] buffer = ClientData<MessageData>.SerializeToByteArray<MessageData>(eventData.Result);
                                var res = TcpLibExtensions.SendBufferAsync(client.ClientSocket, buffer, 0, buffer.Length, SocketFlags.None);
                                if (res.IsFaulted || (res.Result == null) || res.Result.Failure)
                                {
                                    // Just close the socket.  Look into trying to reconnect
                                    Console.WriteLine("Send Fault. Closing socket {0} to client.", client.ClientSocket.Handle);
                                    ClientStore.RemoveClient((long)client.ClientSocket.Handle);
                                }
                            }

                            // Data sent.  Clear out buffer
                            client.ClearData();
                        }
                    }
                    catch (Exception e)
                    {
                        //Console.WriteLine("Send Exception: " + e.Message);
                    }
                }
                Console.WriteLine("Send Loop Exiting...");
            }
        }
    }
}
