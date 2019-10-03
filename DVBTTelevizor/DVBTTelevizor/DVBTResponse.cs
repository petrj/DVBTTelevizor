using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DVBTTelevizor
{
    public class DVBTResponse : JSONObject
    {
        public bool SuccessFlag { get; set; }
        public DateTime ResponseTime { get; set; }

        public List<byte> Bytes { get; set; } = new List<byte>();

        public static byte[] GetByteArrayFromBigEndianLong(long l)
        {
            var reversedArray = BitConverter.GetBytes(l);
            return reversedArray.Reverse().ToArray();
        }

        public static long GetBigEndianLongFromByteArray(byte[] ba, int offset)
        {
            var reversedArray = new List<byte>();
            for (var i = offset + 8 - 1; i >= offset; i--)
            {
                reversedArray.Add(ba[i]);
            }

            return BitConverter.ToInt64(reversedArray.ToArray(), 0);
        }
    }
}
