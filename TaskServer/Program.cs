using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CliServLib;

namespace TaskServer
{
    class Program
    {
        static void Main(string[] args)
        {
            //TaskServer server = null;
            MessageServer svr = null;
            try
            {
                //server = new TaskServer();
                svr = new MessageServer();
            }
            catch (Exception e)
            {
                Console.WriteLine("Server Exception: " + e.Message);
                return;
            }
            ConsoleKeyInfo keyInfo = new ConsoleKeyInfo();
            //while (!server.ClientsAllDone() && !server.AllClientsRemoved)
            while (!svr.ClientsAllDone() && !svr.AllClientsRemoved)
            {
                keyInfo = Console.ReadKey();
                if (keyInfo.Modifiers == ConsoleModifiers.Control && keyInfo.Key == ConsoleKey.X)
                {
                    Console.Write("Console> ");
                    string entry = Console.ReadLine();
                    if (entry == "exit")
                    {
                        //server.RemoveAllClients();
                        //server.ServerIsDone = true;
                        svr.RemoveAllClients();
                        svr.ServerIsDone = true;
                    }
                }
            }
            return;
       }

        static async Task<TcpLib.Result> Runme(TaskServerExample ex)
        {
            var res = await ex.SendAndReceiveTextMessageAsync();
            Task.WaitAny();
            return res;
        }
    }
}
