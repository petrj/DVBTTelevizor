using System;
using System.Collections.Generic;
using System.Text;

namespace DVBTTelevizor
{

    public class DVBTVersion : JSONObject
    {
        public long SuccessFlag { get; set; }
        public long Version { get; set; }
        public long AllRequestsLength { get; set; }

    }
}
