using System;
using System.Collections.Generic;
using System.Text;

namespace DVBTTelevizor
{
    public class MediaTrack
    {
        public int Key { get; set; }
        public string Value { get; set; }
        public bool Active { get; set; }

        public string ActiveFlag
        {
            get
            {
                return Active ? "*" : "";
            }
        }
    }
}
