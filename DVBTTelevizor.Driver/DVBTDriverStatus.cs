using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DVBTTelevizor
{
    public class DVBTDriverStatus : DVBTDriverResponse
    {
        public long snr { get; set; } = 0;
        public long bitErrorRate { get; set; } = 0;
        public long droppedUsbFps { get; set; } = 0;
        public long rfStrengthPercentage { get; set; } = 0;
        public long hasSignal { get; set; } = 0;
        public long hasCarrier { get; set; } = 0;
        public long hasSync { get; set; } = 0;
        public long hasLock { get; set; } = 0;

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
    }
}
