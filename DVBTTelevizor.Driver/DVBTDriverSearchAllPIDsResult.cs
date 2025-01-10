using System;
using System.Collections.Generic;
using System.Text;

namespace DVBTTelevizor
{
    public class DVBTDriverSearchAllPIDsResult : DVBTDriverSearchResult
    {
        public Dictionary<long,List<long>> PIDs = new Dictionary<long, List<long>>(); // Map PID -> PIDs
    }
}

