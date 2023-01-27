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
                sb.AppendLine($"{"Type",4} {"Provider".PadRight(20, ' '),20} {"ServiceName".PadRight(40, ' '),40} {"Program Number"}");
                sb.AppendLine($"{"----",4} {"--------".PadRight(20, '-'),20} {"-----------".PadRight(40, '-'),40} {"--------------"}");
                foreach (var desc in ServiceDescriptors)
                {
                    sb.AppendLine($"{desc.ServisType,4} {desc.ProviderName.PadRight(20, ' '),20} {desc.ServiceName.PadRight(40,' '),40} {desc.ProgramNumber,14}");
                }
            }

            return sb.ToString();
        }

        public override void Parse(List<byte> bytes)
        {
            //Console.WriteLine(MPEGTransportStreamPacket.WriteBytesToString(bytes));

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

            var posAfterTable = pos + SectionLength - 4;

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

            pos = pos + 3;  // reserved_future_use byte

            while (pos< posAfterTable)
            {
                var programNumber = ((bytes[pos + 0]) << 8) + bytes[pos + 1];

                // reserved_future_use
                // EIT_schedule_flag
                // EIT_present_following_flag
                // unning_status 3 uimsbf
                // free_CA_mode

                pos = pos + 3;

                var descriptorLoopLength = ((bytes[pos + 0] & 15) << 8) + bytes[pos + 1];

                pos = pos + 2;

                var posAfterdescriptorLoopLength = pos + descriptorLoopLength;

                while (pos < posAfterdescriptorLoopLength)
                {

                    var descriptorTag = bytes[pos + 0];
                    var descriptorLength = bytes[pos + 1];
                    var descriptorBytes = new byte[descriptorLength + 2]; // descriptorLength + descriptorTag + descriptorLength
                    bytes.CopyTo(pos, descriptorBytes, 0, descriptorLength + 2);

                    if (descriptorTag == 0x48) // service_descriptor
                    {
                        var serviceDescriptor = new ServiceDescriptor(descriptorBytes, programNumber, NetworkID);
                        ServiceDescriptors.Add(serviceDescriptor);
                    } else
                    {
                        //Console.WriteLine($"SDT: unknown tag descriptor: {descriptorTag:X} hex ({descriptorTag} dec)");
                    }

                    pos = pos + descriptorLength +2;
                }
            }
        }

    }
}
