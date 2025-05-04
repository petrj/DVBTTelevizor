using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTLSDR.Common
{
    public class AudioDataDescription
    {
        public int SampleRate { get; set; } = 0;
        public short Channels { get; set; } = 0;
        public short BitsPerSample { get; set; } = 0;
    }
}
