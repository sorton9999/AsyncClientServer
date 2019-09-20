using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TcpLib;

namespace CliServLib
{
    public class ThreadedBase
    {
        public delegate Result StopResultDelegate();
        protected StopResultDelegate stopResult;
        protected Thread theThread;
        protected Action stopAction;
        protected CommsLoop looper;// = new CommsLoop();
        //private Result response = Result.Fail("Not Initialized");
        private Result response = Result.Ok();

        protected ThreadedBase()
        {
            looper = new CommsLoop();
            stopAction = new Action(StopLoop);
            //stopResult = new StopResultDelegate(StopLoopResult);
        }

        //protected ThreadedBase(Func<Result> func)
        //{
        //    looper = new CommsLoop();
        //    stopAction = new Action(StopLoop);
        //    callback = func;
        //}

        protected void InitStart(Func<object, Result> func)
        {
            looper = new CommsLoop(func);
            stopAction = new Action(StopLoop);
            //stopResult = new StopResultDelegate(StopLoopResult);
        }

        protected void StartParam(ParameterizedThreadStart func)
        {
            theThread = new Thread(func);
        }

        //protected void Start(ThreadStart func)
        //{
        //    theThread = new Thread(func);
        //}
        public object Start(object obj)
        {
            theThread = new Thread((s) =>
            {
                if (looper != null)
                {
                    response = looper.LoopFunc(obj);
                }
            });
            return response;
        }

        public Action StopLoopAction
        {
            get { return stopAction; }
            private set { stopAction = value; }
        }

        //public StopResultDelegate StopResult
        //{
        //    get { return stopResult; }
        //    private set { stopResult = value; }
        //}

        //public void Run(ClientData<byte> client)
        public void Run(object client)
        {
            if ((looper != null) && (theThread != null))
            {
                looper.LoopDone = false;
                theThread.Start(client);
            }
        }

        public bool IsDone()
        {
            return ((looper != null) ? looper.LoopDone : true);
        }

        private void StopLoop()
        {
            if ((looper != null) && !looper.LoopDone)
            {
                Console.WriteLine("Stopping loop action...");
                looper.LoopDone = true;
                try
                {
                    theThread.Join();
                }
                catch (Exception)
                {
                    Console.WriteLine("Thread can't join.  Is it started?");
                }
            }
        }

        private Result StopLoopResult()
        {
            if ((looper != null) && !looper.LoopDone)
            {
                Console.WriteLine("Stopping loop. Returning Result.");
                looper.LoopDone = true;
                try
                {
                    theThread.Join();
                }
                catch (Exception)
                {
                    Console.WriteLine("Thread can't join.  Is it started?");
                    return Result.Fail("Thread can't join.  Is it started?");
                }
            }
            return response;
        }

    }
}
