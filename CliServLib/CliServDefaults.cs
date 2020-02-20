using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CliServLib
{
    public static class CliServDefaults
    {
        public const int DfltPort = 8001;
        public const int BufferSize = 5120;
        public const int ReceiveTimeoutMs = 3000;
        public const int SendTimeoutMs = 3000;
        public const int ConnectTimeoutMs = 3000;
    }
}
