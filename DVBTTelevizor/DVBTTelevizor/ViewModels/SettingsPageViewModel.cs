using LoggerService;
using System;
using System.Collections.Generic;
using System.Text;

namespace DVBTTelevizor
{
    public class SettingsPageViewModel
    {
        protected ILoggingService _loggingService;
        protected IDialogService _dialogService;

        public SettingsPageViewModel(ILoggingService loggingService, IDialogService dialogService, DVBTTelevizorConfiguration config)
        {
            _loggingService = loggingService;
            _dialogService = dialogService;
            Config = config;
        }

        public DVBTTelevizorConfiguration Config { get; set; }
    }
}
