using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcpLib
{
    public enum SendTypeEnum
    {
        SendTypeDontCare,
        SendTypeDelay,
        SendTypeCycle
    }

    public interface ISend
    {
        void SendLoop(object arg);

        Result ResultLoop(object arg);
    }
}
