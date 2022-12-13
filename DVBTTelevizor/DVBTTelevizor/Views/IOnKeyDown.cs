using System;
using System.Collections.Generic;
using System.Text;

namespace DVBTTelevizor
{
    public interface IOnKeyDown
    {
        void OnKeyDown(string key, bool longPress);
    }
}
