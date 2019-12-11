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


        public byte[] Data { get; set; }
        public byte[] CRC { get; set; }

        public static string GetStringFromByteArray(byte[] bytes, int pos, int length)
        {
            var res = String.Empty;
            for (var i=pos;i<pos+length;i++)
            {
                res += Convert.ToChar(bytes[i]);
            }

            return res;
        }

        public static uint ComputeCRC(byte[] bytes)
        {
            uint crc32 = 0xffffffff;
            for (int i = 1 + bytes[0]; i < bytes.Length; i++)
            {
                byte b = bytes[i];
                for (int bit = 0; bit < 8; bit++)
                {
                    if ((crc32 >= 0x80000000) != (b >= 0x80))
                        crc32 = (crc32 << 1) ^ 0x04C11DB7;
                    else
                        crc32 = (crc32 << 1);
                    b <<= 1;
                }
            }
            return crc32;
        }

        public bool CRCIsValid()
        {
            var computedCRC = BitConverter.GetBytes(ComputeCRC(Data));

            return
                computedCRC[0] == CRC[3] &&
                computedCRC[1] == CRC[2] &&
                computedCRC[2] == CRC[1] &&
                computedCRC[3] == CRC[0];
        }
    }
}
