using System;
using System.Collections.Generic;
using System.Text;

namespace MPEGTS
{
    public class SubtitlingInfo
    {
        public string LanguageCode { get; set; }

        public byte SubtitlingType { get; set; }
        public int CompositionPageId { get; set; }
        public int AncillaryPageId { get; set; }
    }
}
