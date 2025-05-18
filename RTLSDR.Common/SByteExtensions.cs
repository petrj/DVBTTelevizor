using System;
using System.Collections.Generic;
using System.Text;

namespace RTLSDR.Common
{
    public static class SByteExtensions
    {
        public static sbyte[] CloneArray(this sbyte[] array, int count = -1)
        {
            if (count == -1)
                count = array.Length;

            var arrayCopy = new sbyte[count];
            Buffer.BlockCopy(array, 0, arrayCopy, 0, count);
            return arrayCopy;
        }
    }
}
