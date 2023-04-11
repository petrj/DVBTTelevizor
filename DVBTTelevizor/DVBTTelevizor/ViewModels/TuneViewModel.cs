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

        public Command SetDefaultFrequenciesCommand { get; set; }

        public const long AutoTuningMinFrequencyKhzDefaultValue = 174000;  // 174.0 MHz - VHF high-band (band III) channel 7
        public const long AutoTuningMaxFrequencyKhzDefaultValue = 858000;  // 858.0 MHz - UHF band channel 69

        public const long BandWidthMinKHz = 1000;
        public const long BandWidthMaxKHz = 32000;
        public const long BandWidthDefaultKHz = 8000;

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

            SetDefaultFrequenciesCommand = new Command(async () => await SetDefaultFrequencies());
        }

        public async Task SetDefaultFrequencies()
        {
            AutoTuningFrequencyFromKHz = AutoTuningMinFrequencyKhzDefaultValue;
            AutoTuningFrequencyToKHz = AutoTuningMaxFrequencyKhzDefaultValue;
            TuneBandWidthKHz = BandWidthDefaultKHz;
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

                if (!ValidFrequency(value))
                    return;

                OnPropertyChanged(nameof(AutoTuningFrequencyFromKHz));
                OnPropertyChanged(nameof(AutoTuningFrequencyToKHz));
                OnPropertyChanged(nameof(AutoTuningFrequencyFromToMHz));
                OnPropertyChanged(nameof(AutoTuningFrequencyFromMHz));
                OnPropertyChanged(nameof(AutoTuningFrequencyFromMHzCaption));
                OnPropertyChanged(nameof(AutoTuningFrequencyToMHz));
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

                if (!ValidFrequency(value))
                    return;

                OnPropertyChanged(nameof(AutoTuningFrequencyFromKHz));
                OnPropertyChanged(nameof(AutoTuningFrequencyToKHz));
                OnPropertyChanged(nameof(AutoTuningFrequencyFromToMHz));
                OnPropertyChanged(nameof(TuningFrequencyBandWidthMHz));
                OnPropertyChanged(nameof(AutoTuningFrequencyFromMHz));
                OnPropertyChanged(nameof(AutoTuningFrequencyFromMHzCaption));
                OnPropertyChanged(nameof(AutoTuningFrequencyToMHz));
            }
        }

        public string AutoTuningFrequencyToMHz
        {
            get
            {
                return Math.Round(AutoTuningFrequencyToKHz / 1000.0, 1).ToString();
            }
            set
            {
                if (!ValidFrequency(value))
                    return;

                var freqKHz = ParseFreqMHzToKHz(value);

                if (AutoTuningFrequencyToKHz != freqKHz)
                {
                    AutoTuningFrequencyToKHz = freqKHz;
                }
            }
        }

        public string AutoTuningFrequencyFromToMHz
        {
            get
            {
                return Math.Round(AutoTuningFrequencyFromKHz / 1000.0, 1).ToString() +
                       " - " +
                       Math.Round(AutoTuningFrequencyToKHz / 1000.0, 1).ToString() +
                       " MHz";
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
            set
            {
                if (!ValidFrequency(value))
                    return;

                var freqKHz = ParseFreqMHzToKHz(value);

                if (AutoTuningFrequencyFromKHz != freqKHz)
                {
                    AutoTuningFrequencyFromKHz = freqKHz;
                }
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
            set
            {
                if (!ValidBandWidth(value))
                    return;

                var freqKHz = ParseFreqMHzToKHz(value);

                if (TuneBandWidthKHz != freqKHz)
                {
                    TuneBandWidthKHz = freqKHz;
                }
            }
        }

        public int BandWidthPickerIndex
        {
            get
            {
                switch (_tuneBandWidthKHz)
                {
                    case 01700: return 1;
                    case 05000: return 2;
                    case 06000: return 3;
                    case 07000: return 4;
                    case 08000: return 5;
                    case 10000: return 6;
                    default:
                        return 0;
                }
            }
            set
            {
                long bandWidthKHz;

                switch (value)
                {
                    case 0:
                        return; // custom bandwidth
                    case 1:
                        bandWidthKHz = 1700;
                        break;
                    case 2:
                        bandWidthKHz = 5000;
                        break;
                    case 3:
                        bandWidthKHz = 6000;
                        break;
                    case 4:
                        bandWidthKHz = 7000;
                        break;
                    case 5:
                        bandWidthKHz = 8000;
                        break;
                    case 6:
                        bandWidthKHz = 10000;
                        break;
                    default:
                        return;
                }

                if (TuneBandWidthKHz != bandWidthKHz)
                {
                    TuneBandWidthKHz = bandWidthKHz;

                    OnPropertyChanged(nameof(BandWidthMHz));
                    OnPropertyChanged(nameof(TuneBandWidthKHz));
                }

                OnPropertyChanged(nameof(BandWidthPickerIndex));
            }
        }

        public bool ValidBandWidth(long freqKHz)
        {
            if (freqKHz < BandWidthMinKHz || freqKHz > BandWidthMaxKHz)
            {
                return false;
            }

            return true;
        }

        public bool ValidBandWidth(string freqMHz)
        {
            var freqKHz = ParseFreqMHzToKHz(freqMHz);
            if (freqKHz == -1)
            {
                return false;
            }

            return ValidBandWidth(freqKHz);
        }

        public bool ValidFrequency(long freqKHz)
        {

            if (freqKHz < _autoTuningMinFrequencyKHz || freqKHz > _autoTuningMaxFrequencyKHz)
            {
                return false;
            }

            return true;
        }

        public bool ValidFrequency(string freqMHz)
        {
            var freqKHz = ParseFreqMHzToKHz(freqMHz);
            if (freqKHz == -1)
            {
                return false;
            }

            return ValidFrequency(freqKHz);
        }

        public long ParseFreqMHzToKHz(string freqMHz)
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

                if (!ValidBandWidth(value))
                    return;

                OnPropertyChanged(nameof(TuneBandWidthKHz));
                OnPropertyChanged(nameof(BandWidthMHz));
                OnPropertyChanged(nameof(BandWidthPickerIndex));
            }
        }
    }
}
