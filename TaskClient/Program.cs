using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CliServLib;

namespace TaskClient
{
    class Program
    {
        static string _ip = String.Empty;
        static int _port = 0;

        static bool _done = false;
        static bool _reset = false;
        private static bool useTestObjs = false;

        static void Main(string[] args)
        {
            ParseArgs(args);

            if (useTestObjs)
            {
                CliServLib.DefaultImpl.TaskClient tCli = new CliServLib.DefaultImpl.TaskClient(_ip, _port, "TestClient");
                tCli.InternMsgClient.ResetEvent += InternMsgClient_ResetEvent;
                var res = RunmeTest(tCli);
                Console.WriteLine("Client Return: {0}", res.Success);
                if (res.Failure)
                {
                    Console.WriteLine(res.Error);
                }
            }
            else
            {
                TaskClientExample ex = new TaskClientExample(_ip, _port);
                //MessageClient ex = new MessageClient("127.0.0.1", CliServDefaults.DfltPort, "TestClient");
                ex.ResetEvent += Ex_ResetEvent;
                var res = Runme(ex);
                Console.WriteLine("Client Return: {0}", res.Success);
                if (res.Failure)
                {
                    Console.WriteLine(res.Error);
                }
            }
            Console.WriteLine("Hit ENTER to Exit...");
            Console.ReadLine();
        }

        private static void InternMsgClient_ResetEvent(bool reset)
        {
            _reset = reset;
        }

        private static void Ex_ResetEvent(bool reset)
        {
            _reset = reset;
        }

        //       static TcpLib.Result Runme(TaskClientExample ex)
        static TcpLib.Result Runme(TaskClientExample ex)
        {
            ex.Start();
            while (!_done)
            {
                System.Threading.Thread.Sleep(1000);
                if (!_reset)
                {
                    if (ex.RunResult != null)
                    {
                        _done = true;
                    }
                }
            }
            return ex.RunResult;
        }

        static TcpLib.Result RunmeTest(CliServLib.DefaultImpl.TaskClient ex)
        {
            ex.Start();
            while (!_done)
            {
                System.Threading.Thread.Sleep(1000);
                if (!_reset)
                {
                    if (ex.InternMsgClient.RunResult != null)
                    {
                        _done = true;
                    }
                }
            }
            return ex.InternMsgClient.RunResult;
        }

        static void ParseArgs(string [] args)
        {
            foreach (string arg in args)
            {
                if (arg == "-ip")
                {
                    _ip = args[1];
                }
                else if (arg == "-port")
                {
                    _port = Convert.ToInt32(args[3]);
                }
                else if (arg == "test")
                {
                    useTestObjs = true;
                }
            }
        }

    }
}
