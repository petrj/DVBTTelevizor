using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using LibVLCSharp.Shared;
using LoggerService;
using Microsoft.Maui;
using MPEGTS;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI
{
    public class EPGPageViewModel : BaseViewModel
    {
        private Channel? _channel = null;

        private ObservableCollection<Channel>? _channels { get; set; } = new ObservableCollection<Channel>();
        private ObservableCollection<EventItem>? _eventItems { get; set; } = new ObservableCollection<EventItem>();


        public EPGPageViewModel(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration, IPublicDirectoryProvider publicDirectoryProvider)
          : base(loggingService, driver, tvConfiguration, publicDirectoryProvider)
        {
        }
        public async Task Refresh()
        {
            try
            {
                if (_channel == null)
                {
                    return;
                }

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    _eventItems?.Clear();

                    for (var i = 1; i < 10; i++)
                    {
                        _eventItems?.Add(new EventItem()
                        {
                            EventName = $"Event {i}",
                            StartTime = DateTime.Now.AddMinutes(i * 10)
                        });
                    }
                });

                await NotifyChange();
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, $"EPGPageViewModel.Refresh() failed");
            }
        }

        public Channel? Channel
        {
            get
            {
                return _channel;
            }
            set
            {
                _channel = value;
                //NotifyChange();
            }
        }

        public ObservableCollection<Channel>? Channels
        {
            get
            {
                return _channels;
            }
            set
            {
                _channels = value;
                //NotifyChange();
            }
        }

        public ObservableCollection<EventItem>? EventItems
        {
            get
            {
                return _eventItems;
            }
            set
            {
                _eventItems = value;
                //NotifyChange();
            }
        }


        public async Task NotifyChange()
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                OnPropertyChanged(nameof(Channel));
                OnPropertyChanged(nameof(Channels));
                OnPropertyChanged(nameof(EventItems));
            });
        }
    }
}

