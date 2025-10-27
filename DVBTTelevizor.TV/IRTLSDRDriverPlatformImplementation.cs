using LoggerService;
using RTLSDR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.TV
{
    public interface IRTLSDRDriverPlatformImplementation
    {
        public ISDR GetRTLSDRDriver();
    }
}
