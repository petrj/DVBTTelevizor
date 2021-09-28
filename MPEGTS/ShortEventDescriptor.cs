using System;
using System.Collections.Generic;
using System.Text;

namespace MPEGTS
{
    public class ShortEventDescriptor
    {
        public byte Tag { get; set; }
        public byte Length { get; set; }

        public string LanguageCode { get; set; }
        public string EventName { get; set; }
        public string Text { get; set; }

        public static ShortEventDescriptor Parse(IList<byte> bytes)
        {          

            var res = new ShortEventDescriptor();

            res.Tag = bytes[0];
            res.Length = bytes[1];

            for (var i=2;i<=4;i++)
            {
                res.LanguageCode += Convert.ToChar(bytes[i]);
            }                        

            var eventNameLength = bytes[5];

            var pos = 6;

            for (var i = pos; i < pos + eventNameLength; i++)
            {
                res.EventName += Convert.ToChar(bytes[i]);
            }

            // // ISO/IEC 6937
            // var eventNameUTF8 = System.Text.Encoding.GetEncoding("20269").GetString(eventNameBytes, 0, eventNameLength);

            pos = pos + eventNameLength;

            var textLength = bytes[pos];

            pos++;

            for (var i = pos; i <= pos + textLength; i++)
            {
                res.Text += Convert.ToChar(bytes[i]);
            }


            return res;
        }
    }
}
