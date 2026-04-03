using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using DVBTTelevizor.TV;
using LoggerService;
using RTLSDR;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI
{
    public class DriverPageViewModel : BaseViewModel
    {
        private string _range = string.Empty;
        private DriverStat? _driverState = null;

        private AppDriverTypeEnum? _pageDriver = null;
        private bool? _dvbtDriverInstalled = null;
        private bool? _rtlsdrDriverInstalled = null;


        public DriverPageViewModel(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration, IPublicDirectoryProvider publicDirectoryProvider)
          : base(loggingService, driver, tvConfiguration, publicDirectoryProvider)
        {
            WeakReferenceMessenger.Default.Register<DriverUpdateStatMessage>(this, (r, m) =>
            {
                _driverState = m.Value;
                NotifyDriverChange();
            });

            //WeakReferenceMessenger.Default.Register<DriverChangedMessages>(this, (r, m) =>
            //{
            //    _driver = m.Value;

            //    NotifyChange();
            //});
        }

        public bool? DvbtDriverInstalled
        {
            get => _dvbtDriverInstalled;
            set
            {
                _dvbtDriverInstalled = value;
                NotifyDriverChange();
            }
        }
        public bool? RtlsdrDriverInstalled
        {
            get => _rtlsdrDriverInstalled;
            set
            {
                _rtlsdrDriverInstalled = value;
                NotifyDriverChange();
            }
        }

        //public async Task CheckDriver()
        //{
        //    try
        //    {
        //        _range = string.Empty;

        //        if (_driver == null || !_driver.Connected)
        //            return;

        //        var cap = await _driver.GetCapabalities();

        //        // setting min/max frequencies from device
        //        if (cap.SuccessFlag)
        //        {
        //            _range = $"{Convert.ToDouble(cap.minFrequency / 1E+6).ToString("N1")} - {Convert.ToDouble(cap.maxFrequency / 1E+6).ToString("N1")}";
        //        }
        //    }
        //    finally
        //    {
        //        NotifyChange();
        //    }
        //}


        public AppDriverTypeEnum? PageDriver
        {
            get
            {
                return _pageDriver;
            }
            set
            {
                _pageDriver = value;

                NotifyDriverChange();
            }
        }

        public string PageDriverTitle
        {
            get
            {
                switch (_pageDriver)
                {
                    case AppDriverTypeEnum.DVBT:
                        return "DVB-T".Translated();
                    case AppDriverTypeEnum.DAB:
                        return "DAB".Translated();
                    case AppDriverTypeEnum.FM:
                        return "FM".Translated();
                    default:
                        return "Driver".Translated();
                }
            }
        }

        public override void NotifyDriverChange()
        {
            _loggingService.Info($"DriverPageViewModel: NotifyDriverChange");

            base.NotifyDriverChange();

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                OnPropertyChanged(nameof(PageDriver));
                OnPropertyChanged(nameof(PageDriverTitle));
                OnPropertyChanged(nameof(ConnectButtonVisible));
                OnPropertyChanged(nameof(DriverPreferencesVisible));

                OnPropertyChanged(nameof(InstallDriverButtonVisible));
                OnPropertyChanged(nameof(DriverIconImage));
                OnPropertyChanged(nameof(LastTuneFrequency));
                OnPropertyChanged(nameof(ConnectedDeviceVisible));
                OnPropertyChanged(nameof(StatusTitle));

                OnPropertyChanged(nameof(DisconnectButtonVisible));
                OnPropertyChanged(nameof(ConnectedDeviceRange));

                OnPropertyChanged(nameof(Bitrate));
            });
        }


        public string LastTuneFrequency
        {
            get
            {
                if (_driverState == null)
                {
                    return String.Empty;
                }

                return _driverState.Frequency;
            }
        }

        public string DriverIconImage
        {
            get
            {
                if (_driver == null)
                {
                    return "donglered.png";
                }


                if (_driver.Connected)
                {
                    return "donglegreen.png";

                }

                return "dongleorange.png";
            }
        }


        public string StatusTitle
        {
            get
            {
                if (_driver == null || _pageDriver == null)
                {
                    return "No driver".Translated(); // page is not yet initialized
                }

                if (!IsDriverInstalled(_pageDriver.Value))
                {
                    return "Driver not installed".Translated();
                }

                if (_driver.DriverType == _pageDriver.Value)
                {
                    // same driver
                    if (_driver.Connected)
                    {
                        return "Connected".Translated();
                    }
                    else
                    {
                        return "Disconnected".Translated();
                    }
                }
                else
                {
                    // different driver
                    return "Not connected".Translated();
                }
            }
        }

        public bool DriverPreferencesVisible
        {
            get
            {
                if (_pageDriver == null)
                {
                    return false; // page is not yet initialized
                }

                return IsDriverInstalled(_pageDriver.Value);
            }
        }

        public bool ConnectedDeviceVisible
        {
            get
            {
                return (_driver != null) && _driver.Connected;
            }
        }

        public string ConnectedDeviceRange
        {
            get
            {
                return (_driver != null) && _driver.Connected
                    ? _range
                    : String.Empty;
            }
        }

        public bool DisconnectButtonVisible
        {
            get
            {
                if (_driver == null || _pageDriver == null)
                {
                    return false; // page is not yet initialized
                }

                if (!IsDriverInstalled(_pageDriver.Value))
                {
                    return false; // driver not installed
                }

                if (_driver.DriverType == _pageDriver.Value)
                {
                    // same driver, show button if connected
                    return _driver.Connected;
                }
                else
                {
                    // different driver
                    return false;
                }
            }
        }

        private bool IsDriverInstalled(AppDriverTypeEnum appDriverType)
        {
            if (_driver == null)
            {
                return false;
            }

            if (appDriverType == AppDriverTypeEnum.DVBT)
            {
                return DvbtDriverInstalled.HasValue && DvbtDriverInstalled.Value;
            }

            if ((appDriverType == AppDriverTypeEnum.FM) || (appDriverType == AppDriverTypeEnum.DAB))
            {
                return RtlsdrDriverInstalled.HasValue && RtlsdrDriverInstalled.Value;
            }

            return false;
        }

        public bool ConnectButtonVisible
        {
            get
            {
                if (_driver == null || _pageDriver == null)
                {
                    return false; // page is not yet initialized
                }

                if (!IsDriverInstalled(_pageDriver.Value))
                {
                    return false; // driver not installed
                }

                if (_driver.DriverType == _pageDriver.Value)
                {
                    // same driver, show connect button if not connected
                    return !_driver.Connected;
                } else
                {
                    // different driver
                    return true;// show connect button to allow user to switch driver
                }
            }
        }

        public bool InstallDriverButtonVisible
        {
            get
            {
                if (_pageDriver == null)
                {
                    return false; // page is not yet initialized
                }

                return !IsDriverInstalled(_pageDriver.Value);
            }
        }

        public string Bitrate
        {
            get
            {
                if (_driverState == null)
                {
                    return String.Empty;
                }

                return _driverState.BitRate;
            }
        }
    }
}

