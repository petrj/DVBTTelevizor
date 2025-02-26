using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using LoggerService;
using Microsoft.Maui;
using MPEGTS;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.Maui.ApplicationModel.Permissions;

namespace DVBTTelevizor.MAUI
{
    public class FrequencyPageViewModel : BaseViewModel
    {
        public TuningSettings _tuneSettings { get; set; }

        public FrequencyPageViewModel(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration, IDialogService dialogService, IPublicDirectoryProvider publicDirectoryProvider)
          : base(loggingService, driver, tvConfiguration, dialogService, publicDirectoryProvider)
        {
            _tuneSettings = new TuningSettings();

            WeakReferenceMessenger.Default.Register<FontSizeChangedMessage>(this, (r, m) =>
            {
                _loggingService.Info($"TuningProgressPageViewModel: FontSizeChanged");
                NotifyFontSizeChange();
            });
        }

        public TuningSettings Settings
        {
            get
            {
                return _tuneSettings;
            }
            set
            {
                _tuneSettings = value;
                NotifyChange();
            }
        }

        public long TuneBandWidthKHz
        {
            get
            {
                return Settings.BandwidthKHz;
            }
            set
            {
                Settings.BandwidthKHz = value;

                NotifyChange();
            }
        }

        public void NotifyChange()
        {
            _loggingService.Debug("NotifyChange");

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                OnPropertyChanged(nameof(FrequencyKHz));
                OnPropertyChanged(nameof(FrequencyWholePartMHz));
                OnPropertyChanged(nameof(FrequencyDecimalPartMHzCaption));

                OnPropertyChanged(nameof(TuneBandWidthKHz));

                OnPropertyChanged(nameof(FrequencyMinKHz));
                OnPropertyChanged(nameof(FrequencyMaxKHz));
                OnPropertyChanged(nameof(FrequencyMinMHz));
                OnPropertyChanged(nameof(FrequencyMaxMHz));
                OnPropertyChanged(nameof(FrequencyToMHz));

            });
        }

        public long FrequencyWholePartMHz
        {
            get
            {
                return Convert.ToInt64(Math.Floor(FrequencyKHz / 1000.0));
            }
        }

        public string FrequencyDecimalPartMHzCaption
        {
            get
            {
                var part = (FrequencyKHz / 1000.0) - FrequencyWholePartMHz;
                var part1000 = Convert.ToInt64(part * 1000).ToString().PadLeft(3, '0');
                return $".{part1000} MHz";
            }
        }

        public long FrequencyKHz
        {
            get
            {
                return Settings.FrequencyKHz;
            }
            set
            {
                Settings.FrequencyKHz = value;

                NotifyChange();
            }
        }

        public long FrequencyMinKHz
        {
            get
            {
                return Settings.FrequencyMinKHz;
            }
            set
            {
                Settings.FrequencyMinKHz = value;
                NotifyChange();
            }
        }

        public long FrequencyMaxKHz
        {
            get
            {
                return Settings.FrequencyMaxKHz;
            }
            set
            {
                Settings.FrequencyMaxKHz = value;
                NotifyChange();
            }
        }

        public long FrequencyMinMHz
        {
            get
            {
                return Settings.FrequencyMinKHz / 1000;
            }
        }

        public long FrequencyMaxMHz
        {
            get
            {
                return Settings.FrequencyMaxKHz / 1000;
            }
        }

        public long FrequencyToMHz
        {
            get
            {
                return Settings.FrequencyMaxKHz / 1000;
            }
        }
    }
}

