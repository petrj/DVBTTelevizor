using System;
using System.Collections.Generic;
using System.Text;

namespace MPEGTS
{
    public class DVBTTable
    {
        public byte ID { get; set; }
        public bool SectionSyntaxIndicator { get; set; }
        public bool Private { get; set; }
        public byte Reserved { get; set; }

        public int SectionLength { get; set; }
        public int NetworkID { get; set; }
        public int TableIDExt { get; set; }
        public int SectionNumber { get; set; }
        public int LastSectionNumber { get; set; }
        public byte Version { get; set; }
        public bool CurrentIndicator { get; set; }

        public static string GetStringFromByteArray(byte[] bytes, int pos, int length)
        {
            var res = String.Empty;
            for (var i=pos;i<pos+length;i++)
            {
                res += Convert.ToChar(bytes[i]);
            }

            return res;
        }
    }
}
