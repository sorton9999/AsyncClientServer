﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcpLib;

namespace TaskServer
{
    public class DataGetter : IDataGetter
    {
        public MessageData GetData()
        { 
            throw new NotImplementedException("Implement me when adding send capabilities.");

        }

        public MessageData GetData(long msgType)
        {
            throw new NotImplementedException("Implement me when adding send capabilities.");
        }

        public void SetData(object data)
        {
            throw new NotImplementedException("Implement me when adding send capabilities.");
        }
    }
}
