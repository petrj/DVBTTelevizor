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
            // Error in Xamarine Live Player:  'JObject' does not contain a definition for 'ContainsKey' and no extension method 'ContainsKey' accepting a first argument of type 'JObject' could be found(are you missing a using directive or an assembly reference ?)		Z:\SledovaniTVPlayer\SledovaniTVApi\ParsableJObject.cs  1

            return obj.TryGetValue(key, StringComparison.CurrentCulture, out JToken value);
        }
    }
}
