using System;
using System.Collections.Generic;
using System.Text;

namespace DVBTTelevizor
{
    public class SearchAllPIDsResult : SearchResult
    {
        public Dictionary<long,List<long>> PIDs = new Dictionary<long, List<long>>(); // Map PID -> PIDs
    }
}

