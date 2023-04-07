using DVBTTelevizor.Models;
using LoggerService;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Xamarin.Forms;

namespace DVBTTelevizor
{
    public class TuneViewModel : BaseViewModel
    {
        protected string _tuneFrequency;
        protected long _tuneBandwidth = 8;

        public const long AutoTuningMinFrequencyKhzDefaultValue = 174000;  // 174.0 MHz - VHF high-band (band III) channel 7
        public const long AutomaticTuningLastChannelDefault = 858000;      // 858.0 MHz - UHF band channel 69

        public long _autoTuningMinFrequencyKhz { get; set; } = AutoTuningMinFrequencyKhzDefaultValue;
        public long _autoTuningMaxFrequencyKhz { get; set; } = AutomaticTuningLastChannelDefault;

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
                return AutoTuningMaxFrequencyKhz;
            }
            set
            {
                AutoTuningMaxFrequencyKhz = value;

                OnPropertyChanged(nameof(AutoTuningMaxFrequencyKhz));
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
                return _tuneBandwidth;
            }
            set
            {
                _tuneBandwidth = value;

                OnPropertyChanged(nameof(TuneBandwidth));
            }
        }
    }
}
