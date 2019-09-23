using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace TcpLib
{

    public static partial class TcpLibExtensions
    {
        public static async Task<Result<int>> ReceiveAsync(
            this Socket socket,
            byte[] buffer,
            int offset,
            int size,
            SocketFlags socketFlags,
            CancellationToken cancelToken)
        {
            int bytesReceived = 0;
            try
            {
                var asyncResult = socket.BeginReceive(buffer, offset, size, socketFlags, null, null);
                bytesReceived = await Task<int>.Factory.FromAsync(asyncResult, (s) =>
                    {
                        try
                        {
                            if (cancelToken.IsCancellationRequested)
                            {
                                throw new TaskCanceledException(new Task<Result>(Result.Fail));
                            }
                            cancelToken.ThrowIfCancellationRequested();
                            return socket.EndReceive(s);
                        }
                        catch (Exception e)
                        {
                            System.Diagnostics.Debug.WriteLine("Anonymous Receive Exception: " + e.Message);
                            return -1;
                        }
                    })

                    .ConfigureAwait(false);
            }
            catch (SocketException ex)
            {
                return Result.Fail<int>($"{ex.Message} ({ex.GetType()})");
            }

            return ((bytesReceived >= 0) ? Result.Ok(bytesReceived) : Result.Fail<int>($"Receive Failure from Socket " + socket.Handle));
        }
    }
}
