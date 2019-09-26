using System;
using System.Collections.Generic;
using System.Text;

namespace DVBTTelevizor
{
    public enum RequestStateEnum
    {
        Error = -1,

        Ready = 0,
        SendingRequest = 1,
        ReadingResponse = 2,
        ResponseReceived = 3,
    }
}
