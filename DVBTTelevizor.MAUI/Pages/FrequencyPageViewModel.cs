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
        public bool Rounding { get; set; } = false;

        public FrequencyPageViewModel(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration, IDialogService dialogService, IPublicDirectoryProvider publicDirectoryProvider)
          : base(loggingService, driver, tvConfiguration, dialogService, publicDirectoryProvider)
        {
            _tuneSettings = new TuningSettings(_loggingService);

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

        public void SetDefaultFrequency(TuneFrequencyModeEnum tuneFrequencyModeEnum)
        {
            switch (tuneFrequencyModeEnum)
            {
                case TuneFrequencyModeEnum.From:
                    _tuneSettings.FrequencyKHz = _tuneSettings.DeviceFrequencyFromKHz;
                    break;
                case TuneFrequencyModeEnum.To:
                    _tuneSettings.FrequencyKHz = _tuneSettings.DeviceFrequencyToKHz;
                    break;
                case TuneFrequencyModeEnum.Center:
                    _tuneSettings.FrequencyKHz = _tuneSettings.DefaultFrequencyKHz;
                    break;
            }
            NotifyChange();
        }

        public long TuneBandWidthKHz
        {
            get
            {
                return Settings.BandwidthKHz;
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
                OnPropertyChanged(nameof(FrequencyMaxKHz));

                OnPropertyChanged(nameof(FrequencyKHz));
                OnPropertyChanged(nameof(FrequencyMHz));
                OnPropertyChanged(nameof(FrequencyWholePartMHz));
                OnPropertyChanged(nameof(FrequencyDecimalPartMHzCaption));

                OnPropertyChanged(nameof(TuneBandWidthKHz));

                OnPropertyChanged(nameof(FrequencyMinKHz));

                OnPropertyChanged(nameof(FrequencyMinMHz));
                OnPropertyChanged(nameof(FrequencyMaxMHz));

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
                return Settings.DeviceFrequencyFromKHz;
            }
        }

        public long FrequencyMaxKHz
        {
            get
            {
                return Settings.DeviceFrequencyToKHz;
            }
        }

        public long FrequencyMinMHz
        {
            get
            {
                return Settings.DeviceFrequencyFromKHz / 1000;
            }
        }

        public long FrequencyMaxMHz
        {
            get
            {
                return Settings.DeviceFrequencyToKHz / 1000;
            }
        }

        public void IncreaseFreq()
        {
            var freq = FrequencyKHz + TuneBandWidthKHz;
            if (!_tuneSettings.ValidFrequency(freq, true))
            {
                freq = FrequencyMaxKHz;
            }
            Settings.FrequencyKHz = freq;

            NotifyChange();
        }

        public void DecreaseFreq()
        {
            var freq = FrequencyKHz - TuneBandWidthKHz;
            if (!_tuneSettings.ValidFrequency(freq, true))
            {
                freq = FrequencyMinKHz;
            }
            Settings.FrequencyKHz = freq;

            NotifyChange();
        }

        public void RoundFrequency()
        {
            Rounding = true;
            try
            {
                if (!_tuneSettings.ValidFrequency(FrequencyKHz, true))
                    return;

                // rounding to start freq 474 MHZ
                var startFreq = _tuneSettings.DefaultFrequencyKHz;

                var stepFreq = Math.Round(Convert.ToDecimal(FrequencyKHz - startFreq) / Convert.ToDecimal(Settings.BandwidthKHz));

                var freqRounded = Convert.ToInt64(startFreq + stepFreq * Settings.BandwidthKHz);
                if (freqRounded > TuningSettings.FrequencyMaxKHz)
                {
                    freqRounded = TuningSettings.FrequencyMaxKHz;
                }
                if (freqRounded < TuningSettings.FrequencyMinKHz)
                {
                    freqRounded = TuningSettings.FrequencyMinKHz;
                }

                Settings.FrequencyKHz = freqRounded;
            } finally
            {
                Rounding = false;
            }
        }
    }
}

