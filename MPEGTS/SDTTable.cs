using System;
using System.Collections.Generic;

namespace MPEGTS
{
    // https://www.etsi.org/deliver/etsi_en/300400_300499/300468/01.03.01_60/en_300468v010301p.pdf
    // page 20
    // page 53 - Service Descriptor

    public class SDTTable : TableHeader
    {
        public int TableIdExt { get; set; }
        public byte ReservedExt { get; set; }

        public int ServiceId { get; set; }

        public byte RunningStatus { get; set; }
        public int DescriptorsLoopLength { get; set; }

        public List<ServiceDescriptor> ServiceDescriptors { get; set; } = new List<ServiceDescriptor>();


        public List<byte> TableData = new List<byte>();

        public void WriteToConsole()
        {
            Console.WriteLine($"ID                    : {ID}");
            Console.WriteLine($"SectionSyntaxIndicator: {SectionSyntaxIndicator}");
            Console.WriteLine($"Private               : {Private}");
            Console.WriteLine($"Reserved              : {Reserved}");
            Console.WriteLine($"SectionLength         : {SectionLength}");

            if (SectionSyntaxIndicator)
            {
                Console.WriteLine($"TableIdExt             : {TableIdExt}");
                Console.WriteLine($"ReservedExt            : {ReservedExt}");
                Console.WriteLine($"Version                : {Version}");
                Console.WriteLine($"CurrentIndicator       : {CurrentIndicator}");
                Console.WriteLine($"SectionNumber          : {SectionNumber}");
                Console.WriteLine($"LastSectionNumber      : {LastSectionNumber}");
            }

            Console.WriteLine($"NetworkID              : {NetworkID}");
            Console.WriteLine($"ServiceId              : {ServiceId}");
            Console.WriteLine($"DescriptorsLoopLength  : {DescriptorsLoopLength}");

            foreach (var desc in ServiceDescriptors)
            {
                Console.WriteLine($"");
                Console.WriteLine($"Service descriptor");
                Console.WriteLine($"__________________");
                Console.WriteLine($"Tag           : {desc.Tag}");
                Console.WriteLine($"Length        : {desc.Length}");
                Console.WriteLine($"ServisType    : {desc.ServisType}");
                Console.WriteLine($"ProviderName  : {desc.ProviderName}");
                Console.WriteLine($"ServiceName   : {desc.ServiceName}");
                Console.WriteLine($"ProgramNumber : {desc.ProgramNumber}");
            }
        }

        public static SDTTable Parse(List<byte> bytes)
        {
            if (bytes == null || bytes.Count < 5)
                return null;

            var res = new SDTTable();

            var pointerFiled = bytes[0];
            var pos = 1 + pointerFiled;

            if (bytes.Count < pos+2)
                return null;

            res.ID = bytes[pos];

            // read next 2 bytes
            var tableHeader1 = bytes[pos + 1];
            var tableHeader2 = bytes[pos + 2];

            res.SectionSyntaxIndicator = ((tableHeader1 & 128)==128);
            res.Private = ((tableHeader1 & 64) == 64);
            res.Reserved = Convert.ToByte((tableHeader1 & 48) >> 4);
            res.SectionLength = Convert.ToInt32(((tableHeader1 & 15) << 8) + tableHeader2);

            pos = pos + 3;

            if (res.SectionSyntaxIndicator)
            {
                if (bytes.Count < pos + 4)
                    return null;

                var tableIdExt0 = bytes[pos];
                var tableIdExt1 = bytes[pos+1];
                var tableIdExt2 = bytes[pos + 2];

                res.TableIdExt = (tableIdExt0 << 8) + tableIdExt1;
                res.ReservedExt = Convert.ToByte((tableIdExt2 & 192) >> 6);
                res.Version = Convert.ToByte((tableIdExt2 & 62) >> 1);
                res.CurrentIndicator = (tableIdExt2 & 1) == 1;
                res.SectionNumber = bytes[pos + 3];
                res.LastSectionNumber = bytes[pos + 4];

                pos = pos + 5;
            }

            if (bytes.Count < pos + 7)
                return null;

            res.NetworkID = (bytes[pos] << 8) + bytes[pos + 1];
            res.ServiceId = (bytes[pos+3] << 8) + bytes[pos + 4];

            res.RunningStatus = Convert.ToByte((bytes[pos + 6] & 224) >> 5);
            res.DescriptorsLoopLength = ((bytes[pos + 6] & 15)   << 8) + bytes[pos + 7];

            pos = pos + 8;

            if (bytes.Count < pos + 4)
                return null;

            // reading descriptors
            while (pos+4<bytes.Count)
            {
                // 4 bytes
                if (bytes.Count < pos + 4)
                    break;

                var sDescriptor = new ServiceDescriptor();
                sDescriptor.Tag = bytes[pos + 0];
                sDescriptor.Length = bytes[pos + 1];
                sDescriptor.ServisType = bytes[pos + 2];
                sDescriptor.ProviderNameLength = bytes[pos + 3];

                pos = pos + 4;

                if (bytes.Count < pos + sDescriptor.ProviderNameLength)
                    break;

                for (var i = 0; i < sDescriptor.ProviderNameLength; i++)
                {
                    sDescriptor.ProviderName += Convert.ToChar(bytes[pos + i]);
                }

                pos = pos + sDescriptor.ProviderNameLength;

                if (bytes.Count < pos)
                    break;

                sDescriptor.ServiceNameLength = bytes[pos + 0];

                if (bytes.Count < pos + 1 + sDescriptor.ServiceNameLength)
                    break;

                pos++;

                for (var i = 0; i < sDescriptor.ServiceNameLength; i++)
                {
                    sDescriptor.ServiceName += Convert.ToChar(bytes[pos + i]);
                }

                var numPos = pos + sDescriptor.ServiceNameLength;
                sDescriptor.ProgramNumber = Convert.ToInt32(((bytes[numPos + 0]) << 8) + (bytes[numPos + 1]));

                pos = pos + sDescriptor.ServiceNameLength + 5;

                res.ServiceDescriptors.Add(sDescriptor);

            }

            return res;
        }
    }
}
