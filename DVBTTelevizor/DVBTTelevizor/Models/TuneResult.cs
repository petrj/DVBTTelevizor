using System;
using System.Collections.Generic;
using System.Text;

namespace DVBTTelevizor
{
    public class TuneResult : SearchResult
    {
        public DVBTStatus SignalState { get; set; } = new DVBTStatus();
    }
}
