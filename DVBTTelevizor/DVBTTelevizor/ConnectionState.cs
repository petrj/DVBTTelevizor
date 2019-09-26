using System;
using System.Collections.Generic;
using System.Text;

namespace DVBTTelevizor
{
    public enum DVBTDriverState
    {
        Error = -1,

        Disconnected = 0,
        Connecting = 1,
        Ready = 2,
        SendingRequest = 3,
        ReadingResponse = 4,
        ResponseReceived = 5,
    }
}
