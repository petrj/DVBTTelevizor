using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.DBManager;
using DVBTTelevizor.MAUI.Messages;
using DVBTTelevizor.TV;
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
        private ObservableCollection<EPGEventItem> _eventItems { get; set; } = new ObservableCollection<EPGEventItem>();

        private ListViewSelector? _eventItemsListViewSelector = null;

        private EITManager? _eitManager { get; set; } = null;
        private SledovaniTV.SledovaniTV? _iptv { get; set; } = null;

        private EPGPageActiveItemEnum _activeItem = EPGPageActiveItemEnum.List;

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

        public EPGEventItem? SelectedItem
        {
            get
            {
                foreach (var epgEventItem in _eventItems ?? Enumerable.Empty<EPGEventItem>())
                {
                    if (epgEventItem.Selected)
                    {
                        return epgEventItem;
                    }
                }

                return null;
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

        public void SelectNextItem()
        {
            _eventItemsListViewSelector?.SelectNextItem();
        }


        public void SelectPreviousItem()
        {
            _eventItemsListViewSelector?.SelectPreviousItem();
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
                    var selectedItemUniqueIdentifier = SelectedItem?.UniqueIdentifier;

                    var selectedItem = SelectedItem;
                    var selectFirst = false;
                    if (selectedItem == null && EventItems?.Count > 0)
                    {
                        selectedItem = EventItems?.FirstOrDefault();
                    }
                    if (selectedItem == null)
                    {
                        selectFirst = true;
                    }
                    var selectedItemCSV = selectedItem?.CSV;
                    var selectedItemFocused = selectedItem?.Focused ?? false;
                    _eventItems?.Clear();

                    foreach (var eventItem in events)
                    {
                        var it = new EPGEventItem(eventItem);
                        if (selectFirst || !string.IsNullOrEmpty(selectedItemCSV) && it.CSV == selectedItemCSV)
                        {
                            selectFirst = false;
                            it.Selected = true;
                            it.Focused = selectedItemFocused;
                        }
                        _eventItems?.Add(it);

                        MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            it.NotifyChanges();
                        });
                    }

                });

                _eventItemsListViewSelector = new ListViewSelector(new ObservableCollection<IListViewSelector>(_eventItems.Cast<IListViewSelector>()));

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

        public ObservableCollection<EPGEventItem> EventItems
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
                OnPropertyChanged(nameof(EPGLabelBackgroundColor));
                OnPropertyChanged(nameof(EPGLabelColor));
            });
        }

        private void OnItemSelected(EventItem? item)
        {
            if (item == null)
                return;

            _loggingService.Debug($"Item selected: {item.EventName}");
            // TODO: Handle selected item tap here
        }

        public string EPGLabelBackgroundColor
        {
            get
            {
                switch (_activeItem)
                {
                    case EPGPageActiveItemEnum.EPG:
                        return "#007cd2";
                    default:
                        return "Transparent";
                }
            }
        }

        public string EPGLabelColor
        {
            get
            {
                switch (_activeItem)
                {
                    case EPGPageActiveItemEnum.EPG:
                        return "White";
                    default:
                        return "#31a3cf";
                }
                //return "#29242a";
            }
        }

        public EPGPageActiveItemEnum ActiveItem { get => _activeItem; set => _activeItem = value; }
    }
}

