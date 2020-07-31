using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace TcpLib
{

    public static partial class TcpLibExtensions
    {
        public static async Task<Result<Socket>> TaskAcceptAsync(this Socket socket)
        {
            Socket transferSocket;
            try
            {
                var acceptTask = Task<Socket>.Factory.FromAsync(socket.BeginAccept, socket.EndAccept, null);
                transferSocket = await acceptTask.ConfigureAwait(false);
            }
            catch (SocketException ex)
            {
                return Result.Fail<Socket>($"{ex.Message} ({ex.GetType()})");
            }
            catch (InvalidOperationException ex)
            {
                return Result.Fail<Socket>($"{ex.Message} ({ex.GetType()})");
            }
            catch (Exception ex)
            {
                return Result.Fail<Socket>($"{ex.Message} ({ex.GetType()})");
            }

            return Result.Ok(transferSocket);
        }

        public static async Task<Result<Socket>> TaskAcceptAsync(this Socket socket, CancellationToken cancelToken)
        {
            Socket transferSocket;
            try
            {
                var acceptTask = await Task<Socket>.Factory.FromAsync(socket.BeginAccept(null, null), (s) =>
                {
                    if (cancelToken.IsCancellationRequested)
                    {
                        throw new TaskCanceledException(new Task<Result>(Result.Fail));
                    }
                    cancelToken.ThrowIfCancellationRequested();
                    return socket.EndAccept(s);

                }).ConfigureAwait(false);

               transferSocket = acceptTask;
            }
            catch (SocketException ex)
            {
                return Result.Fail<Socket>($"{ex.Message} ({ex.GetType()})");
            }
            catch (InvalidOperationException ex)
            {
                return Result.Fail<Socket>($"{ex.Message} ({ex.GetType()})");
            }
            catch (Exception ex)
            {
                return Result.Fail<Socket>($"{ex.Message} ({ex.GetType()})");
            }
                
            return Result.Ok(transferSocket);
        }
    }
}
