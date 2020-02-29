using CliServLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TcpLib;

namespace TaskClient
{
    public class TaskClientExample
    {
        const int STD_INPUT_HANDLE = -10;

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CancelIoEx(IntPtr handle, IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool FlushFileBuffers(IntPtr handle);

        public delegate void ResetOnDel(bool reset);
        public event ResetOnDel ResetEvent;

        // Connection info
        string _ip = String.Empty;
        int _port = 0;

        // Entered name for this client
        string name = String.Empty;

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


        public TaskClientExample(string ip, int port)
        {
            _ip = ip;
            _port = port;

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
               CancelRead();

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
                    data.name = name;
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

        public async Task<Result> SendAndConnectMessageAsync()
        {
            var serverPort = ((_port > 0) ? _port : CliServDefaults.DfltPort);
            string address = _ip;
            bool greetingSent = false;

            var connectResult = await conn.ConnectAsync(_port, _ip, 3000, 10);

            Console.WriteLine("Connecting to IP: {0}, Port: {1}", (!String.IsNullOrEmpty(address) ? address : "localhost"), serverPort);

            // Connection failure. Just return
            if (connectResult.Failure)
            {
                return Result.Fail("There was an error connecting to the server.");
            }

            _clientSocket = connectResult.Value;

            // Register a name for this client
            if (reset)
            {
                Console.WriteLine("<<< Reset detected.  Hit ENTER before typing name. >>>");
                reset = false;
            }
            Console.Write("Enter a Name: ");
            name = Console.ReadLine();
            MessageData sendData = new MessageData();
            sendData.name = name;
            MessageData eventData = null;
            while (!done)
            {
                // Reset connection vars.
                ResetEvent?.Invoke(false);
                sndResult = null;
                rcvResult = null;
                EnableRead();

                try
                {
                    eventData = UserSendEvent(ref greetingSent);
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
                            return sendResult;
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
            // Set vars here for possible reconnect
            greetingSent = false;

            // Report successful
            return Result.Ok();
        }

        private MessageData UserSendEvent(ref bool greetingAction)
        {
            string action = String.Empty;
            bool bGreeting = greetingAction;
            if (!bGreeting)
            {
                // Have user send a global message upon first connection
                Console.WriteLine("Send a greeting to Everyone. This registers your Name to the Server.");
                bGreeting = true;
                action = "10";
            }
            else
            {
                // User is registered and working normally
                PrintMenu();
                action = Console.ReadLine();
            }
            greetingAction = bGreeting;
            return (SendAction(action));
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
                    mData = SerializeDeserialize.DeserializeFromByteArray<MessageData>(data);
                }
                return Result.Ok(mData);
            }
            catch (SocketException e)
            {
                throw;
            }
        }

        private MessageData SendAction(string action)
        {
            IDataGetter getter = null;
            switch (action)
            {
                case "99":
                    // Respond to Quit
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
                case "4":
                    GetFileAndSendAsync();
                    break;
                case "10":
                    // Greeting message
                    getter = new DataGetter();
                    // Tell the getter that it's a greeting message
                    getter.SetData(true);

                    break;
                case "q":
                case "Q":
                    // Quit.  Send 'exit' string in message
                    MessageData message = new MessageData();
                    message.id = 99;
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

        private async void GetFileAndSendAsync()
        {
            // Send information about the file to be sent
            MessageData msgData = new MessageData();

            // Pick the file to send.  This calls up a file chooser.
            string fileStr = ShowFileDialog();
            Console.WriteLine("Selected File: " + fileStr);

            // Check for user cancellation
            if (String.IsNullOrEmpty(fileStr))
            {
                return;
            }

            // Split the file from the path
            string filePath = ""; 
            string fileName = fileStr.Replace("\\", "//");
            while (fileName.IndexOf("//") > -1) 
            { 
                filePath += fileName.Substring(0, fileName.IndexOf("//") + 2);
                fileName = fileName.Substring(fileName.IndexOf("//") + 2);
            }

            // Get the data from the file as an array of bytes
            byte[] fileData = File.ReadAllBytes(filePath + fileName);

            // Package up information about the file to be sent in a separate message
            msgData.handle = (long)_clientSocket.Handle;
            msgData.id = 100;
            msgData.length = fileData.Length;
            msgData.name = name;
            msgData.message = fileName;
            msgData.response = false;

            // Send the first message about file info
            var result = await SendMessageAsync(msgData);

            Console.WriteLine("File Send: " + result.Value);
            Console.WriteLine((result.Success ? "SUCCESS" : "FAIL"));

            // Give some time for the server to process the initial info
            System.Threading.Thread.Sleep(1000);

            // Send the file data across
            result = await SendMessageAsync(fileData);

            Console.WriteLine("File Send: " + result.Value);
            Console.WriteLine((result.Success ? "SUCCESS" : "FAIL"));
        }

        public static string ShowFileDialog()
        {
            // Throw up a file chooser dialog in its own thread
            string selectedPath = String.Empty;
            var t = new Thread((ThreadStart)(() =>
            {
                // Create a form to use as the owner of the dialog.
                // Position it in the center of the screen.
                using (var owner = new Form()
                {
                    Width = 0,
                    Height = 0,
                    StartPosition = FormStartPosition.CenterScreen,
                    Text = "Browse for Folder"
                })
                {
                    owner.Show();
                    owner.BringToFront();
                    OpenFileDialog ofd = new OpenFileDialog
                    {
                        InitialDirectory = ".",
                        Title = "Select a File"
                    };
                    if (ofd.ShowDialog(owner) == DialogResult.OK)
                    {
                        selectedPath = ofd.FileName;
                    }
                }
            }));
            t.SetApartmentState(ApartmentState.STA);
            t.Start();

            t.Join();
            return selectedPath;
        }

        private void PrintMenu()
        {
            Console.WriteLine("Actions Menu");
            Console.WriteLine("[1]   Send Message to Everyone");
            Console.WriteLine("[2]   Send Message to Specific User");
            Console.WriteLine("[3]   Print All Users");
            Console.WriteLine("[4]   Send a File");
            Console.WriteLine("[Q|q] Quit");
            Console.Write("What Do You Want to Do? --> ");
        }

        public static void CancelRead()
        {
            var handle = GetStdHandle(STD_INPUT_HANDLE);
            CancelIoEx(handle, IntPtr.Zero);
        }

        public static void EnableRead()
        {
            var handle = GetStdHandle(STD_INPUT_HANDLE);
            FlushFileBuffers(handle);
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
                try
                {
                    bf.Serialize(ms, obj);
                }
                catch (Exception)
                { }
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
                try
                {
                    obj = (U)bf.Deserialize(ms);
                }
                catch (Exception)
                { }
            }
            return obj;
        }
    }
}

