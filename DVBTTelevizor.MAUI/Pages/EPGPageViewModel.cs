using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.DBManager;
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
using System.Windows.Input;

namespace DVBTTelevizor.MAUI
{
    public class EPGPageViewModel : BaseViewModel
    {
        private Channel? _channel = null;
        private bool _isRefreshing = false;
        private EventItem? _selectedItem = null;

        private ObservableCollection<Channel>? _channels { get; set; } = new ObservableCollection<Channel>();
        private ObservableCollection<EventItem>? _eventItems { get; set; } = new ObservableCollection<EventItem>();

        private EITManager? _eitManager { get; set; } = null;
        private SledovaniTV.SledovaniTV? _iptv { get; set; } = null;

        public SledovaniTV.SledovaniTV? IPTV
        {
            get => _iptv;
            set
            {
                _iptv = value;
            }
        }

        public EITManager? EIT
        {
            get => _eitManager;
            set
            {
                _eitManager = value;
            }
        }

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set
            {
                if (_isRefreshing != value)
                {
                    _isRefreshing = value;
                    OnPropertyChanged(nameof(IsRefreshing));
                }
            }
        }

        public EventItem? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    OnPropertyChanged(nameof(SelectedItem));
                }
            }
        }

        public IAsyncRelayCommand RefreshCommand { get; }
        public RelayCommand<EventItem> SelectionChangedCommand { get; }

        public EPGPageViewModel(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration, IPublicDirectoryProvider publicDirectoryProvider)
          : base(loggingService, driver, tvConfiguration, publicDirectoryProvider)
        {
            RefreshCommand = new AsyncRelayCommand(RefreshAsync);
            SelectionChangedCommand = new RelayCommand<EventItem>(OnItemSelected);
        }

        private async Task RefreshAsync()
        {
            IsRefreshing = true;
            try
            {
                await Refresh();
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        private async Task Refresh()
        {
            try
            {
                if (_channel == null)
                {
                    return;
                }

                List<EventItem> events;

                if (_channel.ChannelType == ChannelTypeEnum.SledovaniTV)
                {
                    events = new List<EventItem>();

                    if (IPTV != null && !string.IsNullOrWhiteSpace(_channel.ChannelId))
                    {
                        // TODO cahce EPG data for a channel to avoid unnecessary network requests
                        //
                        var iptvEvents = await IPTV.GetEPG();   // 24*60

                        //EIT?.AddOnlineEvents(iptvEvents);

                        if (iptvEvents.TryGetValue(_channel.ChannelId, out var channelEvents))
                        {
                            events = channelEvents
                                .OrderBy(eventItem => eventItem.StartTime)
                                .Take(100)
                                .ToList();
                        }
                    }
                }
                else
                {
                    events = EIT?.GetEvents(
                        DateTime.Now,
                        _channel.Frequency,
                        _channel.ProgramMapPID,
                        100) ?? new List<EventItem>();
                }

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    SelectedItem = null;
                    _eventItems?.Clear();

                    foreach (var eventItem in events)
                    {
                        _eventItems?.Add(eventItem);
                    }

                    // Auto-select first item if nothing is currently selected
                    if (SelectedItem == null && _eventItems?.Count > 0)
                    {
                        //SelectedItem = _eventItems[0];
                    }
                });

                await NotifyChange();
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, $"EPGPageViewModel.Refresh() failed");
            }
        }

        public string Icon
        {
            get
            {
                return "dvbttelevizor.png";
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
                OnPropertyChanged(nameof(Channel));
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
                OnPropertyChanged(nameof(SelectedItem));
            });
        }

        private void OnItemSelected(EventItem? item)
        {
            if (item == null)
                return;

            _loggingService.Debug($"Item selected: {item.EventName}");
            // TODO: Handle selected item tap here
        }
    }
}

