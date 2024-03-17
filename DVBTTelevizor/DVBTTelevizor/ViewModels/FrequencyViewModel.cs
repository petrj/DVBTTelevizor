using LoggerService;
using System;
using Xamarin.Forms;

namespace DVBTTelevizor
{
    public class FrequencyViewModel : BaseViewModel
    {
        private long _minFrequencyKHz { get; set; } = 174000;
        private long _maxFrequencyKHz { get; set; } = 858000;
        private long _frequencyKHz { get; set; } = 174000;

        private string _title { get; set; }

        public long FrequencyKHzSliderStep { get; set; } = 1000;
        public long FrequencyKHzDefault { get; set; } = 174000;

        public Command SetDefaultFrequencyCommand { get; set; }

        public Command RightFrequencyCommand { get; set; }
        public Command LeftFrequencyCommand { get; set; }

        public FrequencyViewModel(ILoggingService loggingService, IDialogService dialogService, IDVBTDriverManager driver, DVBTTelevizorConfiguration config)
         : base(loggingService, dialogService, driver, config)
        {
            SetDefaultFrequencyCommand = new Command(() => { FrequencyKHz = FrequencyKHzDefault; });

            LeftFrequencyCommand = new Command(() => { ChangeFreq(false); });
            RightFrequencyCommand = new Command(() => { ChangeFreq(true); });
        }

        private void ChangeFreq(bool upOrDown)
        {
            //var freqOld = FrequencyKHz;
            var freq = FrequencyKHz += upOrDown ? FrequencyKHzSliderStep : -FrequencyKHzSliderStep;

            if (ValidFrequency(freq))
            {
                FrequencyKHz = freq;
            }
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

        public void RoundFrequency()
        {
            if (!ValidFrequency(FrequencyKHz))
                return;

            // rounding to start freq 474 MHZ
            var startFreq = 474000;

            var stepFreq = Math.Round(Convert.ToDecimal(FrequencyKHz - startFreq) / Convert.ToDecimal(FrequencyKHzSliderStep));

            var freqRounded = Convert.ToInt64(startFreq + stepFreq * FrequencyKHzSliderStep);
            if (freqRounded > MaxFrequencyKHz)
            {
                freqRounded = MaxFrequencyKHz;
            }
            if (freqRounded < MinFrequencyKHz)
            {
                freqRounded = MinFrequencyKHz;
            }

            FrequencyKHz = freqRounded;
        }

        public long MinFrequencyRoundedMHz
        {
            get
            {
                if (_minFrequencyKHz == 0)
                    return 0;

                return Convert.ToInt32(Math.Round(_minFrequencyKHz / 1000.0));
            }
        }

        public long MaxFrequencyRoundedMHz
        {
            get
            {
                if (_maxFrequencyKHz == 0)
                    return 0;

                return Convert.ToInt32(Math.Round(_maxFrequencyKHz / 1000.0));
            }
        }

        public long MinFrequencyKHz
        {
            get
            {
                return _minFrequencyKHz;
            }
            set
            {
                _minFrequencyKHz = value;

                OnPropertyChanged(nameof(MinFrequencyKHz));
                OnPropertyChanged(nameof(MinFrequencyRoundedMHz));
            }
        }

        public long MaxFrequencyKHz
        {
            get
            {
                return _maxFrequencyKHz;
            }
            set
            {
                _maxFrequencyKHz = value;

                OnPropertyChanged(nameof(MaxFrequencyKHz));
                OnPropertyChanged(nameof(MaxFrequencyRoundedMHz));
            }
        }

        public long FrequencyKHz
        {
            get
            {
                return _frequencyKHz;
            }
            set
            {
                _frequencyKHz = value;

                if (!ValidFrequency(value))
                    return;

                OnPropertyChanged(nameof(FrequencyKHz));
                OnPropertyChanged(nameof(FrequencyMHz));
                OnPropertyChanged(nameof(FrequencyMHzAsString));
                OnPropertyChanged(nameof(FrequencyDecimalPartMHzCaption));
                OnPropertyChanged(nameof(FrequencyWholePartMHz));
            }
        }

        public double FrequencyMHz
        {
            get
            {
                return FrequencyKHz / 1000.0;
            }
            set
            {
                if (double.IsNaN(value) || !ValidFrequency(value * 1000.0))
                    return;

                var freqKHz = Convert.ToInt64(value * 1000.0);

                if (FrequencyKHz != freqKHz)
                {
                    FrequencyKHz = freqKHz;
                }
            }
        }

        public string FrequencyMHzAsString
        {
            get
            {
                return Math.Round(FrequencyKHz / 1000.0, 3).ToString();
            }
            set
            {
                if (!ValidFrequency(value))
                {
                    return;
                }

                var freqKHz = TuneViewModel.ParseFreqMHzToKHz(value);

                if (FrequencyKHz != freqKHz)
                {
                    FrequencyKHz = freqKHz;
                }
            }
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

        public bool ValidFrequency(long freqKHz)
        {

            if (freqKHz < _minFrequencyKHz || freqKHz > _maxFrequencyKHz)
            {
                return false;
            }

            return true;
        }

        public bool ValidFrequency(double freqKHz)
        {
            return ValidFrequency(Convert.ToInt64(freqKHz));
        }

        public bool ValidFrequency(string freqMHz)
        {
            var freqKHz = TuneViewModel.ParseFreqMHzToKHz(freqMHz);
            if (freqKHz == -1)
            {
                return false;
            }

            return ValidFrequency(freqKHz);
        }
    }
}
