using DVBTTelevizor.Models;
using LoggerService;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml.Internals;
using static Android.Renderscripts.Sampler;

namespace DVBTTelevizor
{
    public class TuneViewModel : BaseViewModel
    {
        protected string _tuneFrequency;

        protected long _tuneBandWidth = 8;

        protected long _tuneBandWidthKHz = 8000;

        public const long AutoTuningMinFrequencyKhzDefaultValue = 174000;  // 174.0 MHz - VHF high-band (band III) channel 7
        public const long AutoTuningMaxFrequencyKhzDefaultValue = 858000;  // 858.0 MHz - UHF band channel 69
        public const long AutoTuningFrequencyKHzDefaultValue = 470000;


        public const long BandWidthMinKHz = 1000;
        public const long BandWidthMaxKHz = 32000;
        public const long BandWidthDefaultKHz = 8000;

        public long _tuningFrequencyKHz { get; set; } = AutoTuningFrequencyKHzDefaultValue;

        public long _autoTuningMinFrequencyKHz { get; set; } = AutoTuningMinFrequencyKhzDefaultValue;
        public long _autoTuningMaxFrequencyKHz { get; set; } = AutoTuningMaxFrequencyKhzDefaultValue;

        public long _autoTuningFrequencyFromKHz { get; set; } = AutoTuningMinFrequencyKhzDefaultValue;
        public long _autoTuningFrequencyToKHz { get; set; } = AutoTuningMaxFrequencyKhzDefaultValue;

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

        public long AutoTuningMinFrequencyKHz
        {
            get
            {
                return _autoTuningMinFrequencyKHz;
            }
            set
            {
                _autoTuningMinFrequencyKHz = value;

                OnPropertyChanged(nameof(AutoTuningMinFrequencyKHz));
            }
        }

        public long AutoTuningMaxFrequencyKHz
        {
            get
            {
                return _autoTuningMaxFrequencyKHz;
            }
            set
            {
                _autoTuningMaxFrequencyKHz = value;

                OnPropertyChanged(nameof(AutoTuningMaxFrequencyKHz));
            }
        }

        public long AutoTuningFrequencyFromKHz
        {
            get
            {
                return _autoTuningFrequencyFromKHz;
            }
            set
            {
                _autoTuningFrequencyFromKHz = value;

                OnPropertyChanged(nameof(AutoTuningFrequencyFromKHz));
                OnPropertyChanged(nameof(AutoTuningFrequencyToKHz));
                OnPropertyChanged(nameof(AutoTuningFrequencyFromMHz));
                OnPropertyChanged(nameof(AutoTuningFrequencyFromMHzCaption));
                OnPropertyChanged(nameof(AutoTuningFrequencyToMHzCaption));
                OnPropertyChanged(nameof(AutoTuningFrequencyToMHz));
            }
        }

        public long TuningFrequencyKHz
        {
            get
            {
                return _tuningFrequencyKHz;
            }
            set
            {
                _tuningFrequencyKHz = value;

                OnPropertyChanged(nameof(TuningFrequencyKHz));
                OnPropertyChanged(nameof(TuningFrequencyMHz));
                OnPropertyChanged(nameof(TuningFrequencyMHzCaption));
            }
        }

        public string TuningFrequencyMHz
        {
            get
            {
                return (TuningFrequencyKHz / 1000.0).ToString("N3");
            }
        }

        public string TuningFrequencyMHzCaption
        {
            get
            {
                return TuningFrequencyMHz + " MHz";
            }
        }

        public long AutoTuningFrequencyToKHz
        {
            get
            {
                return _autoTuningFrequencyToKHz;
            }
            set
            {
                _autoTuningFrequencyToKHz = value;

                OnPropertyChanged(nameof(AutoTuningFrequencyFromKHz));
                OnPropertyChanged(nameof(AutoTuningFrequencyToKHz));
                OnPropertyChanged(nameof(TuningFrequencyBandWidthMHz));
                OnPropertyChanged(nameof(AutoTuningFrequencyFromMHz));
                OnPropertyChanged(nameof(AutoTuningFrequencyToMHzCaption));
                OnPropertyChanged(nameof(AutoTuningFrequencyToMHz));
            }
        }

        public string AutoTuningFrequencyToMHz
        {
            get
            {
                return Math.Round(AutoTuningFrequencyToKHz / 1000.0, 1).ToString();
            }
        }

        public string TuningFrequencyBandWidthMHz
        {
            get
            {
                return BandWidthMHz + " MHz";
            }
        }

        public string AutoTuningFrequencyFromMHz
        {
            get
            {
                return Math.Round(AutoTuningFrequencyFromKHz / 1000.0, 1).ToString();
            }
        }

        public string AutoTuningFrequencyFromMHzCaption
        {
            get
            {
                return AutoTuningFrequencyFromMHz + " MHz";
            }
        }

        public string AutoTuningFrequencyToMHzCaption
        {
            get
            {
                return AutoTuningFrequencyToMHz + " MHz";
            }
        }

        public string BandWidthMHz
        {
            get
            {
                return Math.Round(TuneBandWidthKHz / 1000.0, 1).ToString();
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
                return _tuneBandWidthKHz;
            }
            set
            {
                _tuneBandWidthKHz = value;

                OnPropertyChanged(nameof(TuneBandWidthKHz));
                OnPropertyChanged(nameof(BandWidthMHz));
                OnPropertyChanged(nameof(TuningFrequencyBandWidthMHz));
            }
        }

        public static long ParseFreqMHzToKHz(string freqMHz)
        {
            decimal freqMHzDecimal = -1;
            var sep = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;

            if (decimal.TryParse(freqMHz.Replace(".", sep).Replace(",", sep), out freqMHzDecimal))
            {
                return Convert.ToInt64(freqMHzDecimal * 1000);
            }
            else
            {
                return -1;
            }
        }
    }
}
