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
                .AddItem(KeyboardFocusableItem.CreateFrom("AbortButton", new List<View>() { AbortButton }))
                .AddItem(KeyboardFocusableItem.CreateFrom("FinishButton", new List<View>() { FinishButton }));

            _focusItems.OnItemFocusedEvent += _focusItems_OnItemFocusedEvent;
        }

        private void _focusItems_OnItemFocusedEvent(KeyboardFocusableItemEventArgs args)
        {
            Device.BeginInvokeOnMainThread( () =>
            {
                // scroll to item
                if (args.FocusedItem.Name == "FinishButton" || args.FocusedItem.Name == "AbortButton")
                {
                    ChannelsListView.ScrollTo(_viewModel.FirstTunedChannel, ScrollToPosition.End, false);
                }
            });
        }

        private void ChannelsListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            ChannelsListView.ScrollTo(_viewModel.SelectedChannel, ScrollToPosition.MakeVisible, false);
        }

        private void GoDown()
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
                var fi = _focusItems.FocusedItem;

                if (fi == null)
                {
                    _focusItems.FocusItem(_viewModel.TuningInProgress ? "AbortButton" : "FinishButton");
                    return;
                } else
                {
                    _listViewSelected = true;
                    _focusItems.DeFocusAll();
                    _viewModel.SelectedChannel = _viewModel.FirstTunedChannel;
                }
            }
        }

        private void GoUp()
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
                    var steps = _viewModel.SelectPreviousChannel();
                }
            } else
            {
                _listViewSelected = true;
                _focusItems.DeFocusAll();
                _viewModel.SelectedChannel = _viewModel.LastTunedChannel;
            }
        }

        public async void OnKeyDown(string key, bool longPress)
        {
            _loggingService.Debug($"TuningPage OnKeyDown {key}");

            var keyAction = KeyboardDeterminer.GetKeyAction(key);

            switch (keyAction)
            {
                case KeyboardNavigationActionEnum.Down:
                    GoDown();
                    break;

                case KeyboardNavigationActionEnum.Up:
                    GoUp();
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
    }
}