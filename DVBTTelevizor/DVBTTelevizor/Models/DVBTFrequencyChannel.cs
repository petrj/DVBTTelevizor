using System;
using System.Collections.Generic;
using System.Text;

namespace DVBTTelevizor.Models
{
    public class DVBTFrequencyChannel
    {
        public string Description
        {
            get
            {
                return $"{ChannelNumber.ToString()} ({FrequencyMhZ} MHz)";
            }
        }
        public long FrequencyMhZ { get; set; }
        public long ChannelNumber { get; set; }
    }
}
