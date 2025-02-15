using System;
using System.Collections.Generic;
using System.Text;

namespace DVBTTelevizor.MAUI
{
    public class ChannelFoundEventArgs : EventArgs
    {
        public Channel Channel { get; set; }
    }
}
