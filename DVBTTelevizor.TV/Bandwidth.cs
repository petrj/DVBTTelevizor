using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor
{
    public class Bandwidth
    {
        public static Dictionary<DVBTBandwidthEnum, long> BandWidthHz
        {
            get
            {
                var res = new Dictionary<DVBTBandwidthEnum, long>();

                foreach (var kvp in Enum.GetValues(typeof(DVBTBandwidthEnum)))
                {

                }

                return res;
            }
        }
    }
}
