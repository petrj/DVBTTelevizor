using System;
using System.Collections.Generic;
using System.Text;

namespace MPEGTS
{
    // https://en.wikipedia.org/wiki/Program-specific_information#PAT_(Program_association_specific_data

    public class PMTTable : DVBTTable
    {
        public List<ElementaryStreamSpecificData> Streams { get; set; } = new List<ElementaryStreamSpecificData>();

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

            TableIDExt = (bytes[pos + 0] << 8) + bytes[pos + 1];

            pos = pos + 2;

            Version = Convert.ToByte((bytes[pos + 0] & 64) >> 1);
            CurrentIndicator = (bytes[pos + 0] & 1) == 1;

            SectionNumber = bytes[pos + 1];
            LastSectionNumber = bytes[pos + 2];

            pos = pos + 3;

            // reserved bits, PCR PID

            pos = pos + 2;

            var programInfoLength = Convert.ToInt32(((bytes[pos+0] & 3) << 8) + bytes[pos + 1]);

            pos = pos + 2;

            // skipping program info
            pos += programInfoLength;

            // 2 (pointer and Table Id) + 2 (Section Length) + length  - CRC - Program Info Length
            var posAfterElementaryStreamSpecificData = 4 + SectionLength - 4 - programInfoLength;

            while (pos < posAfterElementaryStreamSpecificData)
            {
                // reading elementary stream info data until end of section
                var stream = new ElementaryStreamSpecificData();
                stream.StreamType = bytes[pos + 0];
                stream.PID = Convert.ToInt32(((bytes[pos + 1] & 31) << 8) + bytes[pos + 2]);
                stream.ESInfoLength = Convert.ToInt32(((bytes[pos + 3] & 15) << 8) + bytes[pos + 4]);

                Streams.Add(stream);

                pos += 5;

                // Elementary stream descriptors folow
                // TODO: read stream descriptor from bytes[pos + 0] position

                pos += stream.ESInfoLength;
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
                Console.WriteLine($"ID                    : {ID}");
                Console.WriteLine($"SectionSyntaxIndicator: {SectionSyntaxIndicator}");
                Console.WriteLine($"Private               : {Private}");
                Console.WriteLine($"Reserved              : {Reserved}");
                Console.WriteLine($"SectionLength         : {SectionLength}");

                if (SectionSyntaxIndicator)
                {
                    Console.WriteLine($"Version                : {Version}");
                    Console.WriteLine($"CurrentIndicator       : {CurrentIndicator}");
                    Console.WriteLine($"SectionNumber          : {SectionNumber}");
                    Console.WriteLine($"LastSectionNumber      : {LastSectionNumber}");
                }

                Console.WriteLine($"---- Stream:-----------------------");
                foreach (var stream in Streams)
                {
                    Console.WriteLine($"PID                    : {stream.PID}");
                    Console.WriteLine($"StreamType (byte)      : {stream.StreamType}");
                    Console.WriteLine($"StreamType (enum)      : {stream.StreamTypeDesc}");
                    Console.WriteLine($"-----------------------------------");
                }
            } else
            {
                foreach (var stream in Streams)
                {
                    Console.WriteLine($"  {stream.StreamTypeDesc.ToString().PadRight(38, ' '),38} {"".PadRight(14,' '),14} {stream.PID,8}");
                }
            }

            return sb.ToString();
        }
    }
}
