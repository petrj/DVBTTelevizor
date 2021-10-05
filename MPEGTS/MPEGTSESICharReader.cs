using System;
using System.Collections.Generic;
using System.Text;

namespace MPEGTS
{
    public class MPEGTSESICharReader
    {
        /// <summary>
        ///  https://en.wikipedia.org/wiki/T.51/ISO/IEC_6937
        /// </summary>
        /// <value>The</value>
        public static Dictionary<byte, Tuple<string, string>> ISO6937Table { get; set; } =
            new Dictionary<byte, Tuple<string, string>>()
            {
                { 0xC1, new Tuple<string, string>("AEIOUaeiou", "ÀÈÌÒÙàèìòù") },
                { 0xC2, new Tuple<string, string>("ACEILNORSUYZacegilnorsuyz", "ÁĆÉÍĹŃÓŔŚÚÝŹáćéģíĺńóŕśúýź")},
                { 0xC3, new Tuple<string, string>("ACEGHIJOSUWYaceghijosuwy", "ÂĈÊĜĤÎĴÔŜÛŴŶâĉêĝĥîĵôŝûŵŷ")},
                { 0xC4, new Tuple<string, string>("AINOUainou", "ÃĨÑÕŨãĩñõũ")},
                { 0xC5, new Tuple<string, string>("AEIOUaeiou", "ĀĒĪŌŪāēīōū")},
                { 0xC6, new Tuple<string, string>("AGUagu", "ĂĞŬăğŭ")},
                { 0xC7, new Tuple<string, string>("CEGIZcegz", "ĊĖĠİŻċėġż")},
                { 0xC8, new Tuple<string, string>("AEIOUYaeiouy", "ÄËÏÖÜŸäëïöüÿ")},
                { 0xCA, new Tuple<string, string>("AUau", "ÅŮåů")},
                { 0xCB, new Tuple<string, string>("CGKLNRSTcklnrst", "ÇĢĶĻŅŖŞŢçķļņŗşţ")},
                { 0xCD, new Tuple<string, string>("OUou", "ŐŰőű")},
                { 0xCE, new Tuple<string, string>("AEIUaeiu", "ĄĘĮŲąęįų")},
                { 0xCF, new Tuple<string, string>("CDELNRSTZcdelnrstz", "ČĎĚĽŇŘŠŤŽčďěľňřšťž")},
            };

        private static string ReadControlCode(byte b)
        {
            if ((b >= 0x80) && (b <= 0x85))
            {
                // reserved for future use
                return String.Empty;
            }

            if (b == 0x86)
            {
                // character emphasis on
                return String.Empty;
            }

            if (b == 0x87)
            {
                // character emphasis off
                return String.Empty;
            }

            if ((b >= 0x88) && (b <= 0x89))
            {
                // reserved for future use
                return String.Empty;
            }

            if (b == 0x8A)
            {
                // CRLF
                return Environment.NewLine;
            }

            if ((b >= 0x8B) && (b <= 0x9F))
            {
                // user defined
                return String.Empty;
            }

            return null; // not control code
        }

        public static string ReadString(byte[] bytes, int index, int count)
        {
            if (bytes == null ||
                bytes.Length == 0 ||
                count == 0 ||
                index+count > bytes.Length)
            {
                return String.Empty;
            }

            if (bytes[index]<0x20)
            {
                // not default encoding

                if (bytes[index] == 0x14)
                {
                    // Big5 subset of ISO/IEC 10646 [16] Traditional Chinese
                }

                return String.Empty;
            }

            // all subsequent bytes in the text item are coded using the default character coding table (Latin alphabet)

            var res = new StringBuilder();

            byte accent = 0;

            for (var i=index; i<index+count;i++)
            {
                var b = bytes[i];

                var controlCode = ReadControlCode(b);

                if (controlCode != null)
                {
                    // control code found:
                    res.Append(controlCode);
                    accent = 0;
                    continue;
                }

                if (ISO6937Table.ContainsKey(b))
                {
                    // accent
                    accent = b;
                }
                else
                {
                    if (b >= 0x20 && b <= 0x7F)
                    {
                        if (accent == 0)
                        {
                            res.Append(Encoding.ASCII.GetString(new byte[] { b }));
                        }
                        else
                        {
                            var notAccentedChar = Encoding.ASCII.GetString(new byte[] { b });

                            var accentedChar = notAccentedChar;

                            if (!String.IsNullOrEmpty(ISO6937Table[accent].Item1))
                            {
                                var pos = ISO6937Table[accent].Item1.IndexOf(notAccentedChar);

                                if (pos >= 0)
                                {
                                    accentedChar = ISO6937Table[accent].Item2.Substring(pos, 1);
                                }
                            }

                            res.Append(accentedChar);

                        }
                    }
                    accent = 0;
                }
            }

            return res.ToString();
        }
    }
}
