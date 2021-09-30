using System;
using System.Collections.Generic;

namespace MPEGTS
{
    public class PMTTable : DVBTTable
    {
        public List<ElementaryStreamSpecificData> Streams { get; set; } = new List<ElementaryStreamSpecificData>();

        public static PMTTable Parse(List<byte> bytes)
        {
            if (bytes == null || bytes.Count < 5)
                return null;

            var res = new PMTTable();

            var pointerFiled = bytes[0];
            var pos = 1 + pointerFiled;

            if (bytes.Count < pos + 2)
                return null;

            res.ID = bytes[pos];

            // read next 2 bytes
            var tableHeader1 = bytes[pos + 1];
            var tableHeader2 = bytes[pos + 2];

            res.SectionSyntaxIndicator = ((tableHeader1 & 128) == 128);
            res.Private = ((tableHeader1 & 64) == 64);
            res.Reserved = Convert.ToByte((tableHeader1 & 48) >> 4);
            res.SectionLength = Convert.ToInt32(((tableHeader1 & 15) << 8) + tableHeader2);

            res.Data = new byte[res.SectionLength];
            res.CRC = new byte[4];
            bytes.CopyTo(0, res.Data, 0, res.SectionLength);
            bytes.CopyTo(res.SectionLength, res.CRC, 0, 4);

            pos = pos + 3;

            res.TableIDExt = (bytes[pos + 0] << 8) + bytes[pos + 1];

            pos = pos + 2;

            res.Version = Convert.ToByte((bytes[pos + 0] & 64) >> 1);
            res.CurrentIndicator = (bytes[pos + 0] & 1) == 1;

            res.SectionNumber = bytes[pos + 1];
            res.LastSectionNumber = bytes[pos + 2];

            pos = pos + 3;

            // reserved bits, PCR PID

            pos = pos + 2;

            var programInfoLength = Convert.ToInt32(((bytes[pos+0] & 3) << 8) + bytes[pos + 1]);

            pos = pos + 2;

            // skipping program info
            pos += programInfoLength;

            // 2 (pointer and Table Id) + 2 (Section Length) + length  - CRC - Program Info Length
            var posAfterElementaryStreamSpecificData = 4 + res.SectionLength - 4 - programInfoLength;

            while (pos < posAfterElementaryStreamSpecificData)
            {
                // reading elementary stream info data until end of section
                var stream = new ElementaryStreamSpecificData();
                stream.StreamType = bytes[pos + 0];
                stream.PID = Convert.ToInt32(((bytes[pos + 1] & 15) << 8) + bytes[pos + 2]);
                stream.ESInfoLength = Convert.ToInt32(((bytes[pos + 3] & 15) << 8) + bytes[pos + 4]);

                res.Streams.Add(stream);

                pos += 5;

                // Elementary stream descriptors folow
                // TODO: read stream descriptor from bytes[pos + 0] position

                //Console.WriteLine($"Reading Es Info from position {pos} (byte 0: {bytes[pos + 0]}, byte 1: {bytes[pos + 1]})");

                pos += stream.ESInfoLength;
            }

            return res;
        }

        public void WriteToConsole()
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
        }
    }
}
