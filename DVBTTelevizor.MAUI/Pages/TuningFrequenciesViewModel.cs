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
        public TuningSettings _tuneSettings { get; set; }

        public TuningFrequenciesViewModel(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration, IDialogService dialogService, IPublicDirectoryProvider publicDirectoryProvider)
          : base(loggingService, driver, tvConfiguration, publicDirectoryProvider)
        {
            _tuneSettings = new TuningSettings(loggingService);
        }

        public void NotifyChange()
        {
            OnPropertyChanged(nameof(FrequencyFromKHz));
            OnPropertyChanged(nameof(FrequencyFromKHz));
            OnPropertyChanged(nameof(FrequencyToKHz));
            OnPropertyChanged(nameof(FrequencyKHz));

            OnPropertyChanged(nameof(FrequencyFromWholePartMHz));
            OnPropertyChanged(nameof(FrequencyFromDecimalPartMHzCaption));
            OnPropertyChanged(nameof(FrequencyToWholePartMHz));
            OnPropertyChanged(nameof(FrequencyToDecimalPartMHzCaption));
            OnPropertyChanged(nameof(FrequencyWholePartMHz));
            OnPropertyChanged(nameof(FrequencyDecimalPartMHzCaption));
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

        public long FrequencyFromKHz
        {
            get
            {
                return _tuneSettings.FrequencyFromKHz;
            }
            set
            {
                _tuneSettings.FrequencyFromKHz = value;
                NotifyChange();
            }
        }

        public long FrequencyKHz
        {
            get
            {
                return _tuneSettings.FrequencyKHz;
            }
            set
            {
                _tuneSettings.FrequencyKHz = value;
                NotifyChange();
            }
        }

        public long FrequencyToKHz
        {
            get
            {
                return _tuneSettings.FrequencyToKHz;
            }
            set
            {
                _tuneSettings.FrequencyToKHz = value;
                NotifyChange();
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
                return $".{part1000}";
            }
        }
    }
}

