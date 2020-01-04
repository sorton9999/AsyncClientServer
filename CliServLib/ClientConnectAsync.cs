using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TcpLib;

namespace CliServLib
{
    public class ClientConnectAsync
    {
        public delegate void ConnectDelegate(Socket socket);
        public static event ConnectDelegate OnConnect;

        AddressFamily _addressFamily = AddressFamily.InterNetwork;
        SocketType _socketType = SocketType.Stream;
        ProtocolType _protocolType = ProtocolType.Tcp;

        public ClientConnectAsync()
        {

        }

        public ClientConnectAsync(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
        {
            _addressFamily = addressFamily;
            _socketType = socketType;
            _protocolType = protocolType;
        }

        public async Task<Result<Socket>> ConnectAsync(int port = 0, string address = null, int timeoutMs = 3000, int maxCycles = -1)
        {
            Socket clientSocket = new Socket(_addressFamily, _socketType, _protocolType);

            var serverPort = ((port > 0) ? port : CliServDefaults.DfltPort);
            string _ip = address;
            if (String.IsNullOrEmpty(_ip))
            {
                var ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());

                var ipAddress =
                ipHostInfo.AddressList
                    .Select(ip => ip)
                    .FirstOrDefault(ip => ip.AddressFamily == _addressFamily);
                address = ipAddress.ToString();
            }
            //else
            //{
            //    return Result.Fail<Socket>("Empty IP Address");
            //}

            ListenTypeEnum lType = (maxCycles > 0 ? ListenTypeEnum.ListenTypeCycle : ListenTypeEnum.ListenTypeDelay);
            //System.Threading.Thread.Sleep(1000);
            try
            {
                var connectResult =
                    await clientSocket.ConnectWithTimeoutAsync(
                        address,
                        serverPort,
                        lType,
                        timeoutMs)
                    .ConfigureAwait(false);

                if (connectResult.Success)
                {
                    System.Diagnostics.Debug.WriteLine("We're good.  Returning Socket.");

                    // Notify caller of connection
                    OnConnect?.Invoke(connectResult.Value);
                }
                else
                {
                    Console.WriteLine("Connection Error: " + connectResult.Error);
                }
                return connectResult;
            }
            catch (TimeoutException t)
            {
                System.Console.WriteLine("Timeout Exception: " + t.Message);
                return Result.Fail<Socket>("Connection Timeout." + t.Message);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("We failed to connect.");
                return Result.Fail<Socket>("Connection Failure: " + e.Message);
            }
        }
    }
}
