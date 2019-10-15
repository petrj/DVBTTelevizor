using System;
using System.Collections.Generic;
using System.Text;

namespace DVBTTelevizor
{
    public enum SearchProgramResultEnum
    {
        Error = -1,

        NoSignal = 0,
        NoProgramFound = 1,
        OK = 2
    }
}
