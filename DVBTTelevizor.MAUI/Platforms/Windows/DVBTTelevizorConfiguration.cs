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
    internal class DVBTTelevizorConfiguration : ITVCConfiguration
    {
        private ILoggingService _loggingService;
        private string _configDirectory = string.Empty;
        private bool _fullscreen = true;
        private bool _playOnBackground = true;
        private AppFontSizeEnum _appFontSize = AppFontSizeEnum.Normal;
        private DVBTDriverTypeEnum _DVBTDriverType = DVBTDriverTypeEnum.AndroidDVBTDriver;

        private bool _showTVChannels = true;
        private bool _showNonFreeChannels = false;
        private bool _showRadioChannels = false;
        private bool _showOtherChannels = false;

        private bool _enableLogging = false;

        private string _autoPlayedChannelFrequencyAndMapPID = string.Empty;

        [JsonProperty]
        public bool Fullscreen
        {
            get
            {
                return _fullscreen;
            }
            set
            {
                _fullscreen = value;
                Save();
            }
        }

        [JsonProperty]
        public bool PlayOnBackground
        {
            get
            {
                return _playOnBackground;
            }
            set
            {
                _playOnBackground = value;
                Save();
            }
        }

        [JsonProperty]
        public AppFontSizeEnum AppFontSize
        {
            get
            {
                return _appFontSize;
            }
            set
            {
                _appFontSize = value;
                Save();
            }
        }

        [JsonProperty]
        public DVBTDriverTypeEnum DVBTDriverType
        {
            get
            {
                return _DVBTDriverType;
            }
            set
            {
                _DVBTDriverType = value;
                Save();
            }
        }

        [JsonProperty]
        public bool ShowTVChannels
        {
            get
            {
                return _showTVChannels;
            }
            set
            {
                _showTVChannels = !value;
            }
        }

        [JsonProperty]
        public bool ShowNonFreeChannels
        {
            get
            {
                return _showNonFreeChannels;
            }
            set
            {
                _showNonFreeChannels = value;
                Save();
            }
        }

        [JsonProperty]
        public bool ShowRadioChannels
        {
            get
            {
                return _showRadioChannels;
            }
            set
            {
                _showRadioChannels = value;
                Save();
            }
        }

        [JsonProperty]
        public bool ShowOtherChannels
        {
            get
            {
                return _showOtherChannels;
            }
            set
            {
                _showOtherChannels = value;
                Save();
            }
        }

        [JsonProperty]
        public bool EnableLogging
        {
            get
            {
                return _enableLogging;
            }
            set
            {
                _enableLogging = value;
                Save();
            }
        }

        [JsonProperty]
        public string AutoPlayedChannelFrequencyAndMapPID
        {
            get
            {
                return _autoPlayedChannelFrequencyAndMapPID;
            }
            set
            {
                _autoPlayedChannelFrequencyAndMapPID = value;
                Save();
            }
        }

        public DVBTTelevizorConfiguration(ILoggingProvider loggingProvider)
        {
            if (loggingProvider != null)
            {
                _loggingService = loggingProvider.GetLoggingService();
            } else
            {
                _loggingService = new BasicLoggingService();
            }
        }

        public ObservableCollection<Channel> Channels { get; set; } = new ObservableCollection<Channel>();

        private string ConfigFileName
        {
            get
            {
                return Path.Join(ConfigDirectory, "DVBTTelevizor.MAUI.config.json");
            }
        }

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
                if (File.Exists(ChannelsConfigFileName))
                {
                    var json = File.ReadAllText(ChannelsConfigFileName);

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

                if (File.Exists(ConfigFileName))
                {
                    var jsonAll = File.ReadAllText(ConfigFileName);

                    var cfg = JsonConvert.DeserializeObject<DVBTTelevizorConfiguration>(jsonAll);

                    if (cfg != null)
                    {
                        Fullscreen = cfg.Fullscreen;
                        AppFontSize = cfg.AppFontSize;
                        DVBTDriverType = cfg.DVBTDriverType;
                        ShowNonFreeChannels = cfg.ShowNonFreeChannels;
                        ShowTVChannels = cfg.ShowTVChannels;
                        ShowOtherChannels = cfg.ShowOtherChannels;
                        ShowRadioChannels = cfg.ShowRadioChannels;
                        EnableLogging = cfg.EnableLogging;
                        AutoPlayedChannelFrequencyAndMapPID = cfg.AutoPlayedChannelFrequencyAndMapPID;
                    }
                } else
                {
                    // Saving default configuration
                    Save();
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
                File.WriteAllText(ChannelsConfigFileName, json);

                _loggingService.Info("Saving configuration");

                var jsonAll = JsonConvert.SerializeObject(this);
                File.WriteAllText(ConfigFileName, jsonAll);
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
        }
    }
}
