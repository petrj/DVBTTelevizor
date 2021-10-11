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

            var posAfterTable = pos + SectionLength;

            TableIdExt = (bytes[pos + 0] << 8) + bytes[pos + 1];

            pos = pos + 2;

            Version = Convert.ToByte((bytes[pos + 0] & 64) >> 1);
            CurrentIndicator = (bytes[pos + 0] & 1) == 1;

            SectionNumber = bytes[pos + 1];
            LastSectionNumber = bytes[pos + 2];

            pos = pos + 3;

            while (pos< SectionLength)
            {
                var programNum = Convert.ToInt32(((bytes[pos+0]) << 8) + (bytes[pos + 1]));
                var programPID = Convert.ToInt32(((bytes[pos + 2] & 31) << 8) + (bytes[pos + 3]));

                ProgramAssociations.Add(new ProgramAssociation()
                {
                    ProgramNumber = programNum,
                    ProgramMapPID = programPID
                });

                pos +=4;
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
                sb.AppendLine($"CRC OK                : {CRCIsValid()}");

                if (SectionSyntaxIndicator)
                {
                    sb.AppendLine($"TableIdExt            : {TableIdExt}");
                    sb.AppendLine($"Version               : {Version}");
                    sb.AppendLine($"CurrentIndicator      : {CurrentIndicator}");
                    sb.AppendLine($"SectionNumber         : {SectionNumber}");
                    sb.AppendLine($"LastSectionNumber     : {LastSectionNumber}");
                }
            }

            sb.AppendLine();

            sb.AppendLine($"{"Program number",14} {" (Map) PID".PadRight(10, ' '),10}");
            sb.AppendLine($"{"--------------",14} {"---".PadRight(10, '-'),10}");
            foreach (var programAssociations in ProgramAssociations)
            {
                sb.AppendLine($"{programAssociations.ProgramNumber,14} {programAssociations.ProgramMapPID,10}");
            }

            return sb.ToString();
        }
    }
}
