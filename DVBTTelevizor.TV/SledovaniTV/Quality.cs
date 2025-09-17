using DVBTTelevizor;
using System;
using System.Collections.Generic;
using System.Text;

namespace SledovaniTV
{
    public class Quality : JSONObject
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Allowed { get; set; }
    }
}
