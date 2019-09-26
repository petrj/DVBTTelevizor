using System;
using System.Collections.Generic;
using System.Text;

namespace DVBTTelevizor
{
    public class DVBTResponse : JSONObject
    {
        public bool SuccessFlag { get; set; }
        public DateTime ResponseTime { get; set; }

        public List<byte> Bytes { get; set; } = new List<byte>();

        public DVBTResponse()
        {

        }
    }
}
