using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor
{
    public class TuningSettings
    {
        public TuneModeEnum TuningMode { get; set; } = TuneModeEnum.Automatic;

        public bool DVBT { get; set; } = true;
        public bool DVBT2 { get; set; } = true;

        public bool TuneDVBTPreferred { get; set; } = false;

        public long BandwidthKHz { get; set; } = 8000;

        public long FrequencyFromKHz { get; set; } = 474000;
        public long FrequencyToKHz { get; set; } = 852000;

        public long FrequencyMinKHz { get; set; } = 400000;
        public long FrequencyMaxKHz { get; set; } = 900000;

        public long FrequencyKHz { get; set; } = 474000;

        public static long DefaultFrequencyKHz { get; set; } = 474000;
        public static long DefaultBandwidthKHz { get; set; } = 8000;

        public static long DefaultFrequencyFromKHz { get; set; } = 474000;
        public static long DefaultFrequencyToKHz { get; set; } = 852000;
    }
}
