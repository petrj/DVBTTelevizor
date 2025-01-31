using System;
using System.Collections.Generic;
using System.Text;

namespace DVBTTelevizor
{
    public class DVBTDriverStatusChangedEventArgs : EventArgs
    {
        public DVBTDriverStatus Status { get; set; }
    }
}
