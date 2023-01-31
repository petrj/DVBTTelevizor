using System;
using System.Collections.Generic;
using System.Text;

namespace MPEGTS
{
    public class NITTable : DVBTTable
    {
        public string NetworkName { get; set; }

        public int TransportStreamId { get; set; }
        public int OriginalNetworkId { get; set; }
        public int ServiceId { get; set; }
        public byte LinkageType { get; set; }

        public ServiceListDescriptor ServiceList { get; set; } = null;

        public override void Parse(List<byte> bytes)
        {
            if (bytes == null || bytes.Count < 5)
                return;

            var pointerFiled = bytes[0];
            var pos = 1;
            if (pointerFiled != 0)
            {
                pos = pos + pointerFiled + 1;
            }

            if (bytes.Count < pos + 2)
                return;

            ID = bytes[pos];

            if (ID != 0x40)  // 0x40 = network_information_section - actual_network
                return;

            SectionSyntaxIndicator = ((bytes[pos + 1] & 128) == 128);
            Private = ((bytes[pos + 1] & 64) == 64);
            Reserved = Convert.ToByte((bytes[pos + 1] & 48) >> 4);
            SectionLength = Convert.ToInt32(((bytes[pos + 1] & 15) << 8) + bytes[pos + 2]);  // the number of bytes of the section, starting immediately following the section_length field and including the CRC.

            Data = new byte[SectionLength];
            CRC = new byte[4];

            bytes.CopyTo(0, Data, 0, SectionLength);
            bytes.CopyTo(SectionLength, CRC, 0, 4);

            pos = pos + 3;

            NetworkID = (bytes[pos + 0] << 8) + bytes[pos + 1];

            pos = pos + 2;

            Version = Convert.ToByte((bytes[pos + 0] & 64) >> 1);
            CurrentIndicator = (bytes[pos + 0] & 1) == 1;

            SectionNumber = bytes[pos + 1];
            LastSectionNumber = bytes[pos + 2];

            pos = pos + 3;

            var networkDecriptorsLength = Convert.ToInt32(((bytes[pos + 0] & 15) << 8) + bytes[pos + 1]);

            pos = pos + 2;

            // network decriptors folowing

            var posAfterNetworkDescriptors = pos + networkDecriptorsLength;

            while (pos < posAfterNetworkDescriptors)
            {
                var descriptorTag = bytes[pos + 0];
                var descriptorLength = bytes[pos + 1];

                if (descriptorTag == 0x40) // network_name_descriptor
                {
                    NetworkName = MPEGTSCharReader.ReadString(bytes.ToArray(), pos + 2 , descriptorLength);

                    if (String.IsNullOrEmpty(NetworkName))
                    {
                        // bad encoding?
                        NetworkName = $"Network #{NetworkID}";
                    }

                } else
                if (descriptorTag == 0x40) // linkage_descriptor
                {
                    // 74 (dec) 4A (hex) - linkage_descriptor
                    TransportStreamId = Convert.ToInt32(((bytes[pos + 2]) << 8) + bytes[pos + 3]);
                    OriginalNetworkId = Convert.ToInt32(((bytes[pos + 4]) << 8) + bytes[pos + 5]);
                    ServiceId = Convert.ToInt32(((bytes[pos + 6]) << 8) + bytes[pos + 7]);
                    LinkageType = bytes[pos + 8];
                } else
                if (descriptorTag == 0x41)
                {
                    // Service list descriptor
                    var descriptorBytes = new byte[descriptorLength];
                    bytes.CopyTo(pos, descriptorBytes, 0, descriptorLength);

                    ServiceList = new ServiceListDescriptor(descriptorBytes);
                } else
                {
                    // unsupported descriptor
                }

                pos += descriptorLength + 2;
            }

            var transportStreamLoopLength = Convert.ToInt32(((bytes[pos + 0] & 15) << 8) + bytes[pos + 1]);

            pos += 2;

            var posAfterTransportStreams = pos + transportStreamLoopLength;

            while (pos < posAfterTransportStreams)
            {
                TransportStreamId = Convert.ToInt32(((bytes[pos + 0]) << 8) + bytes[pos + 1]);
                OriginalNetworkId = Convert.ToInt32(((bytes[pos + 2]) << 8) + bytes[pos + 3]);

                var transportDescriptorsLength = Convert.ToInt32(((bytes[pos + 4] & 15) << 8) + bytes[pos + 5]);

                pos += 6;

                var posAfterTransportDescriptors = pos + transportDescriptorsLength;

                while (pos < posAfterTransportDescriptors)
                {
                    var descriptorTag = bytes[pos + 0];
                    var descriptorLength = bytes[pos + 1];

                    if (descriptorTag == 0x41)
                    {
                        // Service list descriptor
                        var descriptorBytes = new byte[descriptorLength];
                        bytes.CopyTo(pos, descriptorBytes, 0, descriptorLength);

                        ServiceList = new ServiceListDescriptor(descriptorBytes);
                    } else
                    {
                        //Console.WriteLine($"NIT: unknown tag descriptor: {descriptorTag:X} hex ({descriptorTag} dec)");
                    }

                    //Console.WriteLine($"Found NIT transport descriptor: {descriptorTag} ({Convert.ToString(descriptorTag,16)})");

                    pos += descriptorLength + 2;
                }
            }
         }

        public void WriteToConsole(bool detailed = false)
        {
            Console.WriteLine(WriteToString(detailed));
        }

        public string WriteToString(bool detailed = false)
        {
            var sb = new StringBuilder();

            if (detailed)
            {
                sb.AppendLine($"ID                    : {ID}");
                sb.AppendLine($"SectionSyntaxIndicator: {SectionSyntaxIndicator}");
                sb.AppendLine($"Private               : {Private}");
                sb.AppendLine($"Reserved              : {Reserved}");
                sb.AppendLine($"SectionLength         : {SectionLength}");

                if (SectionSyntaxIndicator)
                {
                    sb.AppendLine($"Version                : {Version}");
                    sb.AppendLine($"CurrentIndicator       : {CurrentIndicator}");
                    sb.AppendLine($"SectionNumber          : {SectionNumber}");
                    sb.AppendLine($"LastSectionNumber      : {LastSectionNumber}");
                }

                sb.AppendLine($"LinkageType            : {LinkageType}");
            }

            sb.AppendLine($"NetworkName            : {NetworkName}");
            sb.AppendLine($"TransportStreamId      : {TransportStreamId}");
            sb.AppendLine($"OriginalNetworkId      : {OriginalNetworkId}");

            if (ServiceId != 0)
            {
                sb.AppendLine($"ServiceId              : {ServiceId}");
            }

            if (ServiceList != null && ServiceList.Services != null && ServiceList.Services.Count>0)
            {
                Console.WriteLine($"{"Program number".PadRight(14, ' '),14} {"Service type",14} {"Type".PadRight(14, ' '),40}");
                Console.WriteLine($"{"--------------".PadRight(14, '-'),14} {"------------",14} {"--------------".PadRight(40, '-'),40} ");

                foreach (var kvp in ServiceList.Services)
                {
                    Console.WriteLine($"{kvp.Key,14} {kvp.Value,14} {ServiceList.ServiceTypes[kvp.Key].ToString().PadRight(40, ' '),40}");
                }
            }

            return sb.ToString();
        }
    }
}
