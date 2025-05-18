using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTLSDR
{
    public class DriverSettings
    {
        public int Port { get; set; } = 1234;
        public int SDRSampleRate { get; set; } = 1056000;
        public string IP { get; set; } = "127.0.0.1";
        public int Streamport { get; set; } = 1235;
        public int Gain { get; set; } = 0;
        public bool AutoGain { get; set; } = true;
    }
}
