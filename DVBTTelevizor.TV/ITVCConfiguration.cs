using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor
{
    public interface ITVCConfiguration
    {
        public string ConfigDirectory { get; set; }

        public ObservableCollection<Channel> Channels { get; set; }

        public AppFontSizeEnum AppFontSize { get; set; }
        public bool Fullscreen { get; set; }

        void Load();
        void Save();

        int ImportChannelsFromJSON(string fileName);


    }
}
