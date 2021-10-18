using LoggerService;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor
{
    public abstract class ChannelService
    {
        protected ILoggingService _log = new BasicLoggingService();
        protected DVBTTelevizorConfiguration _config;

        public ChannelService(ILoggingService logingService, DVBTTelevizorConfiguration config)
        {
            _log = logingService;
            _config = config;
        }

        public async virtual Task<ObservableCollection<DVBTChannel>> LoadChannels()
        {
            return await Task.Run(() =>
            {
                return new ObservableCollection<DVBTChannel>();
            });
        }

        public async virtual Task<bool> SaveChannels(ObservableCollection<DVBTChannel> channels)
        {
            return await Task.Run(() =>
            {
                return false;
            });
        }

        public virtual string DBPath
        {
            get
            {
                return Path.Combine(BaseViewModel.GetAndroidMediaDirectory(_config), "channels.sqllite");
            }
        }
    }
}
