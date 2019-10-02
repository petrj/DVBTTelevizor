using System;
using System.Collections.Generic;
using System.Text;

namespace DVBTTelevizor
{
    public enum DVBTDriverRequestTypeEnum
    {
        REQ_PROTOCOL_VERSION = 0,
        REQ_EXIT = 1,
        REQ_TUNE = 2,
        REQ_GET_STATUS = 3,
        REQ_SET_PIDS = 4,
        REQ_GET_CAPABILITIES = 5
    }
}
