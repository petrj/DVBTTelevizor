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

        public DateTime StartTime { get; set; }
        public DateTime FinishTime { get; set; }
        public int Duration { get; set; }
        public int EventId { get; set; }

        public List<EventDescriptor> EventDescriptors { get; set; } = new List<EventDescriptor>();

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

            //if (!DVBTTable.CRCIsValid(1,1))
            //{
            //    return null;
            //}

            if (bytes.Count < res.SectionLength + 4)
            {
                throw new IndexOutOfRangeException();
            }

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

            // pointer + table id + sect.length + descriptors - crc
            var posAfterDescriptors = 4 + res.SectionLength - 4;

            // reading descriptors
            while (pos < posAfterDescriptors)
            {
               res.EventId = (bytes[pos + 0] << 8) + bytes[pos + 1];

                pos = pos + 2;

                res.StartTime = ParseTime(bytes, pos);

                pos = pos + 5;

                res.Duration = ParseDuration(bytes, pos);

                res.FinishTime = res.StartTime.AddSeconds(res.Duration);

                pos = pos + 3;

                var running_status = (bytes[pos + 0] & 224) >> 5;
                var freeCAMode = (bytes[pos + 0] & 16) >> 4;

                var descriptorLength = ((bytes[pos + 0] & 15) << 8) + bytes[pos + 1];

                pos = pos + 2;

                var descriptorData = new byte[descriptorLength];
                bytes.CopyTo(pos, descriptorData, 0, descriptorLength);

                var descriptorTag = descriptorData[0];
                if (descriptorTag == 77)
                {
                    res.EventDescriptors.Add(ShortEventDescriptor.Parse(descriptorData));
                } else
                {

                }

                pos = pos + descriptorLength;
            }

            return res;
        }


        public void WriteToConsole(bool detailed = true)
        {
            Console.WriteLine(WriteToString(detailed));
        }

        public string WriteToString(bool detailed = true)
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

                sb.AppendLine($"__________________");
            }

            sb.AppendLine($"NetworkID              : {NetworkID}");
            sb.AppendLine($"ServiceId              : {ServiceId}");
            sb.AppendLine($"EventId                : {EventId}");

            foreach (var desc in EventDescriptors)
            {
                if (desc is ShortEventDescriptor sed)
                {
                    sb.AppendLine($"{StartTime.ToString("HH:mm")}-{FinishTime.ToString("HH:mm")} {sed.EventName} ({sed.Text })");

                    //if (!detailed)
                      //  break;
                }
            }

            return sb.ToString();
        }
    }
}
