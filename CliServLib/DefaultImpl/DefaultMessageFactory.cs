using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CliServLib.DefaultImpl
{
    public class DefaultMessageFactory : IMessageImplFactory
    {
        public enum MessageTypeEnum
        {
            MSG_TYPE_UNINIT = -1,
            MSG_TYPE_DEFAULT = 1
        };

        public IMessageImpl MakeMessageImpl(int msgId, long clientHandle)
        {
            Console.WriteLine("Client {0} getting message impl", clientHandle);
            return GetMessageImpl(msgId);
        }

        public IMessageImpl GetMessageImpl(int msgId)
        {
            MessageTypeEnum msgType = MessageTypeEnum.MSG_TYPE_UNINIT;
            IMessageImpl impl = default(IMessageImpl);

            if (Enum.TryParse(msgId.ToString(), out msgType))
            {
                switch (msgType)
                {
                    case MessageTypeEnum.MSG_TYPE_DEFAULT:
                        impl = new DefaultMessageImpl();
                        break;
                    case MessageTypeEnum.MSG_TYPE_UNINIT:
                    default:
                        Console.WriteLine("Unhandled message type: " + msgType);
                        break;
                }
            }
            return impl;
        }
    }
}
