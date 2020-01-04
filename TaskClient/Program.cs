using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskClient
{
    class Program
    {
        static string _ip = String.Empty;
        static int _port = 0;

        static bool done = false;

        static void Main(string[] args)
        {
            ParseArgs(args);
            var res = Runme(new TaskClientExample(_ip, _port));
            Console.WriteLine("Client Return: {0}", res.Success);
            if (res.Failure)
            {
                Console.WriteLine(res.Error);
            }
            Console.WriteLine("Hit ENTER to Exit...");
            Console.ReadLine();
        }

        //static async Task<TcpLib.Result> Runme(TaskClientExample ex)
        static TcpLib.Result Runme(TaskClientExample ex)
        {
            ex.Start();
            while (!done)
            {
                System.Threading.Thread.Sleep(1000);
                if (ex.RunResult != null)
                {
                    done = true;
                }
            }
            return ex.RunResult;
            //var res = await ex.SendAndReceiveMessageAsync();
            //Task.WaitAny();
            //return res;
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
            }
        }

    }
}
