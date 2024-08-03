using Android.Content;
using Android.Preferences;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using Xamarin.Forms;

namespace DVBTTelevizor
{
    public class DVBTTelevizorConfiguration : CustomSharedPreferencesObject
    {
        public ObservableCollection<DVBTChannel> Channels
        {
            get
            {
                var val = GetPersistingSettingValue<string>("ChannelsJson");
                if (!string.IsNullOrEmpty(val))
                {
                    return JsonConvert.DeserializeObject<ObservableCollection<DVBTChannel>>(val);
                }
                return null;
            }
            set
            {
                SavePersistingSettingValue<string>("ChannelsJson", JsonConvert.SerializeObject(value));
            }
        }

        public bool AutoInitAfterStart
        {
            get
            {
               return !DoNotAutoInitAfterStart;
            }
            set
            {
                DoNotAutoInitAfterStart = value;
            }
        }


        public bool DoNotAutoInitAfterStart
        {
            get
            {
                return GetPersistingSettingValue<bool>("DoNotAutoInitAfterStart");
            }
            set
            {
                SavePersistingSettingValue<bool>("DoNotAutoInitAfterStart", value);
            }
        }


        public bool ShowTVChannels
        {
            get
            {
                return !HideTVChannels;
            }
            set
            {
                HideTVChannels = !value;
            }
        }

        public bool HideTVChannels
        {
            get
            {
                return GetPersistingSettingValue<bool>("HideTVChannels", true);
            }
            set
            {
                SavePersistingSettingValue<bool>("HideTVChannels", value);
            }
        }

        public bool ShowNonFreeChannels
        {
            get
            {
                return GetPersistingSettingValue<bool>("ShowNonFreeChannels", true);
            }
            set
            {
                SavePersistingSettingValue<bool>("ShowNonFreeChannels", value);
            }
        }

        public bool ShowRadioChannels
        {
            get
            {
                return GetPersistingSettingValue<bool>("ShowRadioChannels");
            }
            set
            {
                SavePersistingSettingValue<bool>("ShowRadioChannels", value);
            }
        }

        public bool ShowOtherChannels
        {
            get
            {
                return GetPersistingSettingValue<bool>("ShowOtherChannels");
            }
            set
            {
                SavePersistingSettingValue<bool>("ShowOtherChannels", value);
            }
        }

        public bool EnableLogging
        {
            get
            {
                return GetPersistingSettingValue<bool>("EnableLogging");
            }
            set
            {
                SavePersistingSettingValue<bool>("EnableLogging", value);
            }
        }

        public AppFontSizeEnum AppFontSize
        {
            get
            {
                var index = GetPersistingSettingValue<int>("AppFontSize");
                return (AppFontSizeEnum)index;
            }
            set
            {
                SavePersistingSettingValue<int>("AppFontSize", (int)value);
            }
        }

        public bool Fullscreen
        {
            get
            {
                return GetPersistingSettingValue<bool>("Fullscreen");
            }
            set
            {
                SavePersistingSettingValue<bool>("Fullscreen", value);
            }
        }

        public bool PlayOnBackground
        {
            get
            {
                return GetPersistingSettingValue<bool>("PlayOnBackground");
            }
            set
            {
                SavePersistingSettingValue<bool>("PlayOnBackground", value);
            }
        }

        public string ChannelAutoPlayedAfterStart
        {
            get
            {
                return GetPersistingSettingValue<string>("ChannelAutoPlayedAfterStart");
            }
            set
            {
                SavePersistingSettingValue<string>("ChannelAutoPlayedAfterStart", value);
            }
        }

        public long BandWidthKHz
        {
            get
            {
                return GetPersistingSettingValue<long>("BandWidthKHz");
            }
            set
            {
                SavePersistingSettingValue<long>("BandWidthKHz", value);
            }
        }

        public long FrequencyFromKHz
        {
            get
            {
                return GetPersistingSettingValue<long>("FrequencyFromKHz");
            }
            set
            {
                SavePersistingSettingValue<long>("FrequencyFromKHz", value);
            }
        }

        public long FrequencyToKHz
        {
            get
            {
                return GetPersistingSettingValue<long>("FrequencyToKHz");
            }
            set
            {
                SavePersistingSettingValue<long>("FrequencyToKHz", value);
            }
        }

        public long FrequencyKHz
        {
            get
            {
                return GetPersistingSettingValue<long>("FrequencyKHz");
            }
            set
            {
                SavePersistingSettingValue<long>("FrequencyKHz", value);
            }
        }

        public bool DVBTTuningDisabled
        {
            get
            {
                return GetPersistingSettingValue<bool>("DVBTTuningDisabled");
            }
            set
            {
                SavePersistingSettingValue<bool>("DVBTTuningDisabled", value);
            }
        }

        public bool DVBT2TuningDisabled
        {
            get
            {
                return GetPersistingSettingValue<bool>("DVBT2TuningDisabled");
            }
            set
            {
                SavePersistingSettingValue<bool>("DVBT2TuningDisabled", value);
            }
        }

        public bool ManualTuning
        {
            get
            {
                return GetPersistingSettingValue<bool>("ManualTuning");
            }
            set
            {
                SavePersistingSettingValue<bool>("ManualTuning", value);
            }
        }

        public bool FastTuning
        {
            get
            {
                return GetPersistingSettingValue<bool>("FastTuning");
            }
            set
            {
                SavePersistingSettingValue<bool>("FastTuning", value);
            }
        }

        public string SelectedChannelFrequencyAndMapPID
        {
            get
            {
                return GetPersistingSettingValue<string>("SelectedChannelFrequencyAndMapPID");
            }
            set
            {
                SavePersistingSettingValue<string>("SelectedChannelFrequencyAndMapPID", value);
            }
        }

        public int RemoteAccessServicePort
        {
            get
            {
                var port = GetPersistingSettingValue<int>("RemoteAccessServicePort");
                if (port == default(int))
                {
                    port = 49152;
                }

                return port;
            }
            set
            {
                SavePersistingSettingValue<int>("RemoteAccessServicePort", value);
            }
        }

        public string RemoteAccessServiceSecurityKey
        {
            get
            {
                var key = GetPersistingSettingValue<string>("RemoteAccessServiceSecurityKey");
                if (key == default(string))
                {
                    key = "DVBTTelevizor";
                }

                return key;
            }
            set { SavePersistingSettingValue<string>("RemoteAccessServiceSecurityKey", value); }
        }

        public string RemoteAccessServiceIP
        {
            get
            {
                var ip = GetPersistingSettingValue<string>("RemoteAccessServiceIP");
                if (ip == default(string))
                {
                    try
                    {
                        var ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
                        ip = ipHostInfo.AddressList[0].ToString();
                    }
                    catch
                    {
                        ip = "192.168.1.10";
                    }
                }

                return ip;
            }
            set { SavePersistingSettingValue<string>("RemoteAccessServiceIP", value); }
        }

        public bool AllowRemoteAccessService
        {
            get
            {
                return GetPersistingSettingValue<bool>("AllowRemoteAccessService");
            }
            set
            {
                SavePersistingSettingValue<bool>("AllowRemoteAccessService", value);
            }
        }
    }
}
