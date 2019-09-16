using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcpLib
{
    public class GetDataAsync
    {
        public static async Task<MessageData> GetMessageDataAsync(IDataGetter getter, int msgType)
        {
            try
            {
                //Console.Write("Enter a message to Send: ");
                var task = await Task<MessageData>.Factory.StartNew(() => getter.GetData(msgType));
                return task;
            }
            catch (Exception e)
            {
                throw;
            }
        }
    }
}
