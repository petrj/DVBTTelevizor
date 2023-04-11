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
    public class FrequencyViewModel : BaseViewModel
    {
        private long _autoTuningMinFrequencyKHz { get; set; } = 174000;
        private long _autoTuningMaxFrequencyKHz { get; set; } = 858000;
        private long _autoTuningFrequencyKHz { get; set; } = 174000;

        private string _title { get; set; }

        public long BandWidthKHz { get; set; }
        public long DefaultFrequencyKHz { get; set; }

        public Command SetDefaultFrequencyCommand { get; set; }

        public FrequencyViewModel(ILoggingService loggingService, IDialogService dialogService, IDVBTDriverManager driver, DVBTTelevizorConfiguration config)
         : base(loggingService, dialogService, driver, config)
        {
            SetDefaultFrequencyCommand = new Command(() => { AutoTuningFrequencyKHz = DefaultFrequencyKHz; });
        }

        public string Title
        {
            get
            {
                return _title;
            }
            set
            {
                _title = value;

                OnPropertyChanged(nameof(Title));
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

        public long AutoTuningFrequencyKHz
        {
            get
            {
                return _autoTuningFrequencyKHz;
            }
            set
            {
                _autoTuningFrequencyKHz = value;

                if (!ValidFrequency(value))
                    return;

                OnPropertyChanged(nameof(AutoTuningFrequencyKHz));
                OnPropertyChanged(nameof(AutoTuningFrequencyMHz));
            }
        }

        public string AutoTuningFrequencyMHz
        {
            get
            {
                return Math.Round(AutoTuningFrequencyKHz / 1000.0, 3).ToString();
            }
            set
            {
                if (!ValidFrequency(value))
                    return;

                var freqKHz = ParseFreqMHzToKHz(value);

                if (AutoTuningFrequencyKHz != freqKHz)
                {
                    AutoTuningFrequencyKHz = freqKHz;
                }
            }
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
    }
}
