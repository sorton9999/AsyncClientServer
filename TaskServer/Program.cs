using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskServer
{
    class Program
    {
        static void Main(string[] args)
        {
            //var res = Runme(new TaskServerExample());
            //Console.WriteLine("Server Return: {0}", res.Result.Success);
            TaskServer server = null;
            try
            {
                server = new TaskServer();
            }
            catch (Exception e)
            {
                Console.WriteLine("Server Exception: " + e.Message);
                return;
            }
            ConsoleKeyInfo keyInfo = new ConsoleKeyInfo();
            while (!server.ClientsAllDone())// && keyInfo.Modifiers != ConsoleModifiers.Control && keyInfo.Key != ConsoleKey.C)
            {
                //System.Threading.Thread.Sleep(200);
                keyInfo = Console.ReadKey();
                if (keyInfo.Modifiers == ConsoleModifiers.Control && keyInfo.Key == ConsoleKey.X)
                {
                    Console.Write("Console> ");
                    string entry = Console.ReadLine();
                    if (entry == "exit")
                    {
                        server.RemoveAllClients();
                        server.ServerIsDone = true;
                    }
                }
            }
            return;
            //Console.WriteLine("Hit ENTER to Exit...");
            //Console.ReadLine();
        }

        static async Task<TcpLib.Result> Runme(TaskServerExample ex)
        {
            var res = await ex.SendAndReceiveTextMessageAsync();
            Task.WaitAny();
            return res;
        }
    }
}
