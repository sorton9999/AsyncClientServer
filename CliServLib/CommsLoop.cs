using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TcpLib;

namespace CliServLib
{
    public class CommsLoop //: IReceive, ISend
    {
        private bool loopDone = false;
        private Func<object, Result> loopFunc;

        public CommsLoop()
        {

        }

        public CommsLoop(Func<object, Result> func)
        {
            LoopFunc = func;
        }

        public bool LoopDone
        {
            get { return loopDone; }
            set { loopDone = value; }
        }

        public Func<object, Result> LoopFunc
        {
            get { return loopFunc; }
            private set { loopFunc = value; }
        }

    }
}
