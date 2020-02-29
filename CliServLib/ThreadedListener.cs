using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TcpLib;

namespace CliServLib
{
    public class ThreadedListener : ThreadedBase, IListen
    {
        public delegate void ConnectionDel(Client client);
        public event ConnectionDel OnClientConnect;
        bool loopDone = false;
        Socket _listenSocket;
        IDataGetter dataGetter;

        public ThreadedListener()
            : base()
        {
            dataGetter = new DefaultDataGetter();
            StartParam(new ParameterizedThreadStart(ListenLoop));
        }

        public ThreadedListener(IDataGetter getter)
        {
            dataGetter = getter;
            StartParam(new ParameterizedThreadStart(ListenLoop));
        }

        public void ListenLoop(object arg)
        {
            ClientStore clients = arg as ClientStore;
            if (clients != null)
            {
                while (!looper.LoopDone)
                {
                    try
                    {
                        var res = ServerListenAsync();
                        if (res.IsFaulted || (res.Result == null))
                        {
                            Console.WriteLine("Problem with connection.");
                        }
                        else
                        {
                            Console.WriteLine("Accepted Client Connection on port: {0}", CliServDefaults.DfltPort);
                            Client client = new Client(res.Result, CliServDefaults.BufferSize, dataGetter);
                            try
                            {
                                // Start the service loops
                                client.Start();
                                ClientStore.AddClient(client, client.ClientHandle);
                                OnClientConnect?.Invoke(client);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Client Add to List Exception: {0}", e.Message);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
                if (_listenSocket.Connected)
                {
                    _listenSocket.Shutdown(SocketShutdown.Both);
                    _listenSocket.Close();
                }
                Console.WriteLine("Listener Done.");
            }
        }

        public async Task<Socket> ServerListenAsync()
        {
            using (_listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {

                var serverPort = CliServLib.CliServDefaults.DfltPort;
                var ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());

                var ipAddress =
                ipHostInfo.AddressList.Select(ip => ip)
                    .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);

                var ipEndPoint = new IPEndPoint(ipAddress, serverPort);

                Console.WriteLine("Listening IP: {0}, Port: {1}", ipAddress.ToString(), serverPort);

                // Bind a socket to a local TCP port and Listen for incoming connections
                _listenSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _listenSocket.Bind(ipEndPoint);
                _listenSocket.Listen(5);

                // Create a Task and accept the next incoming connection (ServerAcceptTask)
                // NOTE: This call is not awaited so the method continues executing
                var acceptTask = Task.Run(AcceptConnectionTaskAsync);

                // Await the result of the ServerAcceptTask
                var acceptResult = await acceptTask.ConfigureAwait(false);

                if (acceptResult.Failure)
                {
                    Console.WriteLine("There was a problem with accepting client connection");
                    return null;
                }
                return acceptResult.Value;
            }
        }
        private async Task<Result<Socket>> AcceptConnectionTaskAsync()
        {
            Console.WriteLine("Waiting to Accept Connection from a Client...");
            return await _listenSocket.AcceptAsync(CancelSource.Token).ConfigureAwait(false);
        }

    }
}
