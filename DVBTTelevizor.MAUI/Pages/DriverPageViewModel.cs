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
        private DriverState? _driverState = null;
        private DriverTypeEnum? _pageDriver = null;

        public DriverPageViewModel(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration, IPublicDirectoryProvider publicDirectoryProvider)
          : base(loggingService, driver, tvConfiguration, publicDirectoryProvider)
        {
            WeakReferenceMessenger.Default.Register<DriverUpdateStateMessage>(this, (r, m) =>
            {
                _driverState = m.Value;
                NotifyChange();
            });

            WeakReferenceMessenger.Default.Register<DVBTDriverStateChangedMessages>(this, (r, m) =>
            {
                Task.Run(async () => CheckDriver());
            });
        }

        public async Task CheckDriver()
        {
            try
            {
                _range = string.Empty;

                if (_driver == null || !_driver.Connected)
                    return;

                var cap = await _driver.GetCapabalities();

                // setting min/max frequencies from device
                if (cap.SuccessFlag)
                {
                    _range = $"{Convert.ToDouble(cap.minFrequency / 1E+6).ToString("N1")} - {Convert.ToDouble(cap.maxFrequency / 1E+6).ToString("N1")}";
                }
            }
            finally
            {
                NotifyChange();
            }
        }


        public DriverTypeEnum? PageDriver
        {
            get
            {
                return _pageDriver;
            }
            set
            {
                _pageDriver = value;

                NotifyChange();
            }
        }

        public string PageDriverTitle
        {
            get
            {
                switch (_pageDriver)
                {
                    case DriverTypeEnum.AndroidDVBTDriver:
                    case DriverTypeEnum.AndroidTestingDVBTDriver:
                    case DriverTypeEnum.TestTuneDriver:
                        return "DVB-T Driver".Translated();
                    case DriverTypeEnum.RTLSDRDriverDAB:
                    case DriverTypeEnum.RTLSDRDriverFM:
                        return "SDR Driver".Translated();
                    default:
                        return "Driver".Translated();
                }
            }
        }

        public void NotifyChange()
        {
            //MainThread.BeginInvokeOnMainThread(async () =>
            //{
            OnPropertyChanged(nameof(PageDriver));
            OnPropertyChanged(nameof(PageDriverTitle));
            OnPropertyChanged(nameof(DriverIconImage));
            OnPropertyChanged(nameof(LastTuneFrequency));
            OnPropertyChanged(nameof(ConnectedDeviceVisible));
            OnPropertyChanged(nameof(StatusTitle));
            OnPropertyChanged(nameof(InstallDriverButtonVisible));
            OnPropertyChanged(nameof(DisconnectButtonVisible));
            OnPropertyChanged(nameof(ConnectButtonVisible));
            OnPropertyChanged(nameof(ConnectedDeviceRange));
            OnPropertyChanged(nameof(DriverPreferencesVisible));
            OnPropertyChanged(nameof(Bitrate));
            //});
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
                if (_driver == null || !_driver.DriverInstalled)
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

        private bool SameDriver
        {
            get
            {
                if (_driver == null || _pageDriver == null)
                {
                    return false;
                }

                if (_driver is RTLSDRDABDriverConnector)
                {
                    return
                            _pageDriver == DriverTypeEnum.RTLSDRDriverDAB ||
                            _pageDriver == DriverTypeEnum.RTLSDRDriverFM;
                }

                if (_driver is RTLSDRDriverConnector)
                {
                    return
                            _pageDriver == DriverTypeEnum.AndroidDVBTDriver;
                }

                return false;
            }
        }

        public string StatusTitle
        {
            get
            {
                if (_driver == null)
                {
                    return "No driver".Translated();
                }

                if (!SameDriver)
                {
                    return "Not connected".Translated();
                }

                if ( !_driver.DriverInstalled)
                {
                    return "Driver not installed".Translated();
                }

                if (_driver.Connected)
                {
                    return "Connected".Translated();
                }

                return "Disconnected".Translated();
            }
        }

        public bool DriverPreferencesVisible
        {
            get
            {
                return (_driver != null) && _driver.DriverInstalled;
            }
        }

        public bool ConnectedDeviceVisible
        {
            get
            {
                return (_driver != null) && _driver.DriverInstalled && _driver.Connected;
            }
        }

        public string ConnectedDeviceRange
        {
            get
            {
                return (_driver != null) && _driver.DriverInstalled && _driver.Connected
                    ? _range
                    : String.Empty;
            }
        }



        public bool InstallDriverButtonVisible
        {
            get
            {
                return (_driver == null || !_driver.DriverInstalled);
            }
        }

        public bool DisconnectButtonVisible
        {
            get
            {
                return (_driver != null &&

                    _driver.DriverInstalled && _driver.Connected);
            }
        }

        public bool ConnectButtonVisible
        {
            get
            {
                return (_driver != null &&
                        _driver.DriverInstalled &&
                        !_driver.Connected);
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

