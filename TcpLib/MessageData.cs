using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcpLib
{
    [Serializable]
    public class MessageData
    {
        public int id;
        public string name;
        public object message;
    }
}
