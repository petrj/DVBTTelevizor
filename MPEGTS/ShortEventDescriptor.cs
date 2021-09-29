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
            var enc = new ISO6937Encoding();

            var res = new ShortEventDescriptor();

            res.Tag = bytes[0];
            res.Length = bytes[1];

            res.LanguageCode = enc.GetString(bytes, 2, 3);

            var eventNameLength = bytes[5];

            var pos = 6;

            res.EventName = enc.GetString(bytes, pos, eventNameLength);

            pos = pos + eventNameLength;

            var textLength = bytes[pos];

            pos++;

            res.Text = enc.GetString(bytes, pos, textLength);

            return res;
        }
    }
}
