using DVBTTelevizor.MAUI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI
{
    public interface ITVCConfiguration
    {
        public ObservableCollection<Channel> Channels { get; set; }
    }
}
