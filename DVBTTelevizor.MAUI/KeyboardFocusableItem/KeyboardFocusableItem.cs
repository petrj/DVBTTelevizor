using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace DVBTTelevizor.MAUI
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
                    entry.Focused += delegate { WeakReferenceMessenger.Default.Send(new DispatchKeyEventEnabledMessage(false)); };
                    entry.Unfocused += delegate { WeakReferenceMessenger.Default.Send(new DispatchKeyEventEnabledMessage(true)); };
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

        public double Height
        {
            get
            {
                double h = 0;
                foreach (var part in Parts)
                {
                    if (part.Height > h)
                        h = part.Height;
                }

                return h;
            }
        }

        public double ReComputeMaxYPosition()
        {
            double res = 0;
            foreach (var part in Parts)
            {
                var y = part.Y; var parent = part.Parent as VisualElement; while (parent != null) { y += parent.Y; parent = parent.Parent as VisualElement; }

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
                    button.TextColor = Colors.White;
                    button.Focus();
                } else
                if (part is ImageButton ibutton)
                {
                    ibutton.BackgroundColor = Color.FromHex("#303F9F");
                    //button.TextColor = Colors.White;
                    ibutton.Focus();
                }
                else
                if (part is Picker picker)
                {
                    //picker.BackgroundColor = Color.FromHex("#303F9F");
                }
                else
                if (part is Entry entry)
                {
                    //entry.BackgroundColor = Color.FromHex("#303F9F");
                }
                else
                if (part is Switch sw)
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
                    boxView.BackgroundColor = Colors.Transparent;
                }
                else
                if (part is Button button)
                {
                    button.BackgroundColor = Colors.Gray;
                    button.TextColor = Colors.Black;
                } else
                if (part is ImageButton ibutton)
                {
                    ibutton.BackgroundColor = Colors.Gray;
                    //button.TextColor = Colors.White;
                }
                else
                if (part is Picker picker)
                {
                    //picker.BackgroundColor = Color.FromHex("#222222");
                }
                else
                if (part is Entry entry)
                {
                    //entry.BackgroundColor = Color.FromHex("#222222");
                }
                else
                {

                }
            }
        }
    }
}
