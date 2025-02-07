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
    public class DriverPageViewModel : BaseViewModel
    {
        public DriverPageViewModel(ILoggingService loggingService, IDriverConnector driver, ITVCConfiguration tvConfiguration, IDialogService dialogService, IPublicDirectoryProvider publicDirectoryProvider)
          : base(loggingService, driver, tvConfiguration, dialogService, publicDirectoryProvider)
        {
            WeakReferenceMessenger.Default.Register<DVBTDriverStateChangedMessages>(this, (r, m) =>
            {
                NotifyChange();
            });
        }

        public void NotifyChange()
        {
            OnPropertyChanged(nameof(ConnectedDevice));
            OnPropertyChanged(nameof(DriverIconImage));
            OnPropertyChanged(nameof(Bitrate));
            OnPropertyChanged(nameof(ConnectedDeviceVisible));
            OnPropertyChanged(nameof(DriverStateStatus));
            OnPropertyChanged(nameof(InstallDriverButtonVisible));
            OnPropertyChanged(nameof(DisconnectButtonVisible));
            OnPropertyChanged(nameof(ConnectButtonVisible));
        }

        public string ConnectedDevice
        {
            get
            {
                if (_driver == null || _driver.Configuration == null)
                {
                    return "No device connected".Translated();
                }

                return _driver.Configuration.DeviceName;
            }
        }

        public string Bitrate
        {
            get
            {
                if (_driver == null || _driver.Configuration == null)
                {
                    return String.Empty;
                }

                return DVBTDriverConnector.GetHumanReadableBitRate(_driver.Bitrate);
            }
        }

        public string DriverIconImage
        {
            get
            {
                if (_driver == null || !_driver.DriverInstalled)
                {
                    return "dongleorange.png";
                }


                if (_driver.Connected)
                {
                    return "donglegreen.png";

                }

                return "donglered.png";
            }
        }

        public bool ConnectedDeviceVisible
        {
            get
            {
                return (_driver != null) && _driver.DriverInstalled && (_driver.Connected);
            }
        }

        public string DriverStateStatus
        {
            get
            {
                if (_driver == null || !_driver.DriverInstalled)
                {
                    return "Driver not installed!".Translated();
                }

                if (_driver.Connected)
                {
                    return "Connected".Translated();
                }

                return "Disconnected".Translated();
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
                return (_driver != null && _driver.DriverInstalled && _driver.Connected);
            }
        }

        public bool ConnectButtonVisible
        {
            get
            {
                return (_driver != null && !_driver.Connected);
            }
        }
    }
}

