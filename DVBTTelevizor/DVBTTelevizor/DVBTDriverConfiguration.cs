using System;
using System.Collections.Generic;
using System.Text;

namespace DVBTTelevizor
{
    public class DVBTDriverConfiguration : JSONObject
    {
        public int ControlPort { get; set; }
        public int TransferPort { get; set; }
        public string DeviceName { get; set; }
        public int[] ProductIds { get; set; }
        public int[] VendorIds { get; set; }
    }
}
