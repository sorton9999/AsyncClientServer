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
        public int id = 0;
        public long handle = 0;
        public string name = String.Empty;
        public bool response = false;
        public object message = null;
    }
}
