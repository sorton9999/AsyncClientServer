using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcpLib
{
    public enum ListenTypeEnum
    {
        ListenTypeDontCare,
        ListenTypeDelay,
        ListenTypeCycle
    }


    public interface IListen
    {
        void ListenLoop(object arg);
    }
}
