using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace DVBTTelevizor
{
    public class DVBTDriverConfiguration
    {
        public int ControlPort { get; set; }
        public int TransferPort { get; set; }
        public string DeviceName { get; set; }
        public int[] ProductIds { get; set; }
        public int[] VendorIds { get; set; }

        public string PublicDirectory { get; set; }
    }
}
