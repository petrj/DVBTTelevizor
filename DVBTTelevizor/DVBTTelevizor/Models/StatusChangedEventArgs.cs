using System;
using System.Collections.Generic;
using System.Text;

namespace DVBTTelevizor
{
    public class StatusChangedEventArgs : EventArgs
    {
        public DVBTStatus Status { get; set; }
    }
}
