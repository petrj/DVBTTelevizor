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

                res.Add(DVBTBandwidthEnum.BandWidth_8000000, 8000000);
                res.Add(DVBTBandwidthEnum.BandWidth_7000000,   7000000);
                res.Add(DVBTBandwidthEnum.BandWidth_6000000,   6000000);
                res.Add(DVBTBandwidthEnum.BandWidth_5000000,   5000000);
                res.Add(DVBTBandwidthEnum.BandWidth_10000000, 10000000);
                res.Add(DVBTBandwidthEnum.BandWidth_1700000,   1700000);

                return res;
            }
        }

        public static Dictionary<DVBTBandwidthEnum, string> BandWidthTitle
        {
            get
            {
                var res = new Dictionary<DVBTBandwidthEnum, string>();

                res.Add(DVBTBandwidthEnum.BandWidth_8000000, "8 MHz");
                res.Add(DVBTBandwidthEnum.BandWidth_7000000,  "7 MHz");
                res.Add(DVBTBandwidthEnum.BandWidth_6000000,  "6 MHz");
                res.Add(DVBTBandwidthEnum.BandWidth_5000000, "5 MHz");
                res.Add(DVBTBandwidthEnum.BandWidth_10000000, "10 MHz");
                res.Add(DVBTBandwidthEnum.BandWidth_1700000,  "1.7 MHz");

                return res;
            }
        }

        public static Dictionary<string, DVBTBandwidthEnum> TitleBandWidth
        {
            get
            {
                var res = new Dictionary<string, DVBTBandwidthEnum>();

                res.Add("8 MHz", DVBTBandwidthEnum.BandWidth_8000000);
                res.Add("7 MHz", DVBTBandwidthEnum.BandWidth_7000000);
                res.Add("6 MHz", DVBTBandwidthEnum.BandWidth_6000000);
                res.Add("5 MHz", DVBTBandwidthEnum.BandWidth_5000000);
                res.Add("10 MHz", DVBTBandwidthEnum.BandWidth_10000000);
                res.Add("1.7 MHz", DVBTBandwidthEnum.BandWidth_1700000);

                return res;
            }
        }

        public static Dictionary<string, long> TitleBandWidthHz
        {
            get
            {
                var res = new Dictionary<string, long>();

                res.Add("8 MHz", 8000000);
                res.Add("7 MHz", 7000000);
                res.Add("6 MHz", 6000000);
                res.Add("5 MHz", 5000000);
                res.Add("10 MHz", 10000000);
                res.Add("1.7 MHz", 5000000);

                return res;
            }
        }
    }
}
