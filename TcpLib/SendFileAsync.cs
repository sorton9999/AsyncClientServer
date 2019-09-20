using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace TcpLib
{

    public static partial class TcpLibExtensions
    {
        public static async Task<Result> SendFileAsync(this Socket socket, string filePath)
        {
            try
            {
                await Task.Factory.FromAsync(socket.BeginSendFile, socket.EndSendFile, filePath, null).ConfigureAwait(false);
            }
            catch (SocketException ex)
            {
                return Result.Fail($"{ex.Message} ({ex.GetType()})");
            }

            return Result.Ok();
        }
    }
}
