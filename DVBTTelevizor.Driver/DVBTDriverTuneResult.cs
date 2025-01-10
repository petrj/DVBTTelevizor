using System;
using System.Collections.Generic;
using System.Text;

namespace DVBTTelevizor
{
    public class DVBTDriverTuneResult : DVBTDriverSearchResult
    {
        public DVBTDriverStatus SignalState { get; set; } = new DVBTDriverStatus();
    }
}
