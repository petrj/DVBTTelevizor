using CommunityToolkit.Mvvm.Messaging;
using LoggerService;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI
{
    [JsonObject(MemberSerialization.OptIn)]
    internal class DVBTTelevizorConfiguration : CustomSharedPreferencesObject, ITVCConfiguration
    {
        private ILoggingService _loggingService;
        private string _configDirectory = string.Empty;

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

        public DVBTDriverTypeEnum DVBTDriverType
        {
            get
            {
                var index = GetPersistingSettingValue<int>("DVBTDriverType");
                return (DVBTDriverTypeEnum)index;
            }
            set
            {
                SavePersistingSettingValue<int>("DVBTDriverType", (int)value);
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

        public string AutoPlayedChannelFrequencyAndMapPID
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

        public DVBTTelevizorConfiguration(ILoggingProvider loggingProvider, IPublicDirectoryProvider publicDirectoryProvider)
        {
            if (loggingProvider != null)
            {
                _loggingService = loggingProvider.GetLoggingService();
            } else
            {
                _loggingService = new BasicLoggingService();
            }

            if (publicDirectoryProvider != null)
            {
                _configDirectory = publicDirectoryProvider.GetPublicDirectoryPath();
            }
        }

        public ObservableCollection<Channel> Channels { get; set; } = new ObservableCollection<Channel>();

        private string ChannelsConfigFileName
        {
            get
            {
                return Path.Join(ConfigDirectory, "DVBTTelevizor.MAUI.channels.json");
            }
        }

        public string ConfigDirectory
        {
            get
            {
                return _configDirectory;
            }
            set
            {
                _configDirectory = value;
            }
        }

        public int ImportChannelsFromJSON(string json)
        {
            try
            {
                var importedChannels = JsonConvert.DeserializeObject<ObservableCollection<Channel>>(json);

                var count = 0;
                foreach (var ch in importedChannels)
                {
                    if (!ch.ChannelExists(Channels))
                    {
                        count++;
                        ch.Number = Channel.GetNextChannelNumber(Channels).ToString();
                        Channels.Add(ch);
                    }
                }

                return count;
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "Import failed");
                return -1;
            }
        }

        public void Load()
        {
            try
            {
                var json = GetPersistingSettingValue<string>("ChannelsJson");
                if (string.IsNullOrEmpty(json) && (File.Exists(ChannelsConfigFileName)))
                {
                    json = File.ReadAllText(ChannelsConfigFileName);
                }

                if (!string.IsNullOrEmpty(json))
                {
                    var loadedChannels = JsonConvert.DeserializeObject<ObservableCollection<Channel>>(json);

                    if (loadedChannels != null && loadedChannels.Count > 0)
                    {
                        Channels.Clear();

                        foreach (var channel in loadedChannels)
                        {
                            Channels.Add(channel.Clone());
                        }
                    }
                }

            } catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
        }

        public void Save()
        {
            try
            {
                _loggingService.Info("Saving channels");

                var json = JsonConvert.SerializeObject(Channels);

                SavePersistingSettingValue<string>("ChannelsJson", JsonConvert.SerializeObject(Channels));

                File.WriteAllText(ChannelsConfigFileName, json);
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
        }
    }
}
