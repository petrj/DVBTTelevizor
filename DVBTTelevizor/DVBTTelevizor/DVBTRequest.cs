using System;
using System.Collections.Generic;
using System.Text;

namespace DVBTTelevizor
{
    public class DVBTRequest
    {
        public List<byte> Bytes { get; set; } = new List<byte>();
        public int ResponseBytesExpectedCount { get; set; }

        public char[] BytesAsCharArray
        {
            get
            {
                if (Bytes.Count>0)
                {
                    var res = new char[Bytes.Count];
                    for (var i = 0; i < Bytes.Count; i++) res[i] = Convert.ToChar(Bytes[i]);
                    return res;
                } else
                {
                    return new char[];
                }
            }
        }
    }
}
