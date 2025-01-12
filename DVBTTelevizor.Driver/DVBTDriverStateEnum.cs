using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor
{
    [Flags]
    public enum DVBTDriverStateEnum
    {
        Unknown = 0,
        Disconnected = 1,
        Connected = 2,
        Playing = 4,
        Recording = 8,
        ScanningEPG = 16
    }
}
