using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TcpLib;

namespace TaskSocketDemo
{

    public class TaskSocketExample
    {
        const int BufferSize = 8 * 1024;
        const int ConnectTimeoutMs = 3000;
        const int ReceiveTimeoutMs = 3000;
        const int SendTimeoutMs = 3000;

        Socket _listenSocket;
        Socket _clientSocket;
        Socket _transferSocket;

        public async Task<Result> SendAndReceiveTextMesageAsync()
        {
            _listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _transferSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            var serverPort = 7003;
            var ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());

            var ipAddress =
            ipHostInfo.AddressList.Select(ip => ip)
                .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);

            var ipEndPoint = new IPEndPoint(ipAddress, serverPort);

            // Step 1: Bind a socket to a local TCP port and Listen for incoming connections
            _listenSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _listenSocket.Bind(ipEndPoint);
            _listenSocket.Listen(5);

            // Step 2: Create a Task and accept the next incoming connection (ServerAcceptTask)
            // NOTE: This call is not awaited so the method continues executing
            var acceptTask = Task.Run(AcceptConnectionTask);

            // Step 3: With another socket, connect to the bound socket and await the result (ClientConnectTask)
            var connectResult =
                await _clientSocket.ConnectWithTimeoutAsync(
            ipAddress.ToString(),
            serverPort,
            ListenTypeEnum.ListenTypeDelay,
            ConnectTimeoutMs).ConfigureAwait(false);

            // Step 4: Await the result of the ServerAcceptTask
            var acceptResult = await acceptTask.ConfigureAwait(false);

            // If either ServerAcceptTask or ClientConnectTask did not complete successfully,stop execution and report the error
            if (Result.Combine(acceptResult, connectResult).Failure)
            {
                return Result.Fail("There was an error connecting to the server/accepting connection from the client");
            }

            // Step 5: Store the transfer socket if ServerAcceptTask was successful
            _transferSocket = acceptResult.Value;

            // Step 6: Create a Task and recieve data from the transfer socket (ServerReceiveBytesTask)
            // NOTE: This call is not awaited so the method continues executing
            var receiveTask = Task.Run(ReceiveMessageAsync);

            // Step 7: Encode a string message before sending it to the server
            var messageToSend = "this is a text message from a socket";
            var messageData = Encoding.ASCII.GetBytes(messageToSend);

            // Step 8: Send the message data to the server and await the result (ClientSendBytesTask)
            var sendResult =
                await _clientSocket.SendWithTimeoutAsync(
            messageData,
            0,
            messageData.Length,
            0,
            SendTypeEnum.SendTypeDelay,
            SendTimeoutMs).ConfigureAwait(false);

            // Step 9: Await the result of ServerReceiveBytesTask
            var receiveResult = await receiveTask.ConfigureAwait(false);

            // Step 10: If either ServerReceiveBytesTask or ClientSendBytesTask did not complete successfully,stop execution and report the error
            if (Result.Combine(sendResult, receiveResult).Failure)
            {
                return Result.Fail("There was an error sending/receiving data from the client");
            }

            // Step 11: Compare the string that was received to what was sent, report an error if not matching
            var messageReceived = receiveResult.Value;
            if (messageToSend != messageReceived)
            {
                return Result.Fail("Error: Message received from client did not match what was sent");
            }

            // Step 12: Report the entire task was successful since all subtasks were successful
            return Result.Ok();
        }

        async Task<Result<Socket>> AcceptConnectionTask()
        {
            return await _listenSocket.AcceptAsync().ConfigureAwait(false);
        }

        async Task<Result<string>> ReceiveMessageAsync()
        {
            var message = string.Empty;
            var buffer = new byte[BufferSize];

            var receiveResult =
                await _transferSocket.ReceiveWithTimeoutAsync(
            buffer,
            0,
            BufferSize,
            0,
            ReceiveTypeEnum.ReceiveTypeDelay,
            ReceiveTimeoutMs).ConfigureAwait(false);

            var bytesReceived = receiveResult.Value;
            if (bytesReceived == 0)
            {
                return Result.Fail<string>("Error reading message from client, no data was received");
            }

            message = Encoding.ASCII.GetString(buffer, 0, bytesReceived);

            return Result.Ok(message);
        }
    }
}
