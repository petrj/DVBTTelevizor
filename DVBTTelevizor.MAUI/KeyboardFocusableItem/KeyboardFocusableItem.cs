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

        public bool IsVisible
        {
            get
            {
                if (Parts == null || Parts.Count == 0)
                {
                    return false;
                }

                return Parts[0].IsVisible;
            } set
            {
                if (Parts == null || Parts.Count == 0)
                {
                    return;
                }

                Parts[0].IsVisible = value;
            }
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
                    entry.Focused += delegate { WeakReferenceMessenger.Default.Send(new DispatchKeyEventEnabledMessage(true)); };
                    entry.Unfocused += delegate { WeakReferenceMessenger.Default.Send(new DispatchKeyEventEnabledMessage(false)); };
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

        public View GetFirstView()
        {
            foreach (var part in Parts)
            {
                if (part is View v)
                {
                    return v;
                }
            }

            return null;
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
                if (part is RadioButton radioButton)
                {
                    radioButton.BackgroundColor = Color.FromHex("#303F9F");
                    radioButton.TextColor = Colors.White;
                    radioButton.Focus();
                }
                else
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
                } else
                if (part is Label lbl)
                {
                    lbl.BackgroundColor = Color.FromHex("#303F9F");
                    lbl.Focus();
                }
                else
                if (part is ImgButton ibtn)
                {
                    ibtn.ButtonColor = Color.FromHex("#303F9F");
                    //button.TextColor = Colors.White;
                    ibtn.FocusItem(true); // Focus event did not fired when debugging in Windows
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
                if (part is RadioButton radioButton)
                {
                    radioButton.BackgroundColor = Colors.Gray;
                    radioButton.TextColor = Colors.Black;
                }
                else
                if (part is ImageButton ibutton)
                {
                    ibutton.BackgroundColor = Colors.Gray;
                } else
                if (part is ImgButton ibtn)
                {
                    ibtn.ButtonColor = Colors.Gray;
                    ibtn.FocusItem(false); // Focus event did not fired when debugging in Windows
                } else
                if (part is Label lbl)
                {
                    lbl.Background = Colors.Transparent;
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
