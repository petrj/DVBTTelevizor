using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using LoggerService;
using Microsoft.Maui;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI
{
    public class ChannelPageViewModel : BaseViewModel
    {
        private Channel? _channel = null;

        public ChannelPageViewModel(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration, IDialogService dialogService, IPublicDirectoryProvider publicDirectoryProvider)
          : base(loggingService, driver, tvConfiguration, dialogService, publicDirectoryProvider)
        {
        }

        public Channel? Channel
        {
            get
            {
                return _channel;
            }
            set
            {
                _channel = value;

                OnPropertyChanged(nameof(Channel));
            }
        }
    }
}

