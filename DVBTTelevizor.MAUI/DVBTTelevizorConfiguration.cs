using CommunityToolkit.Mvvm.Messaging;
using GoogleGson;
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
    internal class DVBTTelevizorConfiguration : ITVCConfiguration
    {
        private ILoggingService _loggingService;
        private string _configPath = string.Empty;

        public DVBTTelevizorConfiguration(ILoggingProvider loggingProvider, IPublicDirectoryProvider publicDirectoryProvider)
        {
            _loggingService = loggingProvider.GetLoggingService();
            _configPath = publicDirectoryProvider.GetPublicDirectoryPath();
        }

        public ObservableCollection<Channel> Channels { get; set; } = new ObservableCollection<Channel>();

        private string ConfigFileName
        {
            get
            {
                return Path.Join(_configPath,"config.json");
            }
        }

        private string ChannelsConfigFileName
        {
            get
            {
                return Path.Join(_configPath, "channels.json");
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
            if (!File.Exists(ChannelsConfigFileName))
            {
                return;
            }

            var json = File.ReadAllText(ChannelsConfigFileName);

            var loadedChannels = JsonConvert.DeserializeObject<ObservableCollection<Channel>>(json);

            if (loadedChannels == null || loadedChannels.Count == 0)
            {
                return;
            }

            Channels.Clear();

            foreach (var channel in loadedChannels)
            {
                Channels.Add(channel.Clone());
            }
        }

        public void LoadChannels()
        {
            //throw new NotImplementedException();
        }

        public void Save()
        {
            var json = JsonConvert.SerializeObject(Channels);
            File.WriteAllText(ChannelsConfigFileName, json);
        }

    }
}
