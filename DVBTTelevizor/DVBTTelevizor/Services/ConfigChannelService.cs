using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using LoggerService;
using Newtonsoft.Json;

namespace DVBTTelevizor
{
    public class ConfigChannelService : ChannelService
    {

        public ConfigChannelService(ILoggingService logingService, DVBTTelevizorConfiguration config) : base(logingService, config)
        {
        }

        public override async Task<ObservableCollection<DVBTChannel>> LoadChannels()
        {
            return await Task.Run(() =>
            {
                var chs = _config.Channels;
                if (chs == null)
                    chs = new ObservableCollection<DVBTChannel>();

                return chs;
            });
        }

        public override async Task<bool> SaveChannels(ObservableCollection<DVBTChannel> channels)
        {
            return await Task.Run(() =>
            {
                var chs = new ObservableCollection<DVBTChannel>();

                foreach (var ch in channels)
                {
                    chs.Add(ch.Clone());
                }

                _config.Channels = chs;
                return true;
            });
        }
    }
}
