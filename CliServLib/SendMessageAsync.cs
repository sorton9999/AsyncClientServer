using CliServLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TcpLib;

namespace CliServLib
{
    public class SendMessageAsync
    {
        public static async Task<Result<string>> SendMsgAsync(Socket socket, object message)
        {
            // Encode a string message before sending it to the server
            var messageData = ClientData<MessageData>.SerializeToByteArray(message);

            // Send it away
            var sendResult =
                await socket.SendWithTimeoutAsync(
                    messageData,
                    0,
                    messageData.Length,
                    0,
                    SendTypeEnum.SendTypeCycle,
                    CliServDefaults.SendTimeoutMs
                )
                .ConfigureAwait(false);

            // If Task did not complete successfully, report the error
            if (sendResult.Failure)
            {
                return Result.Fail<string>("There was an error sending data to the server");
            }
            // Sent
            return Result.Ok("Message sent.");
        }
    }
}
