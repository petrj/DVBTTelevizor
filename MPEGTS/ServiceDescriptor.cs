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

        public ServiceDescriptor(byte[] bytes, int programNumber, int networkId)
        {
            ProgramNumber = programNumber;
            var pos = 0;

            Tag =   bytes[pos + 0];
            Length = bytes[pos + 1];
            ServisType = bytes[pos + 2];

            ProviderNameLength = bytes[pos + 3];

            pos = pos + 4;

            ProviderName = MPEGTSCharReader.ReadString(bytes, pos, ProviderNameLength);
            if (String.IsNullOrEmpty(ProviderName))
            {
                // not supported encoding ?
                ProviderName = $"Network #{networkId}";
            }

            pos = pos + ProviderNameLength;

            ServiceNameLength = bytes[pos + 0];
            
            pos = pos +1;

            ServiceName = MPEGTSCharReader.ReadString(bytes, pos, ServiceNameLength);

            if (String.IsNullOrEmpty(ServiceName))
            {
                // not supported encoding ?
                ServiceName = $"Program #{programNumber}";
            }
        }
    }
}


