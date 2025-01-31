using DVBTTelevizor.MAUI;
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

        public string AutoPlayedChannelFrequencyAndMapPID { get; set; }

        public DVBTDriverTypeEnum DVBTDriverType { get; set; }
        public AppFontSizeEnum AppFontSize { get; set; }

        public bool Fullscreen { get; set; }
        public bool PlayOnBackground { get; set; }

        public bool ShowTVChannels { get; set; }
        public bool ShowNonFreeChannels { get; set; }
        public bool ShowRadioChannels { get; set; }
        public bool ShowOtherChannels { get; set; }

        public bool AllowRemoteAccessService { get; set; }
        public string RemoteAccessServiceIP { get; set; }
        public int RemoteAccessServicePort { get; set; }
        public string RemoteAccessServiceSecurityKey { get; set; }

        public bool EnableLogging { get; set; }

        void Load();
        void Save();

        int ImportChannelsFromJSON(string fileName);


    }
}
