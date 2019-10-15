using MPEGTS;
using System;
using System.Collections.Generic;
using System.Text;

namespace DVBTTelevizor
{
    public class SearchMapPIDsResult : SearchResult
    {
        /// <summary>
        /// Service descriptor => Map PID
        /// </summary>
        public Dictionary<ServiceDescriptor, long> ServiceDescriptors {get; set;}
    }
}
