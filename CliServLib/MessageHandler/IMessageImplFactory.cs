﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CliServLib
{
    public interface IMessageImplFactory
    {
        IMessageImpl MakeMessageImpl(MessageTypesEnum msgType, long clientHandle);

        IMessageImpl GetMessageImpl(MessageTypesEnum msgType);
    }
}
