﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcpLib
{
    public interface IDataGetter
    {
        MessageData GetData();
        MessageData GetData(long msgType);
        void SetData(object data);
    }
}
