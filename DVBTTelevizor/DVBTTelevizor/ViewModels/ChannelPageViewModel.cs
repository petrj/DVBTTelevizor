﻿using LoggerService;
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

                NotifyChannelChange();
            }
        }

        public void NotifyChannelChange()
        {
            OnPropertyChanged(nameof(Channel));
        }

        public ChannelPageViewModel(ILoggingService loggingService, IDialogService dialogService, IDVBTDriverManager driver, DVBTTelevizorConfiguration config)
            :base(loggingService, dialogService, driver, config)
        {
            _loggingService = loggingService;
            _dialogService = dialogService;
        }
    }
}
