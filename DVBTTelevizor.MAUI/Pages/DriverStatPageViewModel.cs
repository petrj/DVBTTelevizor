using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using LoggerService;
using Plugin.InAppBilling;
using RTLSDR.Common;
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
        public ObservableCollection<StatValue> Stats { get; } = new();

        public DriverStatPageViewModel(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration, IPublicDirectoryProvider publicDirectoryProvider)
          : base(loggingService, driver, tvConfiguration, publicDirectoryProvider)
        {
            WeakReferenceMessenger.Default.Register<DriverUpdateStatMessage>(this, (r, m) =>
            {
                Task.Run(async () =>
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        FillStat(m?.Value?.StatValues);
                    });
                });
            });
        }

        private void FillStat(List<StatValue>? values)
        {
            try
            {
                Stats.Clear();

                if (values == null || values.Count == 0)
                {
                    return;
                }

                foreach (var item in values)
                {
                    Stats.Add(item);
                }
            } finally
            {
                OnPropertyChanged(nameof(Stats));
            }
        }
    }

}

