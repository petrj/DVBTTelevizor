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

        public TuningFrequenciesViewModel(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration, IPublicDirectoryProvider publicDirectoryProvider)
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
            OnPropertyChanged(nameof(FrequencyWholePartMHzCaption));
            OnPropertyChanged(nameof(FrequencyDecimalPartMHzCaption));
            OnPropertyChanged(nameof(FrequencyUnit));
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

        public string FrequencyFromWholePartMHz
        {
            get
            {
                var dabFreq = ParseDabFreq((int)(FrequencyFromKHz * 1000));
                if (dabFreq != null)
                {
                    return dabFreq;
                }


                return Convert.ToInt64(Math.Floor(FrequencyFromKHz / 1000.0)).ToString();
            }
        }

        public string FrequencyFromDecimalPartMHzCaption
        {
            get
            {
                var dabFreq = ParseDabFreq((int)(FrequencyFromKHz * 1000));
                if (dabFreq != null)
                {
                    return "";
                }

                var part = (FrequencyFromKHz / 1000.0) - Convert.ToInt64(Math.Floor(FrequencyFromKHz / 1000.0));
                var part1000 = Convert.ToInt64(part * 1000).ToString().PadLeft(3, '0');
                return $".{part1000}";
            }
        }

        public string FrequencyToWholePartMHz
        {
            get
            {
                var dabFreq = ParseDabFreq((int)(FrequencyToKHz * 1000));
                if (dabFreq != null)
                {
                    return dabFreq;
                }

                return Convert.ToInt64(Math.Floor(FrequencyToKHz / 1000.0)).ToString();
            }
        }

        public string FrequencyToDecimalPartMHzCaption
        {
            get
            {
                var dabFreq = ParseDabFreq((int)(FrequencyToKHz * 1000));
                if (dabFreq != null)
                {
                    return "";
                }

                var part = (FrequencyToKHz / 1000.0) - Convert.ToInt64(Math.Floor(FrequencyToKHz / 1000.0));
                var part1000 = Convert.ToInt64(part * 1000).ToString().PadLeft(3, '0');
                return $".{part1000}";
            }
        }

        public static string ParseDabFreq(int freq)
        {
            foreach (var kvp in RTLSDR.Common.AudioTools.DabFrequenciesHz)
            {
                if (kvp.Value == freq)
                {
                    return kvp.Key;
                }
            }
            return null;
        }

        public string FrequencyWholePartMHzCaption
        {
            get
            {
                var dabFreq = ParseDabFreq((int)(FrequencyKHz * 1000));
                if (dabFreq != null)
                {
                    return dabFreq;
                }
                return Convert.ToInt64(Math.Floor(FrequencyKHz / 1000.0)).ToString();
            }
        }

        public string FrequencyDecimalPartMHzCaption
        {
            get
            {
                var dabFreq = ParseDabFreq((int)(FrequencyKHz * 1000));
                if (dabFreq != null)
                {
                    return "";
                }
                var part = (FrequencyKHz / 1000.0) - Convert.ToInt64(Math.Floor(FrequencyKHz / 1000.0));
                var part1000 = Convert.ToInt64(part * 1000).ToString().PadLeft(3, '0');
                return $".{part1000}";
            }
        }

        public string FrequencyUnit
        {
            get
            {
                var dabFreq = ParseDabFreq((int)(FrequencyKHz * 1000));
                if (dabFreq != null)
                {
                    return "";
                }
                return $"MHz".Translated();
            }
        }
    }
}

