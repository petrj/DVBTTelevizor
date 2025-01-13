using System;
using System.IO;
using Newtonsoft.Json;

namespace DVBTTelevizor.MAUI
{
    public class JSONObject : BaseNotifableObject
    {
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}
