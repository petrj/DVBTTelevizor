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
        Back = 32,
        Special = 64
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

            if (Back(key))
                return KeyboardNavigationActionEnum.Back;

            if (OK(key))
                return KeyboardNavigationActionEnum.OK;

            if (Special(key))
                return KeyboardNavigationActionEnum.Special;

            return KeyboardNavigationActionEnum.Unknown;
        }

        public static bool Special(string key)
        {
            switch (key.ToLower())
            {
                case "0":
                case "num0":
                case "number0":
                case "numpad0":
                case "1":
                case "num1":
                case "number1":
                case "2":
                case "num2":
                case "number2":
                case "3":
                case "num3":
                case "number3":
                case "4":
                case "num4":
                case "number4":
                case "5":
                case "num5":
                case "number5":
                case "6":
                case "num6":
                case "number6":
                case "7":
                case "num7":
                case "number7":
                case "8":
                case "num8":
                case "number8":
                case "9":
                case "num9":
                case "number9":
                case "f5":
                case "ctrlleft":
                case "buttonl2":
                case "info":
                case "guide":
                case "i":
                case "g":
                case "numpadadd":
                case "buttonthumbl":
                case "tab":
                case "f1":
                case "f6":
                case "f7":
                case "f8":
                case "focus":
                case "camera":
                case "menu":
                case "pagedown":
                case "pageup":
                case "end":
                case "moveend":
                case "home":
                case "movehome":
                case "mediafastforward":
                case "mediaplaypause":
                case "mediaforward":
                case "mediarewind":
                case "mediafastrewind":
                case "mediastop":
                case "blue":
                case "progblue":
                case "yellow":
                case "progyellow":
                case "green":
                case "proggreen":
                case "red":
                case "progred":
                case "F9":
                case "F10":
                case "F11":
                case "F12":
                    return true;
                default:
                    return false;
            }
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

        public static bool Back(string key)
        {
            switch (key.ToLower())
            {
                case "f4":
                case "escape":
                case "mediaclose":
                case "mediaplaystop":
                case "numpadsubtract":
                case "del":
                case "buttonx":
                case "back":
                case "esc":
                case "buttonb":
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
