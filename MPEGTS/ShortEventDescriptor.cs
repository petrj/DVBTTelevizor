using System;
using System.Collections.Generic;
using System.Text;

namespace MPEGTS
{
    public class ShortEventDescriptor : EventDescriptor
    {
        public byte Tag { get; set; }
        public byte Length { get; set; }

        public string LanguageCode { get; set; }
        public string EventName { get; set; }
        public string Text { get; set; }

        public static ShortEventDescriptor Parse(byte[] bytes)
        {
            var res = new ShortEventDescriptor();

            res.Tag = bytes[0];
            res.Length = bytes[1];

            res.LanguageCode = Encoding.GetEncoding("iso-8859-1").GetString(bytes, 2, 3);

            var eventNameLength = bytes[5];

            var pos = 6;

            res.EventName = MPEGTSCharReader.ReadString(bytes, pos, eventNameLength, true);

            pos = pos + eventNameLength;

            var textLength = bytes[pos];

            pos++;

            res.Text = MPEGTSCharReader.ReadString(bytes, pos, textLength, true);

            return res;
        }
    }
}
