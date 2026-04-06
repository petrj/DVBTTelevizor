using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using LoggerService;
using Plugin.InAppBilling;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI
{
    public class DriverStatPageViewModel : BaseViewModel
    {
        private string _stat = string.Empty;

        public DriverStatPageViewModel(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration, IPublicDirectoryProvider publicDirectoryProvider)
          : base(loggingService, driver, tvConfiguration, publicDirectoryProvider)
        {

            WeakReferenceMessenger.Default.Register<DriverUpdateStatMessage>(this, (r, m) =>
            {
                Task.Run(async () =>
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        Stat = m.Value == null ? String.Empty : m.Value.Stat;
                    });
                });
            });
        }

        public string Stat
        {
            get { return _stat; }
            set
            {
                if (_stat != value)
                {
                    _stat = value;
                    OnPropertyChanged(nameof(Stat));
                }
            }
        }
    }

}

