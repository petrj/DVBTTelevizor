using DVBTTelevizor.Models;
using LoggerService;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Text;
using Xamarin.Forms;

namespace DVBTTelevizor
{
    public class TuneViewModel : BaseViewModel
    {
        protected string _tuneFrequency;

        protected long _tuneBandWidth = 8;

        protected long _tuneBandWidthKhz = 8000;

        public const long AutoTuningMinFrequencyKhzDefaultValue = 174000;  // 174.0 MHz - VHF high-band (band III) channel 7
        public const long AutoTuningMaxFrequencyKhzDefaultValue = 858000;  // 858.0 MHz - UHF band channel 69

        public long _autoTuningMinFrequencyKhz { get; set; } = AutoTuningMinFrequencyKhzDefaultValue;
        public long _autoTuningMaxFrequencyKhz { get; set; } = AutoTuningMaxFrequencyKhzDefaultValue;

        public long _autoTuningFrequencyFromKhz { get; set; } = AutoTuningMinFrequencyKhzDefaultValue;
        public long _autoTuningFrequencyToKhz { get; set; } = AutoTuningMaxFrequencyKhzDefaultValue;

        public long AutomaticTuningFirstChannel { get; set; } = 21;
        public long AutomaticTuningLastChannel { get; set; } = 69;

        private DVBTChannel _selectedChannel;

        public ObservableCollection<DVBTFrequencyChannel> FrequencyChannels { get; set; } = new ObservableCollection<DVBTFrequencyChannel>();

        DVBTFrequencyChannel _selectedFrequencyChannel = null;

        public TuneViewModel(ILoggingService loggingService, IDialogService dialogService, IDVBTDriverManager driver, DVBTTelevizorConfiguration config)
         : base(loggingService, dialogService, driver, config)
        {
            FillFrequencyChannels();
        }

        protected void FillFrequencyChannels()
        {
            FrequencyChannels.Clear();

            for (var i = 21; i <= 69; i++)
            {
                var freqMhz = (474 + 8 * (i - 21));
                var fc = new DVBTFrequencyChannel()
                {
                    FrequencyMhZ = freqMhz,
                    ChannelNumber = i
                };

                FrequencyChannels.Add(fc);
            }
        }

        public long AutoTuningMinFrequencyKhz
        {
            get
            {
                return _autoTuningMinFrequencyKhz;
            }
            set
            {
                _autoTuningMinFrequencyKhz = value;

                OnPropertyChanged(nameof(AutoTuningMinFrequencyKhz));
            }
        }

        public long AutoTuningMaxFrequencyKhz
        {
            get
            {
                return _autoTuningMaxFrequencyKhz;
            }
            set
            {
                _autoTuningMaxFrequencyKhz = value;

                OnPropertyChanged(nameof(AutoTuningMaxFrequencyKhz));
            }
        }

        public long AutoTuningFrequencyFromKhz
        {
            get
            {
                return _autoTuningFrequencyFromKhz;
            }
            set
            {
                _autoTuningFrequencyFromKhz = value;

                //CheckFrequencies();

                OnPropertyChanged(nameof(AutoTuningFrequencyFromKhz));
                OnPropertyChanged(nameof(AutoTuningFrequencyToKhz));
                OnPropertyChanged(nameof(AutoTuningFrequencyFromToMHz));
                OnPropertyChanged(nameof(AutoTuningFrequencyFromMHz));
                OnPropertyChanged(nameof(AutoTuningFrequencyToMHz));
            }
        }

        public long AutoTuningFrequencyToKhz
        {
            get
            {
                return _autoTuningFrequencyToKhz;
            }
            set
            {
                _autoTuningFrequencyToKhz = value;

                //CheckFrequencies();

                OnPropertyChanged(nameof(AutoTuningFrequencyFromKhz));
                OnPropertyChanged(nameof(AutoTuningFrequencyToKhz));
                OnPropertyChanged(nameof(AutoTuningFrequencyFromToMHz));
                OnPropertyChanged(nameof(AutoTuningFrequencyFromMHz));
                OnPropertyChanged(nameof(AutoTuningFrequencyToMHz));
            }
        }

        public string AutoTuningFrequencyFromToMHz
        {
            get
            {
                return Math.Round(AutoTuningFrequencyFromKhz / 1000.0, 1).ToString() +
                       " - " +
                       Math.Round(AutoTuningFrequencyToKhz / 1000.0, 1).ToString() +
                       " MHz";
            }
        }

