using System;
using System.Collections.Generic;
using System.Text;

namespace DVBTTelevizor
{
    public class DVBTDriverSearchPIDsResult : DVBTDriverSearchResult
    {
        public List<long> PIDs = new List<long>();
    }
}
