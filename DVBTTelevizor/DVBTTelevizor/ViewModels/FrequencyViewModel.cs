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

        public long DefaultFrequencyKHz { get; set; }
        public long FrequencyKHzSliderStep { get; set; } = 1000;

        public Command SetDefaultFrequencyCommand { get; set; }

        public FrequencyViewModel(ILoggingService loggingService, IDialogService dialogService, IDVBTDriverManager driver, DVBTTelevizorConfiguration config)
         : base(loggingService, dialogService, driver, config)
        {
            SetDefaultFrequencyCommand = new Command(() => { FrequencyKHz = DefaultFrequencyKHz; });
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

            var stepFreq = Math.Round(Convert.ToDecimal(FrequencyKHz - MinFrequencyKHz) / Convert.ToDecimal(FrequencyKHzSliderStep));
            var freqRounded = Convert.ToInt64(MinFrequencyKHz + stepFreq * FrequencyKHzSliderStep);
            if (freqRounded > MaxFrequencyKHz)
            {
                freqRounded = freqRounded - FrequencyKHzSliderStep;
            }

            FrequencyKHz = freqRounded;
        }

        public string MinFrequencyMHz
        {
            get
            {
                if (_minFrequencyKHz == 0)
                    return "Min: 0 MHz";

                return $"Min: {(_minFrequencyKHz / 1000.0).ToString("N0")} MHz";
            }
        }

        public string MaxFrequencyMHz
        {
            get
            {
                if (_maxFrequencyKHz == 0)
                    return "0 MHz";

                return $"Max: {(_maxFrequencyKHz / 1000.0).ToString("N0")} MHz";
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
                OnPropertyChanged(nameof(MaxFrequencyMHz));
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
