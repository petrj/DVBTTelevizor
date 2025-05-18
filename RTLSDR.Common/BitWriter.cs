using System;
using System.Collections.Generic;
using System.Text;

namespace RTLSDR.Common
{
    public class StringBitWriter
    {
        private StringBuilder _buffer = null;

        public StringBitWriter()
        {
            Clear();
        }

        public void Clear()
        {
            _buffer = new StringBuilder();
        }

        public void AddBits(int value, int numberOfBits)
        {
            _buffer.Append(Convert.ToString(value, 2).PadLeft(numberOfBits, '0'));
        }

        public List<byte> GetBytes()
        {
            var res = new List<byte>();
            var allAsString = _buffer.ToString();

            while (allAsString.Length >= 8)
            {
                var b = allAsString.Substring(0, 8);
                allAsString = allAsString.Remove(0, 8);
                res.Add(Convert.ToByte(b, 2));
            }

            return res;
        }
    }
}
