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
        private ListViewSelector? _channelsListViewSelector = null;

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

        public void SelectNextChannel()
        {
            _eventItems?.Clear();
            _channelsListViewSelector?.SelectNextItem();
            Channel = _channelsListViewSelector?.GetSelectedItem() as Channel;
        }

        public void SelectPreviousChannel()
        {
            _eventItems?.Clear();
            _channelsListViewSelector?.SelectPreviousItem();
            Channel = _channelsListViewSelector?.GetSelectedItem() as Channel;
        }

        public void SelectNextItem()
        {
            _eventItemsListViewSelector?.SelectNextItem();
        }

        public void SelectItem(EPGEventItem item)
        {
            _eventItemsListViewSelector?.SetSelectedItem(item);
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

        public async Task RefreshOnlineEvents()
        {
            if (IPTV != null)
            {
                var iptvEvents = await IPTV.GetEPG(24 * 60);   // 24*60

                if (iptvEvents != null && iptvEvents.Count > 0)
                {
                    await EIT?.AddOnlineEvents(iptvEvents, Channels);
                }
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

                var events = EIT?.GetEvents(
                        DateTime.Now,
                        _channel.UniqueIdentifier,
                        100) ?? new List<EventItem>();


                await MainThread.InvokeOnMainThreadAsync(() =>
                {
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
                _channelsListViewSelector = new ListViewSelector(new ObservableCollection<IListViewSelector>(_channels?.Cast<IListViewSelector>() ?? Enumerable.Empty<IListViewSelector>()));
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
                OnPropertyChanged(nameof(LeftButtonIcon));
                OnPropertyChanged(nameof(RightButtonIcon));
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

        public string LeftButtonIcon
        {
            get
            {
                return _activeItem == EPGPageActiveItemEnum.LeftButton ? "leftselected.png" : "left.png";
            }
        }

        public string RightButtonIcon
        {
            get
            {
                return _activeItem == EPGPageActiveItemEnum.RightButton ? "rightselected.png" : "right.png";
            }
        }

        public EPGPageActiveItemEnum ActiveItem { get => _activeItem; set => _activeItem = value; }
    }
}

