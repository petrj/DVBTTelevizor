using System;
using System.Collections.Generic;
using System.Text;

namespace RTLSDR
{
    public class OnDataReceivedEventArgs
    {
        public byte[] Data { get; set; }
        public int Size { get; set; }
    }
}
