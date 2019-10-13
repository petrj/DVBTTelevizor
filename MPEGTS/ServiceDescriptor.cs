using System;

namespace MPEGTS
{
    public class ServiceDescriptor
    {
        public byte Tag { get; set; }
        public int Length { get; set; }
        public byte ServisType { get; set; }

        public byte ProviderNameLength { get; set; }
        public byte ServiceNameLength { get; set; }

        public string ProviderName { get; set; }
        public string ServiceName { get; set; }

        public int ProgramNumber { get; set; } = -1;
    }
}


