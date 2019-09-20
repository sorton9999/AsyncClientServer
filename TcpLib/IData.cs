using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcpLib
{
    public interface IData
    {
        // Make it indexable
        byte this[int i]
        {
            get;
            set;
        }

        // The array of data
        byte[] Data
        {
            get;
            set;
        }
    }
}
