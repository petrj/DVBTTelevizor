using System;
using System.Collections.Generic;

namespace MPEGTS
{
    public class ServiceListDescriptor
    {
        public byte Tag { get; set; }
        public int Length { get; set; }

        public Dictionary<int, byte> Service { get; set; } = new Dictionary<int, byte>(); // ID -> service type from table Table 87: Service type coding (ETSI EN 300 468 V1.15.1 (2016-03))

        public ServiceListDescriptor(List<byte> bytes)
        {

        }
    }
}
