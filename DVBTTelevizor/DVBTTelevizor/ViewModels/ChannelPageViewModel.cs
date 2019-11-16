using LoggerService;
using System;
using System.Collections.Generic;
using System.Text;

namespace DVBTTelevizor
{
    public class ChannelPageViewModel : BaseViewModel
    {
        private DVBTChannel _channel;

        public DVBTChannel Channel
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

       public ChannelPageViewModel(ILoggingService loggingService, IDialogService dialogService, DVBTDriverManager driver, DVBTTelevizorConfiguration config)
            :base(loggingService, dialogService, driver, config)
        {
            _loggingService = loggingService;
            _dialogService = dialogService;            
        }
    }
}
