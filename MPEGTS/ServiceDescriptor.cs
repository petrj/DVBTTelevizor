using System;

namespace MPEGTS
{
    public class ServiceDescriptor
    {
        public byte Tag { get; set; }
        public byte Length { get; set; }
        public byte ServisType { get; set; }

        public byte ProviderNameLength { get; set; }
        public byte ServiceNameLength { get; set; }

        public string ProviderName { get; set; }
        public string ServiceName { get; set; }

        public int Number { get; set; } = -1;
    }
}


