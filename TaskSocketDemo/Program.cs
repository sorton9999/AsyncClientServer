using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskSocketDemo
{
    public class Program
    {
        static void Main(string[] args)
        {
            var re = Runme(new TaskSocketExample());
        }

        static private async Task<TcpLib.Result> Runme(TaskSocketDemo.TaskSocketExample ex)
        {
            var res = await ex.SendAndReceiveTextMesageAsync();
            Task.WaitAny();
            return res;
        }
    }
}
