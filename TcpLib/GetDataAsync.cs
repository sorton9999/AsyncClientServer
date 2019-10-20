using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcpLib
{
    public class GetDataAsync
    {
        public static async Task<MessageData> GetMessageDataAsync(IDataGetter getter, long handle)
        {
            try
            {
                //Console.Write("Enter a message to Send: ");
                var task = await Task<MessageData>.Factory.StartNew(() => getter.GetData(handle));
                //getter.SetData(null);
                return task;
            }
            catch (Exception e)
            {
                throw;
            }
        }

        public static async Task<MessageData> GetMessageDataAsync(IDataGetter getter, int msgType, long handle)
        {
            try
            {
                //Console.Write("Enter a message to Send: ");
                var task = await Task<MessageData>.Factory.StartNew(() => getter.GetData(handle));
                //getter.SetData(null);
                return task;
            }
            catch (Exception e)
            {
                throw;
            }
        }
    }
}