        public string AutoTuningFrequencyFromMHz
        {
            get
            {
                return Math.Round(AutoTuningFrequencyFromKhz / 1000.0, 1).ToString();
            }
        }

        public string AutoTuningFrequencyToMHz
        {
            get
            {
                return Math.Round(AutoTuningFrequencyToKhz / 1000.0, 1).ToString();
            }
        }

        public string BandWidthMHz
        {
            get
            {
                return Math.Round(TuneBandWidthKHz / 1000.0, 1).ToString() + " MHz";
            }
        }

        private void CheckFrequencies()
        {
            // round to BandWidth:
            if (TuneBandWidthKHz != 0)
            {
                var stepFreqFrom = Math.Round(Convert.ToDecimal(_autoTuningFrequencyFromKhz - AutoTuningMinFrequencyKhz) / Convert.ToDecimal(TuneBandWidthKHz));
                _autoTuningFrequencyFromKhz = Convert.ToInt64(AutoTuningMinFrequencyKhz + stepFreqFrom * TuneBandWidthKHz);

                var stepFreqTo = Math.Round(Convert.ToDecimal(_autoTuningFrequencyToKhz - AutoTuningMinFrequencyKhz) / Convert.ToDecimal(TuneBandWidthKHz));
                _autoTuningFrequencyToKhz = Convert.ToInt64(AutoTuningMinFrequencyKhz + stepFreqTo * TuneBandWidthKHz);
            }

            if (_autoTuningFrequencyFromKhz < AutoTuningMinFrequencyKhz)
            {
                _autoTuningFrequencyFromKhz = AutoTuningMinFrequencyKhz;
            }
            if (_autoTuningFrequencyFromKhz > AutoTuningMaxFrequencyKhz)
            {
                _autoTuningFrequencyFromKhz = AutoTuningMaxFrequencyKhz;
            }

            if (_autoTuningFrequencyToKhz < AutoTuningMinFrequencyKhz)
            {
                _autoTuningFrequencyToKhz = AutoTuningMinFrequencyKhz;
            }
            if (_autoTuningFrequencyToKhz > AutoTuningMaxFrequencyKhz)
            {
                _autoTuningFrequencyToKhz = AutoTuningMaxFrequencyKhz;
            }

            if (_autoTuningFrequencyFromKhz > _autoTuningFrequencyToKhz)
            {
                _autoTuningFrequencyToKhz = _autoTuningFrequencyFromKhz;
            }

            if (_autoTuningFrequencyToKhz < _autoTuningFrequencyFromKhz)
            {
                _autoTuningFrequencyFromKhz = _autoTuningFrequencyToKhz;
            }
        }

        public DVBTChannel SelectedChannel
        {
            get
            {
                return _selectedChannel;
            }
            set
            {
                _selectedChannel = value;

                OnPropertyChanged(nameof(SelectedChannel));
            }
        }

        public DVBTFrequencyChannel SelectedFrequencyChannelItem
        {
            get
            {
                return _selectedFrequencyChannel;
            }
            set
            {
                _selectedFrequencyChannel = value;

                if (value != null)
                {
                    TuneFrequency = (_selectedFrequencyChannel.FrequencyMhZ).ToString();
                }

                OnPropertyChanged(nameof(SelectedFrequencyChannelItem));
                OnPropertyChanged(nameof(TuneFrequency));
            }
        }

        public string TuneFrequency
        {
            get
            {
                return _tuneFrequency;
            }
            set
            {
                _tuneFrequency = value;

                foreach (var f in FrequencyChannels)
                {
                    if (f.FrequencyMhZ.ToString() == value)
                    {
                        _selectedFrequencyChannel = f;
                        break;
                    }
                }

                OnPropertyChanged(nameof(TuneFrequency));
                OnPropertyChanged(nameof(SelectedFrequencyChannelItem));
            }
        }

        public long TuneBandwidth
        {
            get
            {
                return _tuneBandWidth;
            }
            set
            {
                _tuneBandWidth = value;

                OnPropertyChanged(nameof(TuneBandwidth));
            }
        }

        public long TuneBandWidthKHz
        {
            get
            {
                return _tuneBandWidthKhz;
            }
            set
            {
                _tuneBandWidthKhz = value;

                OnPropertyChanged(nameof(TuneBandWidthKHz));
                OnPropertyChanged(nameof(BandWidthMHz));

            }
        }
    }
}
