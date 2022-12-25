using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace DVBTTelevizor
{
    public class KeyboardFocusableItem
    {
        public string Name { get; set; }
        public IList<View> Parts { get; set; }

        public KeyboardFocusableItem()
        {
            Parts = new List<View>();
        }

        public static KeyboardFocusableItem CreateFrom(string name, IList<View> parts)
        {
            var keyboardFocusableItem = new KeyboardFocusableItem();
            keyboardFocusableItem.Name = name;

            foreach (var part in parts)
            {
                keyboardFocusableItem.Parts.Add(part);
            }

            return keyboardFocusableItem;
        }

        public void Focus()
        {
            foreach (var part in Parts)
            {
                if (part is BoxView boxView)
                {
                    boxView.BackgroundColor = Color.FromHex("#303F9F");
                } else
                if (part is Button button)
                {
                    button.BackgroundColor = Color.FromHex("#303F9F");
                    button.TextColor = Color.White;
                } else
                {

                }
            }
        }

        public void DeFocus()
        {
            foreach (var part in Parts)
            {
                if (part is BoxView boxView)
                {
                    boxView.BackgroundColor = Color.Transparent;
                }
                else
                if (part is Button button)
                {
                    button.BackgroundColor = Color.Gray;
                    button.TextColor = Color.Black;
                }
                else
                {

                }
            }
        }
    }
}
