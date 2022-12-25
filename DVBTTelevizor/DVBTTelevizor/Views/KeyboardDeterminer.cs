using System;
using System.Collections.Generic;
using System.Text;

namespace DVBTTelevizor
{
    public static class KeyboardDeterminer
    {
        public static bool Down(string key)
        {
            switch (key.ToLower())
            {
                case "dpaddown":
                case "buttonr1":
                case "down":
                case "s":
                case "numpad2":
                    return true;
                default:
                    return false;
            }
        }
    }
}
