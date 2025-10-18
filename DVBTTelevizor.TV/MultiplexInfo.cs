using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.TV
{
    public class MultiplexInfo : JSONObject
    {
        public MultiplexInfo(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
        public bool IsEnabled { get; set; } = true;
        public bool Selected { get; set; } = false;

        public string BackgroundColor
        {
            get
            {
                return Selected
                    ?  "#007cd2"
                    : "Transparent";              }
        }

        public void NotifyChanges()
        {
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(IsEnabled));
            OnPropertyChanged(nameof(Selected));
            OnPropertyChanged(nameof(BackgroundColor));
        }
    }
}
