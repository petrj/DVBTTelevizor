using System;

namespace RTLSDR.Common
{
    public class DataDemodulatedEventArgs : EventArgs
    {
        public byte[] Data { get; set; }
        public AudioDataDescription AudioDescription { get; set; }
    }
}
