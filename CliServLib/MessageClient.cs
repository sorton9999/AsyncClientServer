using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TcpLib;


namespace CliServLib
{
    public class MessageClient
    {
        public delegate void ResetOnDel(bool reset);
        public event ResetOnDel ResetEvent;

        // Connection info
        string _ip = String.Empty;
        int _port = 0;

        // Entered name for this client
        string _name = String.Empty;

        // My socket
        Socket _clientSocket;

        // Receive/Send threads
        System.Threading.Thread rcvThread;
        System.Threading.Thread sndThread;
        // Keep track of last operation result for rec/send
        Result rcvResult;
        Result sndResult;

        // Connection object
        ClientConnectAsync conn = new ClientConnectAsync();

        // Are we done?  Turning this to TRUE exits thread loops
        bool done = false;

        // Are we resetting?
        bool reset = false;

        public MessageClient(string ip, int port, string clientName)
        {
            _ip = ip;
            _port = port;
            _name = clientName;

            rcvThread = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(ReceiveHandler));
            sndThread = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(SendHandler));
        }

        private async void ReceiveHandler(object obj)
        {
            rcvResult = await ReceiveHandlerAsync(obj);
            Task.WaitAny();
        }

        public Result RunResult
        {
            get { return sndResult; }
        }

        public Result RcvResult
        {
            get { return rcvResult; }
        }

        public void Start()
        {
            rcvThread.Start();
            sndThread.Start();
        }

        private async Task<Result> ReceiveHandlerAsync(object obj)
        {
            while (_clientSocket == null || !_clientSocket.Connected)
            {
                System.Threading.Thread.Sleep(250);
            }
            string errMsg = "Receive from Server Failure.";
            while (!done)
            {
                try
                {
                    var rcvRes = await ReceiveMessageAsync();
                    //Task.WaitAny();
                    if (rcvRes.Success && (rcvRes.Value != null))
                    {
                        HandleMessages(rcvRes.Value);
                    }
                    else if (rcvRes.Failure)
                    {
                        Console.WriteLine(errMsg);
                        return rcvRes;
                    }
                }
                catch (SocketException e)
                {
                    Console.WriteLine("Server Disconnected.  Restarting...");
                    done = true;
                    reset = true;
                    ResetEvent?.Invoke(true);
                }
                catch (Exception e)
                {
                    return Result.Fail(errMsg + ": " + e.Message);
                }
            }
            if (reset)
            {
                // Don't await here.  We want to exit this method before the reset finishes.
                ResetAsync();
            }
            Console.WriteLine("ReceiveHandler Returning.");
            return Result.Ok();
        }

        private async Task ResetAsync()
        {
            await Task.Run(() =>
            {
                // Cancel the console readline wait for user inputs
                //CancelRead();

                // Put a little delay in then restart
                System.Threading.Thread.Sleep(5000);
                Console.WriteLine("Client Resetting...");
                Console.WriteLine("Waiting for Server.");

                done = false;
                rcvThread = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(ReceiveHandler));
                sndThread = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(SendHandler));
                _clientSocket = null;
                Start();
            }
           ).ConfigureAwait(false);
        }

        private void HandleMessages(MessageData msg)
        {
            Console.WriteLine("Handling Message From Server.");

            switch (msg.id)
            {
                case 1:
                    // Received Global Message
                    Console.WriteLine("Message: " + msg.message);
                    break;
                case 2:
                    // Received Specific User Message
                    Console.WriteLine("Message: " + msg.message);
                    break;
                case 3:
                    // Received List of Users
                    Console.WriteLine(msg.message);
                    break;
                case 10:
                    // Received request for User Name to Register with Server
                    MessageData data = new MessageData();
                    data.id = msg.id;
                    data.name = _name;
                    data.handle = (long)_clientSocket.Handle;
                    data.response = true;
                    data.message = "Client Name";
                    var res = SendMessageAsync(data);
                    if (res.IsFaulted || res.IsCanceled || res.Result.Failure)
                    {
                        Console.WriteLine("Name Send Failure: " + res.Result.Error);
                    }
                    break;
                default:
                    Console.WriteLine("Unsupported Message Type.  Doing Nothing.");
                    break;
            }
        }

        private async void SendHandler(object obj)
        {
            try
            {
                sndResult = await SendAndConnectMessageAsync();
            }
            catch (Exception)
            { }
            Task.WaitAny();
        }

        public async Task<Result> Connect()
        {
            if (_clientSocket.Connected)
            {
                return Result.Ok("Already connected");
            }

            var serverPort = ((_port > 0) ? _port : CliServDefaults.DfltPort);
            string address = _ip;

            // Connect to the Server
            var connectResult = await conn.ConnectAsync(_port, _ip, 3000, 10);

            Console.WriteLine("Connecting to IP: {0}, Port: {1}", (!String.IsNullOrEmpty(address) ? address : "localhost"), serverPort);

            // Connection failure. Just return
            if (connectResult.Failure)
            {
                return Result.Fail("There was an error connecting to the server.");
            }

            _clientSocket = connectResult.Value;

            // Report successful
            return Result.Ok();
        }

        public async Task<Result> SendAndConnectMessageAsync()
        {
            /*
            var serverPort = ((_port > 0) ? _port : CliServDefaults.DfltPort);
            string address = _ip;

            // Connect to the Server
            var connectResult = await conn.ConnectAsync(_port, _ip, 3000, 10);

            Console.WriteLine("Connecting to IP: {0}, Port: {1}", (!String.IsNullOrEmpty(address) ? address : "localhost"), serverPort);

            // Connection failure. Just return
            if (connectResult.Failure)
            {
                return Result.Fail("There was an error connecting to the server.");
            }

            _clientSocket = connectResult.Value;
            */

            if (!_clientSocket.Connected)
            {
                var connectResult = await Connect();

                if (connectResult.Failure)
                {
                    return Result.Fail("No connection.");
                }
            }

            // Register a name for this client
            if (reset)
            {
                Console.WriteLine("<<< Reset detected.  Hit ENTER before typing name. >>>");
                reset = false;
            }
            MessageData sendData = new MessageData() { name = _name };
            MessageData eventData = null;
            while (!done)
            {
                // Reset connection vars.
                ResetEvent?.Invoke(false);
                sndResult = null;
                rcvResult = null;
                //EnableRead();

                try
                {
                    eventData = UserSendEvent();
                    if (eventData != null)
                    {
                        string message = (string)eventData.message;

                        if (String.Compare(message, "exit", true) == 0)
                        {
                            done = true;
                            message = "I'm exiting.  Goodbye.";
                        }
                        sendData.message = message;
                        sendData.id = eventData.id;

                        if (sendData.id > 0)
                        {
                            var sendResult = await SendMessageAsync(sendData);
                            if (sendResult.Failure)
                            {
                                return sendResult;
                            }
                        }
                        else
                        {
                            System.Threading.Thread.Sleep(250);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Empty Message.");
                    }
                }
                catch (InvalidOperationException)
                {
                    Console.WriteLine("Input Cancelled.");
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Input Cancelled.");
                }
            }

            // Report successful
            return Result.Ok();
        }

        private MessageData UserSendEvent()
        {
            MessageData msg = new MessageData();

            // Send Events Here

            return msg;
        }

        public async Task<Result<MessageData>> ReceiveMessageAsync()
        {
            byte[] data = new byte[CliServDefaults.BufferSize];
            MessageData mData = null;
            try
            {
                var recvResult = await _clientSocket.ReceiveWithTimeoutAsync(
                    data,
                    0,
                    data.Length,
                    SocketFlags.None,
                    ReceiveTypeEnum.ReceiveTypeDelay,
                    // Wait forever
                    -1
                    )
                    .ConfigureAwait(false);

                if (recvResult.Value > 0)
                {
                    mData = ClientData<MessageData>.DeserializeFromByteArray<MessageData>(data);
                }
                return Result.Ok(mData);
            }
            catch (SocketException e)
            {
                throw;
            }
        }

        public async Task<Result<string>> SendMessageAsync(object message)
        {
            // Encode a string message before sending it to the server
            var messageData = ClientData<MessageData>.SerializeToByteArray(message);

            // Send it away
            var sendResult =
                await _clientSocket.SendWithTimeoutAsync(
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
