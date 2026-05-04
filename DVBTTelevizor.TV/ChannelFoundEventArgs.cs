using System;
using System.Collections.Generic;
using System.Text;

namespace DVBTTelevizor.MAUI
{
    public class RawDataReceivedEventArgs : EventArgs
    {
        public byte[] Data { get; set; }
        public int DataSize { get; set; }
    }
}
