using Microsoft.Extensions.Primitives;
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
            if (!_dict.ContainsKey(value))
            {
                _dict.Add(value, string.Empty);
            }

            //Debug.WriteLine(value);

            var translatedString = value;

            if (_dict.ContainsKey(value) && !String.IsNullOrWhiteSpace(_dict[value]))
            {
                translatedString = _dict[value];
            }

            return String.Format(translatedString, arguments);
        }

        public static string Translated(this string value, params string[] arguments)
        {
            return Translate(value, arguments);
        }

        public static void LoadLanguage(string fileName = null)
        {
            if (!File.Exists(fileName))
            {
                return;
            }

            _dict.Clear();

            var text = File.ReadAllText(fileName);
            var items = text.Split(Environment.NewLine);
            foreach (var item in items)
            {
                var keyAndValue = item.Split('=', 2);
                if (keyAndValue.Length == 2 && !String.IsNullOrEmpty(keyAndValue[0]))
                {
                    _dict.Add(keyAndValue[0], keyAndValue[1]);
                }
            }
        }

        public static void SaveToFile(string fileName)
        {
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            var sb = new StringBuilder();
            foreach (var key in _dict.Keys)
            {
                sb.Append(key.Replace("=","~"));
                sb.Append("=");
                sb.Append(_dict[key]);
                sb.Append(Environment.NewLine);
            }

            File.WriteAllText(fileName, sb.ToString());
        }
    }
}
