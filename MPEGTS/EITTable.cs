using System;
using System.Collections.Generic;
using System.Text;

namespace MPEGTS
{
    public class EITTable : DVBTTable
    {
        public int ServiceId { get; set; }
        public int TransportStreamID { get; set; }
        public int OriginalNetworkID { get; set; }

        public byte SegmentLastSectionNumber { get; set; }
        public byte LastTableID { get; set; }

        public static EITTable Parse(List<byte> bytes)
        {
            if (bytes == null || bytes.Count < 5)
                return null;

            var res = new EITTable();

            var pointerFiled = bytes[0];
            var pos = 1 + pointerFiled;

            if (bytes.Count < pos + 2)
                return null;

            res.ID = bytes[pos];

            res.SectionSyntaxIndicator = ((bytes[pos + 1] & 128) == 128);
            res.Private = ((bytes[pos + 1] & 64) == 64);
            res.Reserved = Convert.ToByte((bytes[pos + 1] & 48) >> 4);
            res.SectionLength = Convert.ToInt32(((bytes[pos + 1] & 15) << 8) + bytes[pos + 2]);

            res.Data = new byte[res.SectionLength];
            res.CRC = new byte[4];
            bytes.CopyTo(0, res.Data, 0, res.SectionLength);
            bytes.CopyTo(res.SectionLength, res.CRC, 0, 4);

            pos = pos + 3;

            res.ServiceId = (bytes[pos + 0] << 8) + bytes[pos + 1];

            res.Version = Convert.ToByte((bytes[pos + 2] & 62) >> 1);
            res.CurrentIndicator = (bytes[pos + 2] & 1) == 1;
            res.SectionNumber = bytes[pos + 3];
            res.LastSectionNumber = bytes[pos + 4];

            pos = pos + 5;

            res.TransportStreamID = (bytes[pos + 0] << 8) + bytes[pos + 1];
            res.OriginalNetworkID = (bytes[pos + 2] << 8) + bytes[pos + 3];

            pos = pos + 4;

            res.SegmentLastSectionNumber = bytes[pos + 0];
            res.LastTableID = bytes[pos + 1];

            pos = pos + 2;

            // TODO - Loop event_ids

            return res;
        }
    }
}
