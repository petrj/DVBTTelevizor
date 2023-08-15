using LoggerService;
using Org.Xmlpull.V1.Sax2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace DVBTTelevizor
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class TuningPage : ContentPage, IOnKeyDown
    {
        private TuneViewModel _viewModel;
        private bool _listViewSelected = false;
        protected ILoggingService _loggingService;
        protected IDialogService _dialogService;
        protected IDVBTDriverManager _driver;
        protected DVBTTelevizorConfiguration _config;

        private KeyboardFocusableItemList _focusItems;

        private Size _lastAllocatedSize = new Size(-1, -1);
        private bool _isPortrait = false;

        public TuningPage(ILoggingService loggingService, IDialogService dialogService, IDVBTDriverManager driver, DVBTTelevizorConfiguration config, ChannelService channelService)
        {
            InitializeComponent();

            _loggingService = loggingService;
            _dialogService = dialogService;
            _driver = driver;
            _config = config;

            BindingContext = _viewModel = new TuneViewModel(_loggingService, _dialogService, _driver, _config, channelService);

            ChannelsListView.ItemSelected += ChannelsListView_ItemSelected;

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_CloseTuningPage, (message) =>
            {
                Device.BeginInvokeOnMainThread(delegate
                {
                   Navigation.PopAsync();
                });
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_UpdateTuningPageFocus, (name) =>
            {
                _focusItems.FocusItem(name);
            });

            //NavigationPage.SetHasBackButton(this, false);

            BuildFocusableItems();

            ChannelsListView.ItemAppearing += ChannelsListView_ItemAppearing;
            ChannelsListView.ItemSelected += ChannelsListView_ItemSelected;
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);

            if (_lastAllocatedSize.Width == width &&
                _lastAllocatedSize.Height == height)
            {
                // no size changed
                return;
            }

            _lastAllocatedSize.Width = width;
            _lastAllocatedSize.Height = height;

            if (width > height)
            {
                _isPortrait = false;
            }
            else
            {
                _isPortrait = true;
            }

            RefreshGUI();
        }

        public void RefreshGUI()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                if (_isPortrait)
                {
                    TunningScrollView.SetValue(Grid.ColumnSpanProperty, 3);
                    //TuningStackLayout.SetValue(Grid.RowSpanProperty, 1);
                    VeticalSplitterBoxView.IsVisible = false;
                    ChannelsListView.SetValue(Grid.ColumnSpanProperty, 3);
                    ChannelsListView.SetValue(Grid.ColumnProperty, 0);
                    ChannelsListView.SetValue(Grid.RowProperty, 1);
                    ChannelsListView.SetValue(Grid.RowSpanProperty, 1);
                }
                else
                {
                    TunningScrollView.SetValue(Grid.ColumnSpanProperty, 1);
                    TunningScrollView.SetValue(Grid.RowSpanProperty,2);
                    VeticalSplitterBoxView.IsVisible = true;
                    ChannelsListView.SetValue(Grid.RowProperty, 0);
                    ChannelsListView.SetValue(Grid.ColumnProperty, 2);
                    ChannelsListView.SetValue(Grid.ColumnSpanProperty, 1);
                    ChannelsListView.SetValue(Grid.RowSpanProperty, 2);
                }
            });
        }

        protected override bool OnBackButtonPressed()
        {
            return base.OnBackButtonPressed();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            _viewModel.NotifyFontSizeChange();

            Task.Run(async () =>
            {
                await _viewModel.Tune();
            });
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            if (_viewModel.TuningInProgress)
            {
                _viewModel.AbortTuneCommand.Execute(null);
            }
        }

        public int NewTunedChannelsCount
        {
            get
            {
                return _viewModel.NewTunedChannelsCount;
            }
        }

        public long FrequencyFromKHz
        {
            get { return _viewModel.FrequencyFromKHz; }
            set { _viewModel.FrequencyFromKHz = value; }
        }

        public long FrequencyToKHz
        {
            get { return _viewModel.FrequencyToKHz; }
            set { _viewModel.FrequencyToKHz = value; }
        }

        public long BandWidthKHz
        {
            get { return _viewModel.TuneBandWidthKHz; }
            set { _viewModel.TuneBandWidthKHz = value; }
        }

        public bool DVBTTuning
        {
            get { return _viewModel.DVBTTuning; }
            set { _viewModel.DVBTTuning = value; }
        }

        public bool DVBT2Tuning
        {
            get { return _viewModel.DVBT2Tuning; }
            set { _viewModel.DVBT2Tuning = value; }
        }

        private void BuildFocusableItems()
        {
            _focusItems = new KeyboardFocusableItemList();

            _focusItems
                .AddItem(KeyboardFocusableItem.CreateFrom("ActualTuning", new List<View>() { ActualTuningStateLabel }))
                .AddItem(KeyboardFocusableItem.CreateFrom("AbortButton", new List<View>() { AbortButton }))
                .AddItem(KeyboardFocusableItem.CreateFrom("FinishButton", new List<View>() { FinishButton }));

            _focusItems.OnItemFocusedEvent += ChannelPage_OnItemFocusedEvent;
        }

        private void ChannelPage_OnItemFocusedEvent(KeyboardFocusableItemEventArgs args)
        {
            // scroll to item
            TunningScrollView.ScrollToAsync(0, args.FocusedItem.MaxYPosition, false);
        }

        private void ChannelsListView_ItemAppearing(object sender, ItemVisibilityEventArgs e)
        {
            if (_viewModel.TuningInProgress && !_listViewSelected)
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    ChannelsListView.ScrollTo(e.Item, ScrollToPosition.MakeVisible, false);
                });
            }
        }

        private void ChannelsListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                ChannelsListView.ScrollTo(_viewModel.SelectedChannel, ScrollToPosition.MakeVisible, false);
            });
        }

        private void ActionLeftRight()
        {
            if (_listViewSelected)
            {
                _viewModel.SelectedChannel = null;
                _listViewSelected = false;
                _focusItems.FocusItem(_viewModel.TuningInProgress ? "AbortButton" : "FinishButton");
            } else
            {
                SelectButtonOrListView();
            }
        }

        private void SelectButtonOrListView()
        {
            var fi = _focusItems.FocusedItem;

            if (fi == null)
            {
                _focusItems.FocusItem(_viewModel.TuningInProgress ? "AbortButton" : "FinishButton");
                return;
            }
            else
            {
                if (_focusItems.LastFocusedItemName != "ActualTuning")
                {
                    _listViewSelected = true;
                    _focusItems.DeFocusAll();
                    _viewModel.SelectedChannel = _viewModel.FirstTunedChannel;
                }
                else
                {
                    _focusItems.FocusNextItem();
                }
            }
        }

        private void ActionDown()
        {
            if (_listViewSelected)
            {
                var steps = _viewModel.SelectNextTunedChannel();
                if (steps == 0)
                {
                    _viewModel.SelectedChannel = null;
                    _listViewSelected = false;
                    _focusItems.FocusItem(_viewModel.TuningInProgress ? "AbortButton" : "FinishButton");
                }
            } else
            {
                SelectButtonOrListView();
            }
        }

        private void ActionUp()
        {
            if (_listViewSelected)
            {
                if (_viewModel.SelectedChannel == _viewModel.FirstTunedChannel)
                {
                    _viewModel.SelectedChannel = null;
                    _listViewSelected = false;
                    _focusItems.FocusItem(_viewModel.TuningInProgress ? "AbortButton" : "FinishButton");
                }
                else
                {
                    _viewModel.SelectPreviousChannel();
                }
            } else
            {
                if (_focusItems.LastFocusedItemName == "ActualTuning")
                {
                    _listViewSelected = true;
                    _focusItems.DeFocusAll();
                    _viewModel.SelectedChannel = _viewModel.LastTunedChannel;
                } else
                {
                    _focusItems.FocusPreviousItem();
                }
            }
        }

        public async void OnKeyDown(string key, bool longPress)
        {
            _loggingService.Debug($"TuningPage OnKeyDown {key}");

            var keyAction = KeyboardDeterminer.GetKeyAction(key);

            switch (keyAction)
            {
                case KeyboardNavigationActionEnum.Down:
                    ActionDown();
                    break;

                case KeyboardNavigationActionEnum.Up:
                    ActionUp();
                    break;

                case KeyboardNavigationActionEnum.Right:
                case KeyboardNavigationActionEnum.Left:
                    ActionLeftRight();
                    break;

                case KeyboardNavigationActionEnum.Back:
                    if (_viewModel.TuningInProgress)
                    {
                        if (!await _dialogService.Confirm("Abort tuning?"))
                        {
                            return;
                        }
                    }

                    await Navigation.PopAsync();
                    break;

                case KeyboardNavigationActionEnum.OK:
                        switch (_focusItems.FocusedItemName)
                        {
                            case "AbortButton":
                                _viewModel.AbortTuneCommand.Execute(null);
                                break;

                            case "FinishButton":
                                _viewModel.FinishTuningCommand.Execute(null);
                                break;
                        }
                    break;
            }
        }

        public void OnTextSent(string text)
        {

        }
    }
}