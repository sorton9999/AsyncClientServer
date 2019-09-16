using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace TcpLib
{

    public static partial class TcpLibExtensions
    {
        public static async Task<Result<Socket>> ConnectWithTimeoutAsync(
            this Socket socket,
            string remoteIpAddress,
            int port,
            ListenTypeEnum listenType = ListenTypeEnum.ListenTypeDelay,
            int timeoutMs = -1)
        {
            int cycles = 0;
            bool sendException = false;
            do
            {
                ++cycles;
                //Console.WriteLine("Trying to Connect {0}", cycles);
                System.Diagnostics.Debug.WriteLine("Trying to Connect {0}", cycles);
                if (listenType == ListenTypeEnum.ListenTypeDelay || (cycles > MaxCycles))
                {
                    sendException = true;
                }

                try
                {
                    System.Threading.Thread.Sleep(1000);

                    var connectTask = Task.Factory.FromAsync(
                        socket.BeginConnect,
                        socket.EndConnect,
                        remoteIpAddress,
                        port,
                        null);

                    if (connectTask == await Task.WhenAny(connectTask, Task.Delay(timeoutMs)).ConfigureAwait(false))
                    {
                        await connectTask.ConfigureAwait(false);
                    }
                    else
                    {
                        if (sendException)
                        {
                            throw new TimeoutException();
                        }
                    }
                    // We're connected here.  Break out of loop and send OK.
                    break;
                }
                catch (SocketException ex)
                {
                    if (sendException)
                        return Result.Fail<Socket>($"{ex.Message} ({ex.GetType()})");
                }
                catch (TimeoutException ex)
                {
                    if (sendException)
                        return Result.Fail<Socket>($"{ex.Message} ({ex.GetType()})");
                }
            } while (cycles <= MaxCycles);

            if (!socket.Connected)
            {
                System.Diagnostics.Debug.WriteLine("Returning Fail.");
                return Result.Fail<Socket>("There was a connection problem.");
            }
            System.Diagnostics.Debug.WriteLine("Returning OK.");
            return Result.Ok(socket);
        }
    }
}
