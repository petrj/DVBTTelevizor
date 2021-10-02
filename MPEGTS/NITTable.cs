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

        public override void Parse(List<byte> bytes)
        {
            if (bytes == null || bytes.Count < 5)
                return;

            var pointerFiled = bytes[0];
            var pos = 1 + pointerFiled;

            if (bytes.Count < pos + 2)
                return;

            ID = bytes[pos];

            // read next 2 bytes
            var tableHeader1 = bytes[pos + 1];
            var tableHeader2 = bytes[pos + 2];

            SectionSyntaxIndicator = ((tableHeader1 & 128) == 128);
            Private = ((tableHeader1 & 64) == 64);
            Reserved = Convert.ToByte((tableHeader1 & 48) >> 4);
            SectionLength = Convert.ToInt32(((tableHeader1 & 15) << 8) + tableHeader2);

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

                if (descriptorTag == 64)
                {
                    // 64 (dec) 40 (hex) - network_name_descriptor
                    NetworkName = GetStringFromByteArray(bytes.ToArray(), pos + 2, descriptorLength);
                }

                if (descriptorTag == 74)
                {
                    // 74 (dec) 4A (hex) - linkage_descriptor
                    TransportStreamId = Convert.ToInt32(((bytes[pos + 2]) << 8) + bytes[pos + 3]);
                    OriginalNetworkId = Convert.ToInt32(((bytes[pos + 4]) << 8) + bytes[pos + 5]);
                    ServiceId = Convert.ToInt32(((bytes[pos + 6]) << 8) + bytes[pos + 7]);
                    LinkageType = bytes[pos + 8];
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

                    //Console.WriteLine($"Found descriptor: {descriptorTag}");
                    //Console.WriteLine($"          length: {descriptorLength}");
                    //Console.WriteLine($"        position: {pos + 2}");
                    //Console.WriteLine($"------------------------------------");

                    // TODO: read descriptors of given descriptorTag
                    // 90 (dec) 5A (hex)  - terrestrial_delivery_system_descriptor
                    // 98 (dec) 62 (hex) - frequency_list_descriptor

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
            sb.AppendLine($"ServiceId              : {ServiceId}");

            return sb.ToString();
        }
    }
}
