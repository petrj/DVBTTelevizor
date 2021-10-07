using System;
using System.Collections.Generic;

namespace MPEGTS
{
    public class ServiceListDescriptor
    {
        public byte Tag { get; set; }
        public int Length { get; set; }

        public SortedDictionary<int, byte> Services { get; set; } = new SortedDictionary<int, byte>(); // ID -> service type from table Table 87: Service type coding (ETSI EN 300 468 V1.15.1 (2016-03))
        public SortedDictionary<int, ServiceTypeEnum> ServiceTypes { get; set; } = new SortedDictionary<int, ServiceTypeEnum>();

        public ServiceListDescriptor(byte[] bytes)
        {
            var pos = 0;

            Tag= bytes[pos + 0];
            Length = bytes[pos + 1];

            pos = pos + 2;

            while (pos+3 < Length)
            {
                var serviceId = Convert.ToInt32(((bytes[pos + 0]) << 8) + (bytes[pos + 1]));
                var serviceType = bytes[pos + 2];

                Services[serviceId] = serviceType;

                try
                {
                    ServiceTypes[serviceId] = (ServiceTypeEnum)serviceType;
                } catch
                {
                    ServiceTypes[serviceId] = ServiceTypeEnum.Other;
                }

                pos = pos + 3;
            }
        }
    }
}
