using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace TcpLib
{

    public static partial class TcpLibExtensions
    {
        public static async Task<Result<Socket>> AcceptAsync(this Socket socket)
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

            return Result.Ok(transferSocket);
        }
    }
}
