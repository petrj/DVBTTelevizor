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
        private bool _manualTuning = false;

        protected long _tuneBandWidthKHz = 8000;
        public long _tuningFrequencyKHz { get; set; } = AutoTuningFrequencyKHzDefaultValue;

        public long _autoTuningFrequencyFromKHz { get; set; } = AutoTuningMinFrequencyKHzDefaultValue;
        public long _autoTuningFrequencyToKHz { get; set; } = AutoTuningMaxFrequencyKHzDefaultValue;


        public const long AutoTuningMinFrequencyKHzDefaultValue = 174000;  // 174.0 MHz - VHF high-band (band III) channel 7
        public const long AutoTuningMaxFrequencyKHzDefaultValue = 858000;  // 858.0 MHz - UHF band channel 69
        public const long AutoTuningFrequencyKHzDefaultValue = 470000;

        public const long BandWidthMinKHz = 1000;
        public const long BandWidthMaxKHz = 32000;
        public const long BandWidthDefaultKHz = 8000;

        public long _autoTuningMinFrequencyKHz { get; set; } = AutoTuningMinFrequencyKHzDefaultValue;
        public long _autoTuningMaxFrequencyKHz { get; set; } = AutoTuningMaxFrequencyKHzDefaultValue;

        private DVBTChannel _selectedChannel;

        public ObservableCollection<DVBTChannel> TunedChannels { get; set; } = new ObservableCollection<DVBTChannel>();

        DVBTFrequencyChannel _selectedFrequencyChannel = null;

        public TuneViewModel(ILoggingService loggingService, IDialogService dialogService, IDVBTDriverManager driver, DVBTTelevizorConfiguration config, ChannelService channelService)
         : base(loggingService, dialogService, driver, config)
        {

        }

        public bool ManualTuning
        {
            get
            {
                return _manualTuning;
            }
            set
            {
                _manualTuning = value;

                OnPropertyChanged(nameof(ManualTuning));
                OnPropertyChanged(nameof(AutomaticTuning));
            }
        }

        public bool AutomaticTuning
        {
            get
            {
                return !_manualTuning;
            }
            set
            {
                _manualTuning = !value;

                OnPropertyChanged(nameof(AutomaticTuning));
                OnPropertyChanged(nameof(ManualTuning));
            }
        }

        public int TuneModeIndex
        {
            get
            {
                return _manualTuning ? 1 : 0;
            }
            set
            {
                ManualTuning = value == 1;

                OnPropertyChanged(nameof(ManualTuning));
                OnPropertyChanged(nameof(TuneModeIndex));
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

        public async Task SetChannelsRange()
        {
            _loggingService.Info("SetChannelsRange");

            try
            {
                var cap = await _driver.GetCapabalities();

                if (!cap.SuccessFlag)
                {
                    throw new Exception("Response not success");
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
            finally
            {

            }
        }
    }
}
