using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TcpLib;

namespace TaskServer
{
    public class TaskServerExample
    {
        const int BufferSize = 8 * 1024;
        const int ReceiveTimeoutMs = 3000;

        Socket _listenSocket;
        Socket _transferSocket;

        public async Task<Result> SendAndReceiveTextMessageAsync()
        {
            _listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            var serverPort = 7003;
            var ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());

            var ipAddress =
            ipHostInfo.AddressList.Select(ip => ip)
                .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);

            var ipEndPoint = new IPEndPoint(ipAddress, serverPort);

            // Bind a socket to a local TCP port and Listen for incoming connections
            _listenSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _listenSocket.Bind(ipEndPoint);
            _listenSocket.Listen(5);

            // Create a Task and accept the next incoming connection (ServerAcceptTask)
            // NOTE: This call is not awaited so the method continues executing
            var acceptTask = Task.Run(AcceptConnectionTask);

            // Await the result of the ServerAcceptTask
            var acceptResult = await acceptTask.ConfigureAwait(false);

            if (acceptResult.Failure)
            {
                return Result.Fail("There was a problem with accepting client connection");
            }
            // Store the transfer socket if ServerAcceptTask was successful
            _transferSocket = acceptResult.Value;

            // Create a Task and recieve data from the transfer socket (ServerReceiveBytesTask)
            // NOTE: This call is not awaited so the method continues executing
            var receiveTask = Task.Run(ReceiveMessageAsync);

            // Await the result of ServerReceiveBytesTask
            var receiveResult = await receiveTask.ConfigureAwait(false);

            // If ServerReceiveBytesTask did not complete successfully, stop execution and report the error
            if (receiveResult.Failure)
            {
                return Result.Fail("There was an error receiving data from the client");
            }

            // Capture message received
            var messageReceived = receiveResult.Value;

            Console.WriteLine("Message Received: {0}", messageReceived);

            return Result.Ok();
        }

        async Task<Result<Socket>> AcceptConnectionTask()
        {
            Console.WriteLine("Wating to Accept Connection from a Client...");
            return await _listenSocket.TaskAcceptAsync().ConfigureAwait(false);
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
                    ReceiveTimeoutMs
                )
                .ConfigureAwait(false);

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


