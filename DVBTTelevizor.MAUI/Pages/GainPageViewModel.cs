using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using DVBTTelevizor.TV;
using LoggerService;
using RTLSDR.Common;

namespace DVBTTelevizor.MAUI
{
    public class GainPageViewModel : BaseViewModel
    {
        public bool NotifyEnabled { get; set; } = true;
        private bool _isReadonly = false;
        private IDriverConnector _driver;
        private ITVConfiguration _tvConfiguration;

        public GainPageViewModel(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration, IPublicDirectoryProvider publicDirectoryProvider)
          : base(loggingService, driver, tvConfiguration, publicDirectoryProvider)
        {
            _driver = driver;
            _tvConfiguration = tvConfiguration;
        }

        public void NotifyChange()
        {
            //_loggingService.Debug("NotifyChange");

            if (!NotifyEnabled)
            {
                _loggingService.Debug("NotifyChange disabled");
                //return; On some android is KHzEntry focused after Appearing and it disables notify change!
            }

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                //_isReadonly = true;

                OnPropertyChanged(nameof(GainCaption));
                OnPropertyChanged(nameof(GainValue));

                OnPropertyChanged(nameof(GaunUnitCaption));
                OnPropertyChanged(nameof(GainMin));
                OnPropertyChanged(nameof(GainMax));
                OnPropertyChanged(nameof(GainMinTitle));
                OnPropertyChanged(nameof(GainMaxTitle));

                OnPropertyChanged(nameof(ManualGainBoxVisible));
                OnPropertyChanged(nameof(HW));
                OnPropertyChanged(nameof(SWAuto));
                OnPropertyChanged(nameof(Manual));

                //_isReadonly = false;
            });
        }

        public bool ManualGainBoxVisible
        {
            get
            {
                return Manual;
            }
        }

        public string GainCaption
        {
            get
            {
                if (_tvConfiguration == null)
                {
                    return "-";
                }

                var thdb = _tvConfiguration.GainValue;
                return (thdb/10.0).ToString("N1");
            }
        }

        public int GainValue
        {
            get
            {
                if (_tvConfiguration == null)
                {
                    return 0;
                }

                return _tvConfiguration.GainValue;
            }
            set
            {
                if (_isReadonly)
                {
                    return;
                }

                _tvConfiguration?.GainValue = value;
                NotifyChange();
            }
        }


        private void SetGain(GainEnum g)
        {
            if (_isReadonly)
            {
                return;
            }

            try
            {
                _isReadonly = true;

                _tvConfiguration.Gain = g;

                if ((_driver != null) && (_driver.Connected))
                {
                    _driver.SetGain(g, GainValue);
                }

                NotifyChange();
            } finally
            {
                _isReadonly = false;
            }
        }


        public bool HW
        {
            get => _tvConfiguration?.Gain == GainEnum.HW;
            set
            {
                SetGain(GainEnum.HW);
            }
        }

        public bool SWAuto
        {
            get => _tvConfiguration?.Gain == GainEnum.Auto;
            set
            {
                SetGain(GainEnum.Auto);
            }
        }

        public bool Manual
        {
            get => _tvConfiguration?.Gain == GainEnum.Manual;
            set
            {
                SetGain(GainEnum.Manual);
            }
        }

        public string GaunUnitCaption
        {
            get
            {
                return $"dB";
            }
        }

        public int GainMin
        {
            get
            {
                return -100;
            }
        }

        public int GainMax
        {
            get
            {
                return 500;
            }
        }

        public string GainMinTitle
        {
            get
            {
                return "-10 dB";
            }
        }

        public string GainMaxTitle
        {
            get
            {
                return "50 dB";
            }
        }
    }
}

