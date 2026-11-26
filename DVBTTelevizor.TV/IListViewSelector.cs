using System;
using System.Collections.Generic;
using System.Text;

namespace DVBTTelevizor.TV
{
    public interface IListViewSelector
    {
        public bool Selected { get; set; }
        public bool Focused { get; set; }
        public string UniqueIdentifier { get; }

        public void NotifyChanges();

        public string Number { get; } // for selecting by number
    }
}
