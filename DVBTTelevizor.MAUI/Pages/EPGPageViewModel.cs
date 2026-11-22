using CommunityToolkit.Mvvm.Input;
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

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    _eventItems?.Clear();

                    var nextEvent = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 0).AddHours(-1);

                    for (var i = 0; i < 10; i++)
                    {
                        _eventItems?.Add(new EventItem()
                        {
                            EventName = $"Event {i}",
                            Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.",
                            StartTime = nextEvent.AddHours(i * 10),
                            FinishTime = nextEvent.AddHours((i+1) * 10)
                        });
                    }

                    // Auto-select first item if nothing is currently selected
                    if (SelectedItem == null && _eventItems?.Count > 0)
                    {
                        SelectedItem = _eventItems[0];
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

