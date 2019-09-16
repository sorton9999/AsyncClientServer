using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncClient
{
    static class Program
    {
        static int Main(string[] args)
        {
            AsyncClient.StartClient();
            Console.WriteLine("Hit enter to Exit...");
            Console.Read();
            return 0;
        }
    }
}
