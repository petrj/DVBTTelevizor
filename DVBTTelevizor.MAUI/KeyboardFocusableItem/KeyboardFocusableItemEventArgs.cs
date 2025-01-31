using System;
using System.Collections.Generic;
using System.Text;

namespace DVBTTelevizor.MAUI
{
    public class KeyboardFocusableItemEventArgs : EventArgs
    {
        public KeyboardFocusableItem FocusedItem { get; set; }

        public KeyboardFocusableItemEventArgs(KeyboardFocusableItem item)
        {
            FocusedItem = item;
        }
    }

    public delegate void KeyboardFocusableItemEventDelegate(KeyboardFocusableItemEventArgs _args);
}
