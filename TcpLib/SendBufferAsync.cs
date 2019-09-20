using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TcpLib
{
    public static partial class TcpLibExtensions
    {
        public static async Task<Result> SendBufferAsync(
            Socket socket,
            byte[] buffer,
            int offset,
            int count,
            SocketFlags flags)
        {
            int bytesSent = 0;
            try
            {
                var asyncResult = socket.BeginSend(buffer, offset, count, flags, null, null);
                bytesSent = await Task<int>.Factory.FromAsync(asyncResult, (s) => 
                    {
                        try
                        {
                            return socket.EndSend(s);
                        }
                        catch (Exception e)
                        {
                            System.Diagnostics.Debug.WriteLine("Anonymous Send Exception: " + e.Message);
                            return -1;
                        }
                    })
                    .ConfigureAwait(false);
            }
            catch (SocketException ex)
            {
                return Result.Fail($"{ex.Message} ({ex.GetType()})");
            }

            return ((bytesSent >= 0) ? Result.Ok(bytesSent) : Result.Fail($"Send Failure from Socket " + socket.Handle));
        }
    }
}
