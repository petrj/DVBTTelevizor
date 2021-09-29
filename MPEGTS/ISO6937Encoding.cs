using System;
using System.Collections.Generic;
using System.Text;

namespace MPEGTS
{
    // https://stackoverflow.com/questions/43596227/iso6937-to-utf8-give-wrong-results-in-c-sharp

    /// <summary>
    /// ISO/IEC 6937 Encoding
    /// </summary>
    public class ISO6937Encoding : Encoding
    {
        private Encoding enc = Encoding.GetEncoding(20269);

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            int cnt = enc.GetChars(bytes, byteIndex, byteCount, chars, charIndex);
            for (int ix = 0; ix < byteCount; ix++, charIndex++)
            {
                int bx = byteIndex + ix;
                if (bytes[bx] >= 0xc1 && bytes[bx] <= 0xcf)
                {
                    if (charIndex == chars.Length - 1) chars[charIndex] = '?';
                    else
                    {
                        const string subst = "\u0300\u0301\u0302\u0303\u0304\u0306\u0307\u0308?\u030a\u0337?\u030b\u0328\u030c";
                        chars[charIndex] = chars[charIndex + 1];
                        chars[charIndex + 1] = subst[bytes[bx] - 0xc1];
                        ++ix;
                        ++charIndex;
                    }
                }
            }
            return cnt;
        }
        // Rest is boilerplate
        public override int GetByteCount(char[] chars, int index, int count)
        {
            return enc.GetByteCount(chars, index, count);
        }
        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            return enc.GetBytes(chars, charIndex, charCount, bytes, byteIndex);
        }
        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            return enc.GetCharCount(bytes, index, count);
        }
        public override int GetMaxByteCount(int charCount)
        {
            return enc.GetMaxByteCount(charCount);
        }
        public override int GetMaxCharCount(int byteCount)
        {
            return enc.GetMaxCharCount(byteCount);
        }
    }
}
