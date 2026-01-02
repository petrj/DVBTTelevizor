using RTLSDR.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor
{
    public delegate void DemodulatedEventHandler(object sender, DemodulatedEventArgs e);

    public class DemodulatedEventArgs : EventArgs
    {
        public byte[] DemodulatedData { get; }
        public AudioDataDescription Description { get; set; }

        public DemodulatedEventArgs(byte[] data)
        {
            DemodulatedData = data;
        }
    }
}
