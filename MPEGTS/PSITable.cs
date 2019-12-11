using System;
using System.Collections.Generic;
using System.Text;

namespace MPEGTS
{
    // https://en.wikipedia.org/wiki/Program-specific_information#PAT_(Program_association_specific_data)

    public class PSITable : DVBTTable
    {
        public int TableIdExt { get; set; }

        public List<ProgramAssociation> ProgramAssociations { get; set; } = new List<ProgramAssociation>();

        public static PSITable Parse(List<byte> bytes)
        {
            if (bytes == null || bytes.Count < 5)
                return null;

            var res = new PSITable();

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

            var posAfterTable = pos + res.SectionLength;

            res.TableIdExt = (bytes[pos + 0] << 8) + bytes[pos + 1];

            pos = pos + 2;

            res.Version = Convert.ToByte((bytes[pos + 0] & 64) >> 1);
            res.CurrentIndicator = (bytes[pos + 0] & 1) == 1;

            res.SectionNumber = bytes[pos + 1];
            res.LastSectionNumber = bytes[pos + 2];

            pos = pos + 3;

            while (pos< posAfterTable)
            {
                var programNum = Convert.ToInt32(((bytes[pos+0]) << 8) + (bytes[pos + 1]));
                var programPID = Convert.ToInt32(((bytes[pos + 2] & 31) << 8) + (bytes[pos + 3]));

                res.ProgramAssociations.Add(new ProgramAssociation()
                {
                    ProgramNumber = programNum,
                    ProgramMapPID = programPID
                });

                pos +=4;
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
            Console.WriteLine($"CRC OK                : {CRCIsValid()}");

            if (SectionSyntaxIndicator)
            {
                Console.WriteLine($"TableIdExt            : {TableIdExt}");
                Console.WriteLine($"Version               : {Version}");
                Console.WriteLine($"CurrentIndicator      : {CurrentIndicator}");
                Console.WriteLine($"SectionNumber         : {SectionNumber}");
                Console.WriteLine($"LastSectionNumber     : {LastSectionNumber}");
            }

            foreach (var programAssociations in ProgramAssociations)
            {
                Console.WriteLine($"Program:");
                Console.WriteLine($"   Number: {programAssociations.ProgramNumber}");
                Console.WriteLine($"   PID: {programAssociations.ProgramMapPID}");
                Console.WriteLine($"------------------------------------");
            }
        }
    }
}
