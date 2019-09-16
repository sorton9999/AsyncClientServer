using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace TcpLib
{

    public static partial class TcpLibExtensions
    {
        public static async Task<Result<int>> ReceiveWithTimeoutAsync(
            this Socket socket,
            byte[] buffer,
            int offset,
            int size,
            SocketFlags socketFlags,
            ReceiveTypeEnum recvType = ReceiveTypeEnum.ReceiveTypeDelay,
            int timeoutMs = -1)
        {
            int bytesReceived = 0;
            int cycles = 0;
            bool sendException = false;
            bool msgRecv = false;

            do
            {
                ++cycles;
                Console.WriteLine("Trying to Connect {0}", cycles);
                if (recvType == ReceiveTypeEnum.ReceiveTypeDelay && (cycles > MaxCycles))
                {
                    sendException = true;
                }

                try
                {
                    var asyncResult = socket.BeginReceive(buffer, offset, size, socketFlags, null, null);
                    var receiveTask = Task<int>.Factory.FromAsync(asyncResult, _ => socket.EndReceive(asyncResult));

                    if (receiveTask == await Task.WhenAny(receiveTask, Task.Delay(timeoutMs)).ConfigureAwait(false))
                    {
                        bytesReceived = await receiveTask.ConfigureAwait(false);
                    }
                    else
                    {
                        if (sendException)
                        {
                            throw new TimeoutException();
                        }
                    }
                    msgRecv = true;
                    break;
                }
                catch (SocketException ex)
                {
                    if (sendException)
                    {
                        return Result.Fail<int>($"{ex.Message} ({ex.GetType()})");
                    }
                }
                catch (TimeoutException ex)
                {
                    if (sendException)
                    {
                        return Result.Fail<int>($"{ex.Message} ({ex.GetType()})");
                    }
                }

            } while (cycles <= MaxCycles);

            if (!msgRecv || (cycles > MaxCycles))
            {
                return Result.Fail<int>("There was a receive problem.");
            }
            return Result.Ok(bytesReceived);
        }
    }
}
