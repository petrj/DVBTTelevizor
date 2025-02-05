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
            WeakReferenceMessenger.Default.Register<DVBTDriverConnectedMessage>(this, (r, m) =>
            {
                NotifyChange();
            });

            WeakReferenceMessenger.Default.Register<DVBTDriverConnectionFailedMessage>(this, (r, m) =>
            {
                NotifyChange();
            });

            WeakReferenceMessenger.Default.Register<DVBTDriverNotInstalledMessage>(this, (r, m) =>
            {
                NotifyChange();
            });
        }

        private void NotifyChange()
        {
            OnPropertyChanged(nameof(ConnectedDevice));
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
    }
}

