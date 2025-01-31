using DVBTTelevizor.MAUI;
using LoggerService;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI
{
    public abstract class ChannelService
    {
        protected ILoggingService _log = new BasicLoggingService();
        protected ITVCConfiguration _config;

        public ChannelService(ILoggingService logingService, ITVCConfiguration config)
        {
            _log = logingService;
            _config = config;
        }

        public async virtual Task<ObservableCollection<Channel>> LoadChannels()
        {
            return await Task.Run(() =>
            {
                return new ObservableCollection<Channel>();
            });
        }

        public async virtual Task<bool> SaveChannels(ObservableCollection<Channel> channels)
        {
            return await Task.Run(() =>
            {
                return false;
            });
        }
        /*
        public virtual string DBPath
        {
            get
            {
                return Path.Combine(BaseViewModel.AndroidAppDirectory, "channels.sqllite");
            }
        }
        */
    }
}
