using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncServer
{
    static class Program
    {
        static int Main(string[] args)
        {
            AsyncServer.StartListening();
            Console.WriteLine("Hit Enter to Exit...");
            Console.ReadLine();
            return 0;
        }
    }
}
