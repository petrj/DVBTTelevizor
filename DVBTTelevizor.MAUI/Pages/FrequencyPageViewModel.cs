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

        public bool NotifyEnabled { get; set; } = true;

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

        public bool ValidFrequencyKHz(long freqKHz)
        {

            if (freqKHz < _tuneSettings.FrequencyMinKHz || freqKHz > _tuneSettings.FrequencyMaxKHz)
            {
                return false;
            }

            return true;
        }

        public bool ValidFrequencyKHz(double freqKHz)
        {
            return ValidFrequencyKHz(Convert.ToInt64(freqKHz));
        }

        public bool ValidFrequencyMHz(double freqMHz)
        {
            return ValidFrequencyKHz(Convert.ToInt64(freqMHz*1000));
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

            if (!NotifyEnabled)
            {
                _loggingService.Debug("NotifyChange disabled");
                return;
            }

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                OnPropertyChanged(nameof(FrequencyKHz));
                OnPropertyChanged(nameof(FrequencyMHz));
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
                _loggingService.Debug($"Setting value:{value}");
                Settings.FrequencyKHz = value;
                NotifyChange();
            }
        }

        public string FrequencyMHz
        {
            get
            {
                return (Settings.FrequencyKHz / 1000.0).ToString("0.###");
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

