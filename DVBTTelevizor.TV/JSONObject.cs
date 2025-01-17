using System;
using System.IO;
using Newtonsoft.Json;

namespace DVBTTelevizor
{
    public class JSONObject : BaseNotifableObject
    {
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}
