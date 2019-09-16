using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace TcpLib
{

    public static partial class TcpLibExtensions
    {
        private const int MaxCycles = 20;

        public static async Task<Result> SendWithTimeoutAsync(
            this Socket socket,
            byte[] buffer,
            int offset,
            int size,
            SocketFlags socketFlags,
            SendTypeEnum sendType = SendTypeEnum.SendTypeDelay,
            int timeoutMs = -1)
        {
            int cycles = 0;
            bool sendException = false;
            bool msgSent = false;

            do
            {
                ++cycles;
                Console.WriteLine("Trying to send: {0}", cycles);

                if (sendType == SendTypeEnum.SendTypeDelay || (cycles > MaxCycles))
                {
                    sendException = true;
                }

                try
                {
 
                    var asyncResult = socket.BeginSend(buffer, offset, size, socketFlags, null, null);
                    var sendTask = Task<int>.Factory.FromAsync(asyncResult, _ => socket.EndSend(asyncResult));

                    if (sendTask != await Task.WhenAny(sendTask, Task.Delay(timeoutMs)).ConfigureAwait(false))
                    {
                        if (sendException)
                        {
                            throw new TimeoutException();
                        }
                    }
                    // we sent so break out of loop and return OK
                    msgSent = true;
                    break;
                }
                catch (SocketException ex)
                {
                    if (sendException)
                    {
                        return Result.Fail($"{ex.Message} ({ex.GetType()})");
                    }
                }
                catch (TimeoutException ex)
                {
                    if (sendException)
                    {
                        return Result.Fail($"{ex.Message} ({ex.GetType()})");
                    }
                }

            } while (cycles <= MaxCycles);

            if (!msgSent || (cycles > MaxCycles))
            {
                return Result.Fail("There was a problem sending.");
            }
            return Result.Ok();
        }
    }
}
