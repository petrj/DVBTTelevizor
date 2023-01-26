using System;
using System.Collections.Generic;
using System.Text;

namespace MPEGTS
{
    public class SubtitlingDescriptor : EventDescriptor
    {
        public List<SubtitlingInfo> SubtitleInfo = new List<SubtitlingInfo>();

        public void Parse(byte[] bytes)
        {
            var pos = 0;

            var descriptorTag = bytes[pos + 0];
            var descriptorLength = bytes[pos + 1];

            if (descriptorTag != 89)
                return;
        }
    }
}
