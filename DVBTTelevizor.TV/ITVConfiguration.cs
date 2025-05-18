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
    public interface ITVConfiguration
    {
        public string ConfigDirectory { get; set; }

        public ObservableCollection<Channel> GetChannels();
        public void SaveChannels(ObservableCollection<Channel> channels);

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

        public bool TuneDVBTEnabled { get; set; }
        public bool TuneDVBT2Enabled { get; set; }
        public bool TuneDVBTPreferred { get; set; }

        public long FrequencyFromKHz { get; set; }
        public long FrequencyToKHz { get; set; }
        public long FrequencyKHz { get; set; }

        public long DVBTBandwidthKHz { get; set; }

        public int SDRDriverStreamPort { get; set; }
        public int SDRDriverPort { get; set; }
        public int SDRSampleRate { get; set; }
    }
}
