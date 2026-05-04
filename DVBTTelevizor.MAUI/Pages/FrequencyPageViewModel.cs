using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using LoggerService;
using RTLSDR.Common;

namespace DVBTTelevizor.MAUI
{
    public class FrequencyPageViewModel : BaseViewModel
    {
        public bool NotifyEnabled { get; set; } = true;
        public bool Rounding { get; set; } = false;

        private TuningSettings _tuneSettings { get; set; }
        private bool isReadonly = false;
        private IDriverConnector _driver;

        public FrequencyPageViewModel(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration, IPublicDirectoryProvider publicDirectoryProvider)
          : base(loggingService, driver, tvConfiguration, publicDirectoryProvider)
        {
            _tuneSettings = new TuningSettings(_loggingService);
            _driver = driver;
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
                case TuneFrequencyModeEnum.Center:
                default:
                    _tuneSettings.FrequencyKHz = _tuneSettings.DefaultFrequencyMinKHz;
                    break;
                case TuneFrequencyModeEnum.To:
                    _tuneSettings.FrequencyKHz = _tuneSettings.DefaultFrequencyMaxKHz;
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
            //_loggingService.Debug("NotifyChange");

            if (!NotifyEnabled)
            {
                _loggingService.Debug("NotifyChange disabled");
                //return; On some android is KHzEntry focused after Appearing and it disables notify change!
            }

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                isReadonly = true;

                OnPropertyChanged(nameof(FrequencyMaxKHz));

                OnPropertyChanged(nameof(FrequencyKHz));   // this invokes value change! using "isReadonly" disables changing the value
                OnPropertyChanged(nameof(FrequencyMHz));
                OnPropertyChanged(nameof(FrequencyWholePartMHzCaption));
                OnPropertyChanged(nameof(FrequencyDecimalPartMHzCaption));

                OnPropertyChanged(nameof(TuneBandWidthKHz));

                OnPropertyChanged(nameof(FrequencyMinKHz));

                OnPropertyChanged(nameof(FrequencyMinMHz));
                OnPropertyChanged(nameof(FrequencyMaxMHz));

                isReadonly = false;
            });
        }

        private static long GetNearestDABFreq(long freq)
        {
            var min = long.MaxValue;
            foreach (var kvp in RTLSDR.Common.AudioTools.DabFrequenciesHz)
            {
                if (Math.Abs(kvp.Value - freq) < Math.Abs(min - freq))
                {
                    min = kvp.Value;
                }
            }

            return min;
        }

        public string FrequencyWholePartMHzCaption
        {
            get
            {
                if (Settings.DAB)
                {
                    var dabFreq = GetNearestDABFreq(FrequencyKHz * 1000);
                    return TuningFrequenciesViewModel.ParseDabFreq((int)(dabFreq));
                }
                else
                {
                    return Convert.ToInt64(Math.Floor(FrequencyKHz / 1000.0)).ToString();
                }
            }
        }

        public string FrequencyDecimalPartMHzCaption
        {
            get
            {
                if (Settings.DAB)
                {
                    return "";
                }

                var part = (FrequencyKHz / 1000.0) - Math.Floor(FrequencyKHz / 1000.0);
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
                if (isReadonly)
                    return;

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
                return Settings.DeviceFrequencyMinKHz;
            }
        }

        public long FrequencyMaxKHz
        {
            get
            {
                return Settings.DeviceFrequencyMaxKHz;
            }
        }

        public long FrequencyMinMHz
        {
            get
            {
                return Settings.DeviceFrequencyMinKHz / 1000;
            }
        }

        public long FrequencyMaxMHz
        {
            get
            {
                return Settings.DeviceFrequencyMaxKHz / 1000;
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

            RoundFrequency();

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

            RoundFrequency();

            NotifyChange();
        }

        public void RoundFrequency()
        {
            Rounding = true;
            try
            {
                if (!_tuneSettings.ValidFrequency(FrequencyKHz, true))
                    return;

                Int64 freqRounded = _tuneSettings.DefaultFrequencyMinKHz;

                // rounding to start freq
                if (_tuneSettings.DAB)
                {
                    // there is not constant bandwidth, so rounding is different
                    var minFreqDist = long.MaxValue;
                    foreach (var freq in AudioTools.DabFrequenciesHz)
                    {
                        var dist = freq.Value - FrequencyKHz * 1000;
                        if (Math.Abs(dist) < Math.Abs(minFreqDist))
                        {
                            minFreqDist = dist;
                            freqRounded = freq.Value / 1000;
                        }
                    }
                }
                else
                {
                    var startFreq = _tuneSettings.DefaultFrequencyMinKHz;

                    var stepFreq = Math.Round(Math.Truncate(Convert.ToDecimal(FrequencyKHz - startFreq) / Convert.ToDecimal(Settings.BandwidthKHz)));

                    freqRounded = Convert.ToInt64(startFreq + stepFreq * Settings.BandwidthKHz);
                }


                if (freqRounded > _tuneSettings.DeviceFrequencyMaxKHz)
                {
                    freqRounded = _tuneSettings.DeviceFrequencyMaxKHz;
                }
                if (freqRounded < _tuneSettings.DeviceFrequencyMinKHz)
                {
                    freqRounded = _tuneSettings.DeviceFrequencyMinKHz;
                    //freqRounded = Convert.ToInt64(TuningSettings.DefaultDVBTFrequencyMinKHz + (stepFreq+1)* Convert.ToDecimal(Settings.BandwidthKHz));
                }

                Settings.FrequencyKHz = freqRounded;
            } finally
            {
                Rounding = false;
            }
        }
    }
}

