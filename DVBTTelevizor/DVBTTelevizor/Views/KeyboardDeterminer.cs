using System;
using System.Collections.Generic;
using System.Text;

namespace DVBTTelevizor
{
    public static class KeyboardDeterminer
    {
        public static string GetKeyAction(string key)
        {
            if (Down(key))
                return "down";

            if (Up(key))
                return "up";

            if (OK(key))
                return "OK";

            return null;
        }

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

        public static bool Up(string key)
        {
            switch (key.ToLower())
            {
                case "dpadup":
                case "buttonl1":
                case "up":
                case "w":
                case "numpad8":
                    return true;
                default:
                    return false;
            }
        }

        public static bool OK(string key)
        {
            switch (key.ToLower())
            {
                case "dpadcenter":
                case "space":
                case "buttonr2":
                case "mediaplay":
                case "enter":
                case "numpad5":
                case "numpadenter":
                case "buttona":
                case "buttonstart":
                case "capslock":
                case "comma":
                case "semicolon":
                case "grave":
                    return true;
                default:
                    return false;
            }
        }
    }
}
