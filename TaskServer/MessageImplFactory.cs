using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskCommon;

namespace TaskServer
{
    public class MessageImplFactory
    {
        private static MessageImplFactory _instance = null;

        // We are storing the impl objects since we will reuse them during
        // the run.  Once an impl is created it stays available and the
        // existing instantiation will be returned.
        private readonly Dictionary<MessageTypesEnum, IMessageImpl> implStore = new Dictionary<MessageTypesEnum, IMessageImpl>();

        public static MessageImplFactory Instance()
        {
            return _instance ?? (_instance = new MessageImplFactory());
        }

        protected MessageImplFactory()
        {

        }

        public IMessageImpl MakeMessageImpl(MessageTypesEnum msgType)
        {
            IMessageImpl impl = default(IMessageImpl);

            if (implStore.ContainsKey(msgType))
            {
                bool found = implStore.TryGetValue(msgType, out impl);
            }
            else
            {
                switch (msgType)
                {
                    case MessageTypesEnum.ALL_USERS_MSG_TYPE:
                        break;
                    case MessageTypesEnum.CLIENT_EXIT_MSG_TYPE:
                        break;
                    case MessageTypesEnum.FILE_MSG_TYPE:
                        impl = new FileMessageImpl();
                        implStore.Add(msgType, impl);
                        break;
                    case MessageTypesEnum.GET_USERS_MSG_TYPE:
                        break;
                    case MessageTypesEnum.GLOBAL_MSG_TYPE:
                        break;
                    case MessageTypesEnum.USER_MSG_TYPE:
                        break;
                    case MessageTypesEnum.MSG_TYPE_UNINIT:
                    default:
                        break;
                }
            }
            return impl;
        }
    }
}
