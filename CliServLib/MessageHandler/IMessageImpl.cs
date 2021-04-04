using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcpLib;

namespace CliServLib
{
    public interface IMessageImpl
    {
        bool PerformAction(Client client, MessageData data);

        void SetActionData(object data);
    }
}
