using System;
using System.Collections.Generic;
using System.Text;

namespace DVBTTelevizor
{

    public class DVBTVersion : DVBTResponse
    {
        public long Version { get; set; }
        public long AllRequestsLength { get; set; }

    }
}
