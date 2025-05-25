using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI
{
    public class MenuItem : BaseNotifableObject
    {
        public string Id { get; set; }

        public string Title { get; set; }
        public string ImgSource { get; set; }
        public bool Selected { get; set; } = false;

        public string BackgroundColor
        {
            get
            {
                return Selected ? "#007cd2" : "#444444";
            }
        }

        public void Update()
        {
            OnPropertyChanged(nameof(Id));
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(ImgSource));
            OnPropertyChanged(nameof(Selected));
            OnPropertyChanged(nameof(BackgroundColor));
        }
    }
}
