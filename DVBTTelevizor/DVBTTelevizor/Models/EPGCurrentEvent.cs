using MPEGTS;
using System;
using System.Collections.Generic;
using System.Text;

namespace DVBTTelevizor.Models
{
    public class EPGCurrentEvent
    {
        public EventItem CurrentEventItem { get; set; }
        public EventItem NextEventItem { get; set; }
    }
}
