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
        private static bool useLocalhost = false;
        private static bool useTestObjs = false;

        static void Main(string[] args)
        {
            ParseArgs(args);

            //TaskServer server = null;
            MessageServer svr = null;
            CliServLib.DefaultImpl.TaskServer tSvr = null;
            try
            {
                if (useTestObjs)
                {
                    tSvr = new CliServLib.DefaultImpl.TaskServer(useLocalhost);
                }
                else
                {
                    //server = new TaskServer();
                    svr = new MessageServer(useLocalhost);
                    svr.MessageFactory = MessageImplFactory.Instance();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Server Exception: " + e.Message);
                return;
            }
            ConsoleKeyInfo keyInfo = new ConsoleKeyInfo();
            //while (!server.ClientsAllDone() && !server.AllClientsRemoved)
            bool clientsDone = false;
            bool clientsRemoved = false;
            if (useTestObjs)
            {
                clientsDone = tSvr.ClientsAllDone();
            }
            else
            {
                clientsDone = svr.ClientsAllDone();
                clientsRemoved = svr.AllClientsRemoved;
            }
            while (!clientsDone && !clientsRemoved)
            {
                keyInfo = Console.ReadKey();
                if (keyInfo.Modifiers == ConsoleModifiers.Control && keyInfo.Key == ConsoleKey.X)
                {
                    Console.Write("Console> ");
                    string entry = Console.ReadLine();
                    if (entry == "exit")
                    {
                        if (useTestObjs)
                        {
                            try
                            {
                                tSvr.InternMsgServer.RemoveAllClients();
                                tSvr.InternMsgServer.ServerIsDone = true;
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Exiting Exception: " + e.Message);
                            }
                        }
                        else
                        {
                            try
                            {
                                //server.RemoveAllClients();
                                //server.ServerIsDone = true;
                                svr.RemoveAllClients();
                                svr.ServerIsDone = true;
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Exiting Exception: " + e.Message);
                            }
                        }
                    }
                }

                if (useTestObjs)
                {
                    clientsDone = tSvr.ClientsAllDone();
                }
                else
                {
                    clientsDone = svr.ClientsAllDone();
                    clientsRemoved = svr.AllClientsRemoved;
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

        static void ParseArgs(string[] args)
        {
            foreach (string arg in args)
            {
                if ((arg == "localhost") || (arg == "127.0.0.1"))
                {
                    useLocalhost = true;
                }
                else if (arg == "test")
                {
                    useTestObjs = true;
                }
            }
        }
    }
}
