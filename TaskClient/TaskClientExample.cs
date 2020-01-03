using CliServLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using TcpLib;

namespace TaskClient
{
    public class TaskClientExample
    {
        string _ip = String.Empty;
        int _port = 0;

        Socket _clientSocket;

        System.Threading.Thread rcvThread;
        System.Threading.Thread sndThread;
        Result rcvResult;
        Result sndResult;

        ClientConnectAsync conn = new ClientConnectAsync();

        bool done = false;

        public TaskClientExample(string ip, int port)
        {
            _ip = ip;
            _port = port;

            rcvThread = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(ReceiveHandler));
            sndThread = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(SendHandler));
        }

        public Result RunResult
        {
            get { return sndResult; }
        }

        public void Start()
        {
            rcvThread.Start();
            sndThread.Start();
        }

        private async void ReceiveHandler(object obj)
        {
            while (_clientSocket == null || !_clientSocket.Connected)
            {
                System.Threading.Thread.Sleep(250);
            }
            while (!done)
            {
                var rcvRes = await ReceiveMessageAsync();
                Task.WaitAny();
                if (rcvRes.Success && (rcvRes.Value != null))
                {
                    Console.WriteLine("Message: " + rcvRes.Value.message);
                }
            }
        //    else
        //    {
        //        Console.WriteLine("Receive Failure!");
        //    }
        }

        private async void SendHandler(object obj)
        {
            sndResult = await SendAndReceiveMessageAsync();
            Task.WaitAny();
        }

        public async Task<Result> SendAndReceiveMessageAsync()
        {
            var serverPort = ((_port > 0) ? _port : CliServDefaults.DfltPort);
            string address = _ip;

            var connectResult = await conn.ConnectAsync(_port, _ip, 3000, 10);

            Console.WriteLine("Connecting to IP: {0}, Port: {1}", (!String.IsNullOrEmpty(address) ? address : "localhost"), serverPort);

            // Connection failure. Just return
            if (connectResult.Failure)
            {
                return Result.Fail("There was an error connecting to the server.");
            }

            _clientSocket = connectResult.Value;

            string name = String.Empty;
            Console.Write("Enter a Name: ");
            name = Console.ReadLine();
            MessageData sendData = new MessageData();
            sendData.name = name;
            DataGetter getter = new DataGetter();
            while (!done)
            {
                PrintMenu();
                string action = Console.ReadLine();
                MessageData eventData = SendAction(action, ref done);
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

                    var sendResult = await SendMessageAsync(sendData);
                    if (sendResult.Failure)
                    {
                        return Result.Fail("There was a problem sending a message to the server.");
                    }
                }
                else
                {
                    Console.WriteLine("Empty Message.");
                }
                /*
                var eventData = GetDataAsync.GetMessageDataAsync(getter, (long)_clientSocket.Handle);
                if (eventData != null && eventData.Result != null && eventData.Result.message != null)
                {
                    string message = (string)eventData.Result.message;
                    if (String.Compare(message, "exit", true) == 0)
                    {
                        done = true;
                        message = "I'm exiting.  Goodbye.";
                    }
                    data.message = message;
                    data.id = eventData.Result.id;
                    var sendResult = SendMessageAsync(data);

                    if (sendResult.Result.Failure)
                    {
                        return Result.Fail("There was a problem sending a message to the server.");
                    }
                }
                */
            }
            // Report successful
            return Result.Ok();
        }

        public async Task<Result<string>> SendMessageAsync(object message)
        {
            // Encode a string message before sending it to the server
            var messageData = SerializeDeserialize.SerializeToByteArray(message);

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

        public async Task<Result<MessageData>> ReceiveMessageAsync()
        {
            byte[] data = new byte[CliServDefaults.BufferSize];
            MessageData mData = null;
            var recvResult = await _clientSocket.ReceiveWithTimeoutAsync(
                data,
                0,
                data.Length,
                SocketFlags.None,
                ReceiveTypeEnum.ReceiveTypeDelay,
                //CliServDefaults.SeSndTimeoutMs
                -1
                )
                .ConfigureAwait(false);

            if (recvResult.Value > 0)
            {
                mData = SerializeDeserialize.DeserializeFromByteArray<MessageData>(data);
            }
            return Result.Ok(mData);
        }

        private MessageData SendAction(string action, ref bool done)
        {
            IDataGetter getter = null;
            switch (action)
            {
                case "0":
                    Console.WriteLine("User chose to Exit.");
                    done = true;
                    break;
                case "1":
                    // Send Message to Everyone
                    getter = new DataGetter();
                    break;
                case "2":
                    // Send message to specific user
                    getter = new UserDataGetter();
                    break;
                case "3":
                    // Get all users
                    getter = new UserNamesDataGetter();
                    break;
                case "q":
                case "Q":
                    // Quit.  Send 'exit' string in message
                    MessageData message = new MessageData();
                    message.id = 0;
                    message.handle = 0;
                    message.name = String.Empty;
                    message.response = false;
                    message.message = "exit";
                    return message;
                    // No break, returning directly.
                default:
                    Console.WriteLine("Unsupported Action " + action);
                    break;
            }
            if (getter != null)
            {
                var eventData = GetDataAsync.GetMessageDataAsync(getter, (long)_clientSocket.Handle);
                if ((eventData == null) || eventData.IsFaulted || (eventData.Status == TaskStatus.Canceled))
                {
                    return null;
                }
                else
                {
                    return eventData.Result;
                }
            }
            return null;
        }

        private void PrintMenu()
        {
            Console.WriteLine("Actions Menu");
            Console.WriteLine("[1]   Send Message to Everyone");
            Console.WriteLine("[2]   Send Message to Specific User");
            Console.WriteLine("[3]   Print All Users");
            Console.WriteLine("[Q|q] Quit");
            Console.Write("What Do You Want to Do? --> ");
        }
    }

    public class SerializeDeserialize
    {
        public static byte[] SerializeToByteArray<U>(U obj)
        {
            if (obj == null)
            {
                return null;
            }
            using (var ms = new MemoryStream())
            {
                var bf = new BinaryFormatter();
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        public static U DeserializeFromByteArray<U>(byte[] byteArr)
        {
            if (byteArr == null)
            {
                return default(U);
            }
            U obj = default(U);
            using (var ms = new MemoryStream(byteArr))
            {
                var bf = new BinaryFormatter();
                //ms.Write(byteArr, 0, byteArr.Length);
                //ms.Seek(0, SeekOrigin.Begin);
                obj = (U)bf.Deserialize(ms);
            }
            return obj;
        }
    }
}

