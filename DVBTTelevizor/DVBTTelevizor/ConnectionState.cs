using System;
using System.Collections.Generic;
using System.Text;

namespace DVBTTelevizor
{
    public enum ConnectionState
    {
        Error = -1,
        Disconnected = 0,
        Connecting = 1,
        Ready = 2,
        Busy = 3,        
    }
}
