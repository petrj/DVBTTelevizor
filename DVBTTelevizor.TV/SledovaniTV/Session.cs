using DVBTTelevizor;
using System;


namespace SledovaniTV
{
    public class Session : JSONObject
    {
        public Session()
        { }

        public string? PHPSESSID { get; set; }
    }
}
