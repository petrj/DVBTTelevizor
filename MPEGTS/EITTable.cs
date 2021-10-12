using System;
using System.Collections.Generic;
using System.Text;

namespace MPEGTS
{
    public class EITTable : DVBTTable
    {
        // ID:
        // 78     0x4E event_information_section - actual_transport_stream, present/following
        // 79     0x4F event_information_section - other_transport_stream, present/following
        // 80-95  0x50 to 0x5F event_information_section - actual_transport_stream, schedule
        // 96-111 0x60 to 0x6F event_information_section - other_transport_stream, schedule

        public int ServiceId { get; set; }
        public int TransportStreamID { get; set; }
        public int OriginalNetworkID { get; set; }

        public byte SegmentLastSectionNumber { get; set; }
        public byte LastTableID { get; set; }


        public List<EventItem> EventItems { get; set; } = new List<EventItem>();

        public override void Parse(List<byte> bytes)
        {
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

            SectionSyntaxIndicator = ((bytes[pos + 1] & 128) == 128);
            Private = ((bytes[pos + 1] & 64) == 64);
            Reserved = Convert.ToByte((bytes[pos + 1] & 48) >> 4);
            SectionLength = Convert.ToInt32(((bytes[pos + 1] & 15) << 8) + bytes[pos + 2]);

            Data = new byte[SectionLength];
            CRC = new byte[4];

            //if (!DVBTTable.CRCIsValid(1,1))
            //{
            //    return null;
            //}

            if (bytes.Count < SectionLength + 4)
            {
                throw new IndexOutOfRangeException();
            }

            bytes.CopyTo(0, Data, 0, SectionLength);
            bytes.CopyTo(SectionLength, CRC, 0, 4);

            pos = pos + 3;

            ServiceId = (bytes[pos + 0] << 8) + bytes[pos + 1];

            Version = Convert.ToByte((bytes[pos + 2] & 62) >> 1);
            CurrentIndicator = (bytes[pos + 2] & 1) == 1;
            SectionNumber = bytes[pos + 3];
            LastSectionNumber = bytes[pos + 4];

            pos = pos + 5;

            TransportStreamID = (bytes[pos + 0] << 8) + bytes[pos + 1];
            OriginalNetworkID = (bytes[pos + 2] << 8) + bytes[pos + 3];

            pos = pos + 4;

            SegmentLastSectionNumber = bytes[pos + 0];
            LastTableID = bytes[pos + 1];

            pos = pos + 2;

            // pointer + table id + sect.length + descriptors - crc
            var posAfterDescriptors = 4 + SectionLength - 4;

            // reading descriptors
            while (pos < posAfterDescriptors)
            {
                var eventId = (bytes[pos + 0] << 8) + bytes[pos + 1];

                pos = pos + 2;

                var startTime = ParseTime(bytes, pos);

                pos = pos + 5;

                var duration = ParseDuration(bytes, pos);

                var finishTime = startTime.AddSeconds(duration);

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
                    var shortDescriptor = ShortEventDescriptor.Parse(descriptorData);
                    var eventItem = EventItem.Create(eventId, ServiceId, startTime, finishTime, shortDescriptor);
                    EventItems.Add(eventItem);
                } else
                {
                    // TODO: read other descriptors
                }

                pos = pos + descriptorLength;
            }

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

            foreach (var desc in EventItems)
            {
                if (desc is EventItem ev)
                {
                    sb.AppendLine(ev.WriteToString());
                }
            }

            return sb.ToString();
        }
    }
}
