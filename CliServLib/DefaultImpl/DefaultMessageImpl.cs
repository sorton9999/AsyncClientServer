using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcpLib;

namespace CliServLib.DefaultImpl
{
    public class DefaultMessageImpl : IMessageImpl
    {
        public bool PerformAction(Client client, MessageData data)
        {
            bool retVal = true;

            try
            {
                string temp = (string)data.message;
                data.message = String.Format("[Server] says \'{1}\'.", data.name, temp);
                data.response = true;
                SendMessage(client, data);
            }
            catch (Exception e)
            {
                Console.WriteLine("Server Message Exception: " + e.Message);
                retVal = false;
            }

            return retVal;
        }

        public void SetActionData(object data)
        {

        }

        private async void SendMessage(Client client, MessageData msg)
        {
            await Task.Factory.StartNew( () =>
            {
                var res = CliServLib.MessageServer.SendMessageAsync(client, msg);
                if (res.Result.Failure)
                {
                    Console.WriteLine("There is a problem sending data out to the client.");
                }
            });
        }
    }
}
