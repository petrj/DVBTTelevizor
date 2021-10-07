using System;
using System.Collections.Generic;
using System.Text;

namespace MPEGTS
{
    // https://www.etsi.org/deliver/etsi_en/300400_300499/300468/01.03.01_60/en_300468v010301p.pdf
    // page 20
    // page 27
    // page 53 - Service Descriptor

    // https://www.etsi.org/deliver/etsi_en/300400_300499/300468/01.15.01_60/en_300468v011501p.pdf
    // page 26

    public class SDTTable : DVBTTable
    {
        public int TableIdExt { get; set; }
        public byte ReservedExt { get; set; }

        public int ServiceId { get; set; }

        public byte RunningStatus { get; set; }
        public int DescriptorsLoopLength { get; set; }

        public List<ServiceDescriptor> ServiceDescriptors { get; set; } = new List<ServiceDescriptor>();


        public List<byte> TableData = new List<byte>();

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
                sb.AppendLine($"CRC OK                : {CRCIsValid()}");

                if (SectionSyntaxIndicator)
                {
                    sb.AppendLine($"TableIdExt             : {TableIdExt}");
                    sb.AppendLine($"ReservedExt            : {ReservedExt}");
                    sb.AppendLine($"Version                : {Version}");
                    sb.AppendLine($"CurrentIndicator       : {CurrentIndicator}");
                    sb.AppendLine($"SectionNumber          : {SectionNumber}");
                    sb.AppendLine($"LastSectionNumber      : {LastSectionNumber}");
                }

                sb.AppendLine($"DescriptorsLoopLength  : {DescriptorsLoopLength}");
            }

            sb.AppendLine($"NetworkID              : {NetworkID}");
            sb.AppendLine($"ServiceId              : {ServiceId}");

            sb.AppendLine();

            if (detailed)
            {
                foreach (var desc in ServiceDescriptors)
                {
                    sb.AppendLine($"__________________");
                    sb.AppendLine($"Tag           : {desc.Tag}");
                    sb.AppendLine($"Length        : {desc.Length}");
                    sb.AppendLine($"ServisType    : {desc.ServisType}");
                    sb.AppendLine($"ProviderName  : {desc.ProviderName}");
                    sb.AppendLine($"ServiceName   : {desc.ServiceName}");
                    sb.AppendLine($"ProgramNumber : {desc.ProgramNumber}");
                }
            } else
            {
                sb.AppendLine($"{"Type",4} {"Provider".PadRight(20, ' '),20} {"ServiceName".PadRight(20, ' '),20} {"Program Number",6}");
                sb.AppendLine($"{"----",4} {"--------".PadRight(20, '-'),20} {"-----------".PadRight(20, '-'),20} {"--------------",6}");
                foreach (var desc in ServiceDescriptors)
                {
                    sb.AppendLine($"{desc.ServisType,4} {desc.ProviderName.PadRight(20, ' '),20} {desc.ServiceName.PadRight(20,' '),20} {desc.ProgramNumber,6}");
                }
            }

            return sb.ToString();
        }

        public override void Parse(List<byte> bytes)
        {
            if (bytes == null || bytes.Count < 5)
                return;

            var pointerFiled = bytes[0];
            var pos = 1 + pointerFiled;

            if (bytes.Count < pos + 2)
                return;

            ID = bytes[pos];

            if (ID != 0x42)
                return;

            SectionSyntaxIndicator = ((bytes[pos + 1] & 128) == 128);
            Private = ((bytes[pos + 1] & 64) == 64);
            Reserved = Convert.ToByte((bytes[pos + 1] & 48) >> 4);
            SectionLength = Convert.ToInt32(((bytes[pos + 1] & 15) << 8) + bytes[pos + 2]);

            Data = new byte[SectionLength];
            CRC = new byte[4];
            bytes.CopyTo(0, Data, 0, SectionLength);
            bytes.CopyTo(SectionLength, CRC, 0, 4);

            pos = pos + 3;

            if (SectionSyntaxIndicator)
            {
                TableIdExt = (bytes[pos + 0] << 8) + bytes[pos + 1];
                ReservedExt = Convert.ToByte((bytes[pos + 2] & 192) >> 6);
                Version = Convert.ToByte((bytes[pos + 2] & 62) >> 1);
                CurrentIndicator = (bytes[pos + 2] & 1) == 1;
                SectionNumber = bytes[pos + 3];
                LastSectionNumber = bytes[pos + 4];

                pos = pos + 5;
            }

            NetworkID = (bytes[pos + 0] << 8) + bytes[pos + 1]; // original network Id
            ServiceId = (bytes[pos + 3] << 8) + bytes[pos + 4];
            pos += 3;

            // pointer + table id + sect.length + descriptors - crc
            var posAfterDescriptors = 4 + SectionLength - 4;

            // reading descriptors
            while (pos < posAfterDescriptors)
            {
                var sDescriptor = new ServiceDescriptor();
                sDescriptor.ProgramNumber = ((bytes[pos + 0]) << 8) + bytes[pos + 1];

                pos += 3;

                sDescriptor.Length = ((bytes[pos + 0] & 7) << 8) + bytes[pos + 1];

                pos += 2;

                var posAfterThisDescriptor = pos + sDescriptor.Length;

                sDescriptor.Tag = bytes[pos + 0];

                sDescriptor.ServisType = bytes[pos + 2];
                sDescriptor.ProviderNameLength = bytes[pos + 3];

                pos = pos + 4;

                if (bytes.Count < pos + sDescriptor.ProviderNameLength)
                    break;

                sDescriptor.ProviderName = MPEGTSESICharReader.ReadString(bytes.ToArray(), pos, sDescriptor.ProviderNameLength);
                if (String.IsNullOrEmpty(sDescriptor.ProviderName))
                {
                    // not supported encoding ?
                    sDescriptor.ProviderName = $"ServiceId ID {ServiceId}";
                }

                pos = pos + sDescriptor.ProviderNameLength;

                if (bytes.Count < pos)
                    break;

                sDescriptor.ServiceNameLength = bytes[pos + 0];

                if (bytes.Count < pos + 1 + sDescriptor.ServiceNameLength)
                    break;

                pos++;

                sDescriptor.ServiceName = MPEGTSESICharReader.ReadString(bytes.ToArray(), pos, sDescriptor.ServiceNameLength);

                if (String.IsNullOrEmpty(sDescriptor.ServiceName))
                {
                    // not supported encoding ?
                    sDescriptor.ServiceName = $"Channel {sDescriptor.ProgramNumber}";
                }

                ServiceDescriptors.Add(sDescriptor);

                pos = posAfterThisDescriptor;
            }
        }

    }
}
