using System;
using System.Collections.Generic;
using System.Text;

namespace DVBTTelevizor
{
    [Flags]
    public enum KeyboardNavigationActionEnum
    {
        Unknown = 0,
        OK = 1,
        Up = 2,
        Right = 4,
        Down = 8,
        Left = 16,
        Back = 32
    }

    public static class KeyboardDeterminer
    {
        public static KeyboardNavigationActionEnum GetKeyAction(string key)
        {
            if (Down(key))
                return KeyboardNavigationActionEnum.Down;

            if (Up(key))
                return KeyboardNavigationActionEnum.Up;

            if (Right(key))
                return KeyboardNavigationActionEnum.Right;

            if (Left(key))
                return KeyboardNavigationActionEnum.Left;

            if (OK(key))
                return KeyboardNavigationActionEnum.OK;

            return KeyboardNavigationActionEnum.Unknown;
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

        public static bool Right(string key)
        {
            switch (key.ToLower())
            {
                case "pagedown":
                case "dpadright":
                case "right":
                case "d":
                case "f":
                case "f3":
                case "mediaplaynext":
                case "medianext":
                case "numpad6":
                    return true;
                default:
                    return false;
            }
        }

        public static bool Left(string key)
        {
            switch (key.ToLower())
            {
                case "dpadleft":
                case "pageup":
                case "left":
                case "a":
                case "b":
                case "f2":
                case "mediaplayprevious":
                case "mediaprevious":
                case "numpad4":
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
