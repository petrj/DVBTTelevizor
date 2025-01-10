using System;
using System.Collections.Generic;
using System.Text;

namespace DVBTTelevizor
{
    public class DVBTDriverCapabilities : DVBTDriverResponse
    {
        public long supportedDeliverySystems { get; set; }
        public long minFrequency { get; set; }
        public long maxFrequency { get; set; }
        public long frequencyStepSize { get; set; }
        public long vendorId { get; set; }
        public long productId { get; set; }

        public void ParseFromByteArray(byte[] ar, int offset)
        {
            SuccessFlag = GetBigEndianLongFromByteArray(ar, offset) == 1;

            supportedDeliverySystems = GetBigEndianLongFromByteArray(ar, offset + 8);
            minFrequency = GetBigEndianLongFromByteArray(ar, offset + 16);
            maxFrequency = GetBigEndianLongFromByteArray(ar, offset + 3 * 8);
            frequencyStepSize = GetBigEndianLongFromByteArray(ar, offset + 4 * 8);
            vendorId = GetBigEndianLongFromByteArray(ar, offset + 5 * 8);
            productId = GetBigEndianLongFromByteArray(ar, offset + 6 * 8);
        }
    }
}
