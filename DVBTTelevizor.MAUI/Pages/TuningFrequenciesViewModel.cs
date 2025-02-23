using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using LoggerService;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI
{
    public class TuningFrequenciesViewModel : BaseViewModel
    {
        private bool _dvbt = true;
        private bool _dvbt2 = true;
        private long _tuneBandWidthKHz = 8000;

        private long _frequencyFromKHz = 474000;
        private long _frequencyToKHz = 852000;

        public TuningFrequenciesViewModel(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration, IDialogService dialogService, IPublicDirectoryProvider publicDirectoryProvider)
          : base(loggingService, driver, tvConfiguration, dialogService, publicDirectoryProvider)
        {
            WeakReferenceMessenger.Default.Register<FontSizeChangedMessage>(this, (r, m) =>
            {
                _loggingService.Info($"TuningFrequenciesViewModel: FontSizeChanged");
                NotifyFontSizeChange();
            });
        }

        public long FrequencyFromKHz
        {
            get
            {
                return _frequencyFromKHz;
            }
            set
            {
                _frequencyFromKHz = value;
                OnPropertyChanged(nameof(FrequencyFromKHz));
            }
        }

        public long FrequencyToKHz
        {
            get
            {
                return _frequencyToKHz;
            }
            set
            {
                _frequencyToKHz = value;
                OnPropertyChanged(nameof(FrequencyToKHz));
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
            }
        }

        public bool DVBT
        {
            get
            {
                return _dvbt;
            }
            set
            {
                _dvbt = value;
                OnPropertyChanged(nameof(DVBT));
            }
        }

        public bool DVBT2
        {
            get
            {
                return _dvbt2;
            }
            set
            {
                _dvbt2 = value;
                OnPropertyChanged(nameof(DVBT2));
            }
        }

        public long FrequencyFromWholePartMHz
        {
            get
            {
                return Convert.ToInt64(Math.Floor(FrequencyFromKHz / 1000.0));
            }
        }

        public string FrequencyFromDecimalPartMHzCaption
        {
            get
            {
                var part = (FrequencyFromKHz / 1000.0) - FrequencyFromWholePartMHz;
                var part1000 = Convert.ToInt64(part * 1000).ToString().PadLeft(3, '0');
                return $".{part1000}";
            }
        }

        public long FrequencyToWholePartMHz
        {
            get
            {
                return Convert.ToInt64(Math.Floor(FrequencyToKHz / 1000.0));
            }
        }

        public string FrequencyToDecimalPartMHzCaption
        {
            get
            {
                var part = (FrequencyToKHz / 1000.0) - FrequencyToWholePartMHz;
                var part1000 = Convert.ToInt64(part * 1000).ToString().PadLeft(3, '0');
                return $".{part1000}";
            }
        }
    }
}

