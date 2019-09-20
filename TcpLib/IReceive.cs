using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcpLib
{
    public enum ReceiveTypeEnum
    {
        ReceiveTypeDontCare,
        ReceiveTypeDelay,
        ReceiveTypeCycle
    }

    public interface IReceive
    {
        void ReceiveLoop(object arg);
    }
}
