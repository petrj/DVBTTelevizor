using MPEGTS;
using System;
using System.Collections.Generic;
using System.Text;

namespace DVBTTelevizor
{
    public class PlayStreamInfo
    {
        public Channel Channel { get; set; }
        public EPGCurrentEvent CurrentEvent { get; set; }
        public string RecordingStream { get; set; } = null;
        public int SignalStrengthPercentage { get; set; } = 0;
        public bool ShortInfoWithoutChannelName { get; set; } = false;
    }
}
