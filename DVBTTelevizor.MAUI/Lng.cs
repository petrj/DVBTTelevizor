using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI
{
    public static class Lng
    {
        private static Dictionary<string,string> _dict = new Dictionary<string,string>();

        public static string Translate(string value, params string[] arguments)
        {
            if (_dict.ContainsKey(value))
            {
                _dict.Add(value, string.Empty);
            }

            //Debug.WriteLine(value);

            return String.Format(value, arguments);
        }

        public static string Translated(this string value, params string[] arguments)
        {
            return Translate(value, arguments);
        }

    }
}
