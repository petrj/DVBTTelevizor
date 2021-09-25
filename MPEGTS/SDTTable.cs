using System;
using System.Collections.Generic;

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

        public void WriteToConsole()
        {
            Console.WriteLine($"ID                    : {ID}");
            Console.WriteLine($"SectionSyntaxIndicator: {SectionSyntaxIndicator}");
            Console.WriteLine($"Private               : {Private}");
            Console.WriteLine($"Reserved              : {Reserved}");
            Console.WriteLine($"SectionLength         : {SectionLength}");
            Console.WriteLine($"CRC OK                : {CRCIsValid()}");

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

            res.SectionSyntaxIndicator = ((bytes[pos + 1] & 128)==128);
            res.Private = ((bytes[pos + 1] & 64) == 64);
            res.Reserved = Convert.ToByte((bytes[pos + 1] & 48) >> 4);
            res.SectionLength = Convert.ToInt32(((bytes[pos + 1] & 15) << 8) + bytes[pos + 2]);

            res.Data = new byte[res.SectionLength];
            res.CRC = new byte[4];
            bytes.CopyTo(0, res.Data, 0, res.SectionLength);
            bytes.CopyTo(res.SectionLength, res.CRC, 0, 4);

            pos = pos + 3;

            if (res.SectionSyntaxIndicator)
            {
                res.TableIdExt = (bytes[pos + 0] << 8) + bytes[pos + 1];
                res.ReservedExt = Convert.ToByte((bytes[pos + 2] & 192) >> 6);
                res.Version = Convert.ToByte((bytes[pos + 2] & 62) >> 1);
                res.CurrentIndicator = (bytes[pos + 2] & 1) == 1;
                res.SectionNumber = bytes[pos + 3];
                res.LastSectionNumber = bytes[pos + 4];

                pos = pos + 5;
            }

            res.NetworkID = (bytes[pos+0] << 8) + bytes[pos + 1]; // original network Id
            res.ServiceId = (bytes[pos+3] << 8) + bytes[pos + 4];
            pos += 3;

                // pointer + table id + sect.length + descriptors - crc
            var posAfterDescriptors = 4 + res.SectionLength - 4;

            // reading descriptors
            while (pos< posAfterDescriptors)
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

                res.ServiceDescriptors.Add(sDescriptor);

                pos = posAfterThisDescriptor;
            }

            return res;
        }
    }
}
