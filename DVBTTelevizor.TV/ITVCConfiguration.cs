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
        public ObservableCollection<Channel> Channels { get; set; }

        void Load();
        void Save();

        int ImportChannelsFromJSON(string fileName);


    }
}
