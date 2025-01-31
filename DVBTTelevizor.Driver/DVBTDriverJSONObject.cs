using System;
using System.IO;
using Newtonsoft.Json;

namespace DVBTTelevizor
{
    public class DVBTDriverJSONObject
    {
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}
