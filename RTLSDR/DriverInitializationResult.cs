using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTLSDR
{
    public class DriverInitializationResult
    {
        public int[] SupportedTcpCommands { get; set; }
        public string DeviceName { get; set; }
        public string OutputRecordingDirectory { get; set; }
    }
}
