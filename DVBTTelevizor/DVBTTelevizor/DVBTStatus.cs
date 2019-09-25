using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DVBTTelevizor
{
    public class DVBTStatus : DVBTResponse
    {
        public long snr { get; set; }
        public long bitErrorRate { get; set; }
        public long droppedUsbFps { get; set; }
        public long rfStrengthPercentage { get; set; }
        public long hasSignal { get; set; }
        public long hasCarrier { get; set; }
        public long hasSync { get; set; }
        public long hasLock { get; set; }

        public void ParseFromByteArray(byte[]ar, int offset)
        {
            SuccessFlag = GetBigEndianLongFromByteArray(ar, offset) == 1;

            snr = GetBigEndianLongFromByteArray(ar, offset+8);
            bitErrorRate = GetBigEndianLongFromByteArray(ar, offset+16);
            droppedUsbFps = GetBigEndianLongFromByteArray(ar, offset+3*8);
            rfStrengthPercentage = GetBigEndianLongFromByteArray(ar, offset + 4*8);
            hasSignal = GetBigEndianLongFromByteArray(ar, offset + 5*8);
            hasCarrier = GetBigEndianLongFromByteArray(ar, offset + 6 * 8);
            hasSync = GetBigEndianLongFromByteArray(ar, offset + 7 * 8);
            hasLock = GetBigEndianLongFromByteArray(ar, offset + 8 * 8);
        }

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
