using System;
using System.Collections.Generic;

namespace MPEGTS
{
    public class PMTTable : DVBTTable
    {
        public PMTTable()
        {
        }

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

            Console.WriteLine($"pos 0 byte: {bytes[pos + 0]}");
            Console.WriteLine($"pos 1 byte: {bytes[pos + 1]}");

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

        }
    }
}
