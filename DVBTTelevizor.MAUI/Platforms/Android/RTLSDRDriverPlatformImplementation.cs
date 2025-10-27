using DVBTTelevizor.TV;
using LoggerService;
using RTLSDR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI.Platforms.Windows
{
    public class RTLSDRDriverPlatformImplementation : IRTLSDRDriverPlatformImplementation
    {
        private ILoggingProvider _loggingProvider;

        public RTLSDRDriverPlatformImplementation(ILoggingProvider loggingProvider)
        {
            _loggingProvider = loggingProvider;
        }

        public ISDR GetRTLSDRDriver()
        {
            return new RTLSDRDriver(_loggingProvider.GetLoggingService());
        }
    }
}
