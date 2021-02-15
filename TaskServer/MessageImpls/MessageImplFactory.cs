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
        /// <summary>
        /// The message types defined for this implementation
        /// </summary>
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

        /// <summary>
        /// Lock object
        /// </summary>
        private object lockObj = new object();

        /// <summary>
        /// The holder of this object instance following the singleton pattern.
        /// </summary>
        private static MessageImplFactory _instance = null;

        /// <summary>
        /// We are storing the impl objects since we will reuse them during the run.  Once an impl is created
        /// it stays available and the existing instantiation will be returned.
        /// </summary>
        private readonly Dictionary<long, Dictionary<MessageFactoryTypesEnum, IMessageImpl>> implStore = new Dictionary<long, Dictionary<MessageFactoryTypesEnum, IMessageImpl>>();

        /// <summary>
        /// Instance accessor that follows the Singleton pattern
        /// </summary>
        /// <returns></returns>
        public static MessageImplFactory Instance()
        {
            return _instance ?? (_instance = new MessageImplFactory());
        }

        /// <summary>
        /// Constructor
        /// </summary>
        protected MessageImplFactory()
        {
            // Add the Make Impl methods
            MakeImpls.Add(MessageFactoryTypesEnum.CLIENT_EXIT_MSG_TYPE, MakeExitMessage);
            MakeImpls.Add(MessageFactoryTypesEnum.ALL_USERS_MSG_TYPE, MakeAllUsersMessage);
            MakeImpls.Add(MessageFactoryTypesEnum.FILE_MSG_TYPE, MakeFileMessage);
            MakeImpls.Add(MessageFactoryTypesEnum.GET_USERS_MSG_TYPE, MakeUsersMessage);
            MakeImpls.Add(MessageFactoryTypesEnum.GLOBAL_MSG_TYPE, MakeGlobalMessage);
            MakeImpls.Add(MessageFactoryTypesEnum.USER_MSG_TYPE, MakeUserMessage);
            MakeImpls.Add(MessageFactoryTypesEnum.MSG_TYPE_UNINIT, MakeUninitializeMessage);
        }

        #region Interface Methods

        /// <summary>
        /// Interface implementation to create the Impls that provide the actions to
        /// received messages.
        /// </summary>
        /// <param name="msgType">The incoming message type</param>
        /// <param name="clientHandle">The client that sent the message</param>
        /// <returns>IMessageImpl</returns>
        public IMessageImpl MakeMessageImpl(int msgType, long clientHandle)
        {
            IMessageImpl impl = default(IMessageImpl);
            MessageFactoryTypesEnum msgFactoryType;

            bool success = Enum.TryParse(msgType.ToString(), out msgFactoryType);

            Console.WriteLine("Getting IMPL for Client: {0}; TASK: {1}", clientHandle, msgType.ToString());

            if (!success)
            {
                return impl;
            }

            // The stored client impls we're looking to reuse.  New clients are created and
            // stored further below.
            Dictionary<MessageFactoryTypesEnum, IMessageImpl> client;

            // The success block.  If we have a client already and it has the impl,
            // just return the impl for re-use.
            bool clientFound = implStore.TryGetValue(clientHandle, out client);
            if (clientFound)
            {
                if (client.TryGetValue(msgFactoryType, out impl))
                {
                    Console.WriteLine("Found Client: {0}", clientHandle);

                    return impl;
                }
            }

            // The not found block.  We have to create a new impl regardless.  We may have
            // found a client already.  Add the impl if we have.  If not, create a new one
            // and add it to the impl Store.
            try
            {

                impl = GetMessageImpl(msgType);

                if (impl == default(IMessageImpl))
                {
                    return impl;
                }

                lock (lockObj)
                {
                    if (!clientFound)
                    {
                        client = new Dictionary<MessageFactoryTypesEnum, IMessageImpl>
                            {
                                { msgFactoryType, impl }
                            };
                        implStore.Add(clientHandle, client);

                        Console.WriteLine("Added Client: {0}", clientHandle);

                        return impl;
                    }

                    // We have a client  Add the impl.
                    client.Add(msgFactoryType, impl);
                    Console.WriteLine("Added Task: {0} to Client: {1}", msgType.ToString(), clientHandle);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("ImplFactory Exception: " + e.Message);
                impl = default(IMessageImpl);
            }
            return impl;
        }

        /// <summary>
        /// Interface implementation that does the actual creation of the message Impl objects.
        /// </summary>
        /// <param name="msgType">The message type value</param>
        /// <returns>IMessageImpl</returns>
        public IMessageImpl GetMessageImpl(int msgType)
        {
            if (Enum.TryParse(msgType.ToString(), out MessageFactoryTypesEnum type))
            {
                return MakeImpls[type].Invoke();
            }
            return default(IMessageImpl);
        }

        #endregion

        /// <summary>
        /// Remove the client stored in the ImplStore.  This should occur when the network client
        /// goes away.
        /// </summary>
        /// <param name="clientHandle">The handle to the client</param>
        /// <returns>bool</returns>
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


        #region Impl Makers

        /// <summary>
        /// Follow the Open-Close principle and create methods that return impls instead of creating
        /// a single switch method that returns new impls.
        /// A new message type will require a new impl and a new impl creation method to go with it.
        /// </summary>
        /// <returns>IMessageImpl</returns>
        public delegate IMessageImpl MakeImplD();

        readonly Dictionary<MessageFactoryTypesEnum, MakeImplD> MakeImpls = new Dictionary<MessageFactoryTypesEnum, MakeImplD>();

        IMessageImpl MakeExitMessage()
        {
            return new ExitMessageImpl();
        }

        IMessageImpl MakeAllUsersMessage()
        {
            return new AllUsersMessageImpl();
        }

        IMessageImpl MakeFileMessage()
        {
            return new FileMessageImpl();
        }

        IMessageImpl MakeUsersMessage()
        {
            return new GetUserNameMessageImpl();
        }

        IMessageImpl MakeGlobalMessage()
        {
            return new GlobalMessageImpl();
        }

        IMessageImpl MakeUserMessage()
        {
            return new UserMessageImpl();
        }

        IMessageImpl MakeUninitializeMessage()
        {
            return default(IMessageImpl);
        }

        #endregion

    }
}
