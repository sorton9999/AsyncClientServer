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

        ClientConnectAsync conn = new ClientConnectAsync();

        bool done = false;

        public TaskClientExample(string ip, int port)
        {
            _ip = ip;
            _port = port;
        }

        public async Task<Result> SendAndReceiveMessageAsync()
        {
            var serverPort = ((_port > 0) ? _port : CliServDefaults.DfltPort);
            string address = _ip;

            var connectResult = await conn.ConnectAsync(_port, _ip);

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
            MessageData data = new MessageData();
            data.name = name;
            DataGetter getter = new DataGetter();
            while (!done)
            {
                var eventData = GetDataAsync.GetMessageDataAsync(getter, 10);
                if (eventData != null)
                {
                    string message = (string)eventData.Result.message;
                    if (String.Compare(message, "exit", true) == 0)
                    {
                        done = true;
                        message = "I'm exiting.  Goodbye.";
                    }
                    data.message = message;
                    var sendResult = SendMessageAsync(data);

                    if (sendResult.Result.Failure)
                    {
                        return Result.Fail("There was a problem sending a message to the server.");
                    }
                }
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

