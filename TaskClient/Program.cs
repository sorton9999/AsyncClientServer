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

        static void Main(string[] args)
        {
            ParseArgs(args);
            var res = Runme(new TaskClientExample(_ip, _port));
            Console.WriteLine("Client Return: {0}", res.Result.Success);
            Console.WriteLine("Hit ENTER to Exit...");
            Console.ReadLine();
        }

        static async Task<TcpLib.Result> Runme(TaskClientExample ex)
        {
            var res = await ex.SendAndReceiveMessageAsync();
            Task.WaitAny();
            return res;
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
