using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace DVBTTelevizor
{
    public static class JObjectExtensionMethods
    {
        public static string GetStringValue(this JObject obj, string key)
        {
            return obj[key].ToString();
        }

        public static bool HasValue(this JObject obj, string key)
        {
            // ContainsKey method of JObject is not implemented in Xamarine Live Player!
            return obj.TryGetValue(key, StringComparison.CurrentCulture, out JToken value);
        }
    }
}
