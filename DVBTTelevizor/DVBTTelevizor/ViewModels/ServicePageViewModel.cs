using Xamarin.Forms;
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using LoggerService;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
using System.Threading;
using Newtonsoft.Json;

namespace DVBTTelevizor
{
    public class ServicePageViewModel : BaseViewModel
    {
        private long _tuneFrequency = 730;
        private long _tuneBandwidth = 8;
        private int _tuneDVBTType = 0;

        public ServicePageViewModel(ILoggingService loggingService, IDialogService dialogService, DVBTDriverManager driver, DVBTTelevizorConfiguration config)
         : base(loggingService, dialogService, driver, config)
        {

        }

        public long TuneFrequency
        {
            get
            {
                return _tuneFrequency;
            }
            set
            {
                _tuneFrequency = value;

                OnPropertyChanged(nameof(TuneFrequency));
            }
        }

        public long TuneBandwidth
        {
            get
            {
                return _tuneBandwidth;
            }
            set
            {
                _tuneBandwidth = value;

                OnPropertyChanged(nameof(TuneBandwidth));
            }
        }

        public int TuneDVBTType
        {
            get
            {
                return _tuneDVBTType;
            }
            set
            {
                _tuneDVBTType = value;

                OnPropertyChanged(nameof(TuneDVBTType));
            }
        }

    }
}
