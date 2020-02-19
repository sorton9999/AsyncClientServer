using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskCommon
{
    /// <summary>
    /// Message type defs.
    /// </summary>
    public enum MessageTypesEnum
    {
        MSG_TYPE_UNINIT = 0,
        GLOBAL_MSG_TYPE,
        USER_MSG_TYPE,
        ALL_USERS_MSG_TYPE,
        GET_USERS_MSG_TYPE = 10,
        CLIENT_EXIT_MSG_TYPE = 99,
        FILE_MSG_TYPE = 100
    };
}
