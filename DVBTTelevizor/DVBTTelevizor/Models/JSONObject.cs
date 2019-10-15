using System;
using System.IO;
using Newtonsoft.Json;

namespace DVBTTelevizor
{
    public class JSONObject
    {
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}
