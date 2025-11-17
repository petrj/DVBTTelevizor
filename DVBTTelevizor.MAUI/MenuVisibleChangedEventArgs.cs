using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI
{
    public class MenuVisibleChangedEventArgs : EventArgs
    {
        public bool IsVisible { get; }

        public MenuVisibleChangedEventArgs(bool isVisible)
        {
            IsVisible = isVisible;
        }
    }
}
