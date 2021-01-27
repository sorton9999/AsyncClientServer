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
        public enum MessageFactoryTypesEnum
        {
            MSG_TYPE_UNINIT = 0,
            GLOBAL_MSG_TYPE,
            USER_MSG_TYPE,
            ALL_USERS_MSG_TYPE,
            GET_USERS_MSG_TYPE = 10,
            CLIENT_EXIT_MSG_TYPE = 99,
            FILE_MSG_TYPE = 100
        };

        private object lockObj = new object();

        private static MessageImplFactory _instance = null;

        // We are storing the impl objects since we will reuse them during
        // the run.  Once an impl is created it stays available and the
        // existing instantiation will be returned.
        private readonly Dictionary<long, Dictionary<MessageFactoryTypesEnum, IMessageImpl>> implStore = new Dictionary<long, Dictionary<MessageFactoryTypesEnum, IMessageImpl>>();

        public static MessageImplFactory Instance()
        {
            return _instance ?? (_instance = new MessageImplFactory());
        }

        protected MessageImplFactory()
        {

        }

        public IMessageImpl MakeMessageImpl(int msgType, long clientHandle)
        {
            IMessageImpl impl = default(IMessageImpl);
            bool found = false;
            bool clientFound = false;
            MessageFactoryTypesEnum msgFactoryType;
            bool success = Enum.TryParse(msgType.ToString(), out msgFactoryType);
            Console.WriteLine("Getting IMPL for Client: {0}; TASK: {1}", clientHandle, msgType.ToString());
            Dictionary<MessageFactoryTypesEnum, IMessageImpl> client = new Dictionary<MessageFactoryTypesEnum, IMessageImpl>();
            if (success && implStore.ContainsKey(clientHandle))
            {
                client = new Dictionary<MessageFactoryTypesEnum, IMessageImpl>();
                clientFound = implStore.TryGetValue(clientHandle, out client);
                if (clientFound)
                {
                    found = client.TryGetValue(msgFactoryType, out impl);

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
                            Dictionary<MessageFactoryTypesEnum, IMessageImpl> temp = new Dictionary<MessageFactoryTypesEnum, IMessageImpl>
                        {
                            { msgFactoryType, impl }
                        };
                            implStore.Add(clientHandle, temp);

                            Console.WriteLine("Added Client: {0}", clientHandle);
                        }
                        else
                        {
                            impl = GetMessageImpl(msgType);
                            if (impl != default(IMessageImpl))
                            {
                                client.Add(msgFactoryType, impl);
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

        public IMessageImpl GetMessageImpl(int msgType)
        {
            IMessageImpl impl = default(IMessageImpl);
            MessageFactoryTypesEnum msgFactoryType;
            bool success = Enum.TryParse(msgType.ToString(), out msgFactoryType);
            if (success)
            {
                switch (msgFactoryType)
                {
                    case MessageFactoryTypesEnum.ALL_USERS_MSG_TYPE:
                        impl = new AllUsersMessageImpl();
                        break;
                    case MessageFactoryTypesEnum.CLIENT_EXIT_MSG_TYPE:
                        impl = new ExitMessageImpl();
                        break;
                    case MessageFactoryTypesEnum.FILE_MSG_TYPE:
                        impl = new FileMessageImpl();
                        break;
                    case MessageFactoryTypesEnum.GET_USERS_MSG_TYPE:
                        impl = new GetUserNameMessageImpl();
                        break;
                    case MessageFactoryTypesEnum.GLOBAL_MSG_TYPE:
                        impl = new GlobalMessageImpl();
                        break;
                    case MessageFactoryTypesEnum.USER_MSG_TYPE:
                        impl = new UserMessageImpl();
                        break;
                    case MessageFactoryTypesEnum.MSG_TYPE_UNINIT:
                    default:
                        Console.WriteLine("Unsupported Message Type: " + msgType);
                        break;
                }
            }
            return impl;
        }
    }
}
