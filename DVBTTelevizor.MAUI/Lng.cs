using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI
{
    public static class Lng
    {
        public static string Translate(string value, params string[] arguments)
        {
            return String.Format(value, arguments);
        }

        public static string Translated(this string value, params string[] arguments)
        {
            return Translate(value, arguments);
        }
    }
}
