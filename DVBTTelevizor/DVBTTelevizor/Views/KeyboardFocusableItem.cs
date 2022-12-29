using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace DVBTTelevizor
{
    public class KeyboardFocusableItem
    {
        public string Name { get; set; }

        private IList<View> Parts { get; set; }
        private double _maxYPos = -1;

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

                if (part is Entry entry)
                {
                    entry.Focused += delegate { MessagingCenter.Send(String.Empty, BaseViewModel.MSG_EnableDispatchKeyEvent); };
                    entry.Unfocused += delegate { MessagingCenter.Send(String.Empty, BaseViewModel.MSG_DisableDispatchKeyEvent); };
                }
            }

            return keyboardFocusableItem;
        }

        public double MaxYPosition
        {
            get
            {
                if (_maxYPos == -1)
                {
                    ReComputeMaxYPosition();
                }

                return _maxYPos;
            }
        }

        public double ReComputeMaxYPosition()
        {
            double res = 0;
            foreach (var part in Parts)
            {
                var y = part.Y; var parent = part.ParentView; while (parent != null) { y += parent.Y; parent = parent.ParentView; }

                if (y > res)
                    res = y;
            }

            _maxYPos = res;
            return _maxYPos;
        }

        public void Focus()
        {
            foreach (var part in Parts)
            {
                if (part is BoxView boxView)
                {
                    boxView.BackgroundColor = Color.FromHex("#303F9F");
                    boxView.Focus();
                } else
                if (part is Button button)
                {
                    button.BackgroundColor = Color.FromHex("#303F9F");
                    button.TextColor = Color.White;
                    button.Focus();
                } else
                if (part is Picker picker)
                {
                    picker.BackgroundColor = Color.FromHex("#303F9F");
                }
                else
                if (part is Entry entry)
                {
                    entry.BackgroundColor = Color.FromHex("#303F9F");
                }
                else if (part is Switch sw)
                {
                    sw.Focus();
                }
                else
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
                if (part is Picker picker)
                {
                    picker.BackgroundColor = Color.Transparent;
                }
                else
                if (part is Entry entry)
                {
                    entry.BackgroundColor = Color.Transparent;
                }
                else
                {

                }
            }
        }
    }
}
