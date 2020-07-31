using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CliServLib;

namespace TaskServer
{
    public class MessageImplFactory : IMessageImplFactory
    {
        private object lockObj = new object();

        private static MessageImplFactory _instance = null;

        // We are storing the impl objects since we will reuse them during
        // the run.  Once an impl is created it stays available and the
        // existing instantiation will be returned.
        private readonly Dictionary<long, Dictionary<MessageTypesEnum, IMessageImpl>> implStore = new Dictionary<long, Dictionary<MessageTypesEnum, IMessageImpl>>();

        public static MessageImplFactory Instance()
        {
            return _instance ?? (_instance = new MessageImplFactory());
        }

        protected MessageImplFactory()
        {

        }

        public IMessageImpl MakeMessageImpl(MessageTypesEnum msgType, long clientHandle)
        {
            IMessageImpl impl = default(IMessageImpl);
            bool found = false;
            bool clientFound = false;
            Console.WriteLine("Getting IMPL for Client: {0}; TASK: {1}", clientHandle, msgType.ToString());
            Dictionary<MessageTypesEnum, IMessageImpl> client = new Dictionary<MessageTypesEnum, IMessageImpl>();
            if (implStore.ContainsKey(clientHandle))
            {
                client = new Dictionary<MessageTypesEnum, IMessageImpl>();
                clientFound = implStore.TryGetValue(clientHandle, out client);
                if (clientFound)
                {
                    found = client.TryGetValue(msgType, out impl);

                    Console.WriteLine("Found Client: {0}", clientHandle);
                }
                else
                {
                    impl = GetMessageImpl(msgType);
                }
            }
            else
            {
                impl = GetMessageImpl(msgType);
            }
            try
            {
                if (!found)
                {
                    lock (lockObj)
                    {
                        if (!clientFound)
                        {
                            Dictionary<MessageTypesEnum, IMessageImpl> temp = new Dictionary<MessageTypesEnum, IMessageImpl>
                        {
                            { msgType, impl }
                        };
                            implStore.Add(clientHandle, temp);

                            Console.WriteLine("Added Client: {0}", clientHandle);
                        }
                        else
                        {
                            impl = GetMessageImpl(msgType);
                            if (impl != default(IMessageImpl))
                            {
                                client.Add(msgType, impl);
                            }
                            Console.WriteLine("Added Task: {0} to Client: {1}", msgType.ToString(), clientHandle);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("ImplFactory Exception: " + e.Message);
            }
            return impl;
        }

        public bool RemoveClient(long clientHandle)
        {
            bool retVal = false;
            try
            {
                retVal = implStore.Remove(clientHandle);
            }
            catch (Exception e)
            {
                Console.WriteLine("Client Impl Remove Exception: " + e.Message);
            }
            return retVal;
        }

        public IMessageImpl GetMessageImpl(MessageTypesEnum msgType)
        {
            IMessageImpl impl = default(IMessageImpl);
            switch (msgType)
            {
                case MessageTypesEnum.ALL_USERS_MSG_TYPE:
                    impl = new AllUsersMessageImpl();
                    break;
                case MessageTypesEnum.CLIENT_EXIT_MSG_TYPE:
                    impl = new ExitMessageImpl();
                    break;
                case MessageTypesEnum.FILE_MSG_TYPE:
                    impl = new FileMessageImpl();
                    break;
                case MessageTypesEnum.GET_USERS_MSG_TYPE:
                    impl = new GetUserNameMessageImpl();
                    break;
                case MessageTypesEnum.GLOBAL_MSG_TYPE:
                    impl = new GlobalMessageImpl();
                    break;
                case MessageTypesEnum.USER_MSG_TYPE:
                    impl = new UserMessageImpl();
                    break;
                case MessageTypesEnum.MSG_TYPE_UNINIT:
                default:
                    Console.WriteLine("Unsupported Message Type: " + msgType);
                    break;
            }
            return impl;
        }
    }
}
