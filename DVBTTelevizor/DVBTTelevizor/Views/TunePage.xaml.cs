using LoggerService;
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
    public partial class TunePage : ContentPage, IOnKeyDown
    {
        private TunePageViewModel _viewModel;
        protected ILoggingService _loggingService;
        protected IDialogService _dialogService;
        protected IDVBTDriverManager _driver;
        protected DVBTTelevizorConfiguration _config;

        private KeyboardFocusableItemList _focusItems;

        private KeyboardFocusableItemList _focusItemsAuto;
        private KeyboardFocusableItemList _focusItemsManual;
        private KeyboardFocusableItemList _focusItemsAbort;
        private KeyboardFocusableItemList _focusItemsDone;

        public TunePage(ILoggingService loggingService, IDialogService dialogService, IDVBTDriverManager driver, DVBTTelevizorConfiguration config, ChannelService channelService)
        {
            InitializeComponent();

            _loggingService = loggingService;
            _dialogService = dialogService;
            _driver = driver;
            _config = config;

            BindingContext = _viewModel = new TunePageViewModel(_loggingService, _dialogService, _driver, _config, channelService);
            _viewModel.TuneFrequency = "730";

            ChannelsListView.ItemSelected += ChannelsListView_ItemSelected;

            Appearing += TunePage_Appearing;

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_UpdateDriverState, (message) =>
            {
                _viewModel.UpdateDriverState();
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_DVBTDriverConfigurationFailed, (message) =>
            {
                Device.BeginInvokeOnMainThread(delegate
                {
                    _viewModel.UpdateDriverState();
                });
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_UpdateTunePageFocus, (name) =>
            {
                UpdateFocusedPart(name);
            });

            BuildFocusableItems();
        }

        private void BuildFocusableItems()
        {
            _focusItemsAuto = new KeyboardFocusableItemList();
            _focusItemsManual = new KeyboardFocusableItemList();
            _focusItemsAbort = new KeyboardFocusableItemList();
            _focusItemsDone = new KeyboardFocusableItemList();

            _focusItemsAuto
                .AddItem(KeyboardFocusableItem.CreateFrom("ManualTuning", new List<View>() { ManualTuningBoxView, ManualTuningCheckBox }))
                //.AddItem(KeyboardFocusableItem.CreateFrom("Frequency", new List<View>() { FrequencyBoxView, EntryFrequency }))
                //.AddItem(KeyboardFocusableItem.CreateFrom("Channel", new List<View>() { ChannelBoxView, ChannelPicker }))
                .AddItem(KeyboardFocusableItem.CreateFrom("BandWith", new List<View>() { BandWithBoxView, EntryBandWidth }))
                .AddItem(KeyboardFocusableItem.CreateFrom("DVBT", new List<View>() { DVBTBoxView, DVBTTuningCheckBox }))
                .AddItem(KeyboardFocusableItem.CreateFrom("DVBT2", new List<View>() { DVBT2BoxView, DVBT2TuningCheckBox }))
                .AddItem(KeyboardFocusableItem.CreateFrom("TuneButton", new List<View>() { TuneButton }));

            _focusItemsManual
                .AddItem(KeyboardFocusableItem.CreateFrom("ManualTuning", new List<View>() { ManualTuningBoxView, ManualTuningCheckBox }))
                .AddItem(KeyboardFocusableItem.CreateFrom("Frequency", new List<View>() { FrequencyBoxView, EntryFrequency }))
                .AddItem(KeyboardFocusableItem.CreateFrom("Channel", new List<View>() { ChannelBoxView, ChannelPicker }))
                .AddItem(KeyboardFocusableItem.CreateFrom("BandWith", new List<View>() { BandWithBoxView, EntryBandWidth }))
                .AddItem(KeyboardFocusableItem.CreateFrom("DVBT", new List<View>() { DVBTBoxView, DVBTTuningCheckBox }))
                .AddItem(KeyboardFocusableItem.CreateFrom("DVBT2", new List<View>() { DVBT2BoxView, DVBT2TuningCheckBox }))
                .AddItem(KeyboardFocusableItem.CreateFrom("TuneButton", new List<View>() { TuneButton }));

            _focusItemsAbort.AddItem(KeyboardFocusableItem.CreateFrom("AbortButton", new List<View>() { AbortTuneButton }));
            _focusItemsDone.AddItem(KeyboardFocusableItem.CreateFrom("FinishButton", new List<View>() { FinishButton }));

            _focusItems = _focusItemsAuto;
            _focusItems.FocusItem("TuneButton");

            ChannelPicker.Unfocused += delegate
            {
                // loosing focus after Picker item selected
                ChannelPicker.IsVisible = false;
                ChannelPicker.IsVisible = true;
            };
        }

        private void TunePage_Appearing(object sender, EventArgs e)
        {
            _viewModel.NotifyFontSizeChange();
            TuneButton.Focus();
        }

        private async void ToolConnect_Clicked(object sender, EventArgs e)
        {
            if (_driver.Started)
            {
                if (!(await _dialogService.Confirm($"Connected device: {_driver.Configuration.DeviceName}.", $"Device status", "Back", "Disconnect")))
                {
                    await _viewModel.DisconnectDriver();
                }
            }
            else
            {

                if (await _dialogService.Confirm($"Disconnected.", $"Device status", "Connect", "Back"))
                {
                    MessagingCenter.Send("", BaseViewModel.MSG_Init);
                }
            }
        }
        private void ChannelsListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            ChannelsListView.ScrollTo(_viewModel.SelectedChannel, ScrollToPosition.MakeVisible, false);
        }

        private void UpdateFocusedPart(string part)
        {
            switch (part)
            {
                case "ManualTuning":
                    _focusItems = _focusItemsManual;
                    //_focusItems.FocusItem("TuneButton");
                    break;

                case "AutoTuning":
                    _focusItems = _focusItemsAuto;
                    //_focusItems.FocusItem("TuneButton");
                    break;

                case "AbortButton":
                    _focusItems = _focusItemsAbort;
                    _focusItems.FocusItem("AbortButton");
                    break;

                case "FinishButton":
                    _focusItems = _focusItemsDone;
                    _focusItems.FocusItem("FinishButton");
                    break;
            }
        }

        public async void OnKeyDown(string key, bool longPress)
        {
            _loggingService.Debug($"TunePage OnKeyDown {key}");

            if (KeyboardDeterminer.Down(key))
            {
                _focusItems.FocusNextItem();
            }

            if (KeyboardDeterminer.Up(key))
            {
                _focusItems.FocusPreviousItem();
            }

            if (KeyboardDeterminer.OK(key))
            {
                switch (_focusItems.FocusedItemName)
                {
                    case "ManualTuning":
                        _viewModel.ManualTuning = !_viewModel.ManualTuning;
                        UpdateFocusedPart(_viewModel.ManualTuning ? "ManualTuning" : "AutoTuning");
                        break;

                    case "TuneButton":
                        _viewModel.TuneCommand.Execute(null);
                        break;

                    case "AbortButton":
                        _viewModel.AbortTuneCommand.Execute(null);
                        break;

                    case "FinishButton":
                        _viewModel.FinishTunedCommand.Execute(null);
                        break;

                    case "Channel":
                        ChannelPicker.Focus();
                        break;

                    case "Frequency":
                        EntryFrequency.Focus();
                        break;

                    case "BandWith":
                        EntryBandWidth.Focus();
                        break;

                    case "DVBT":
                        DVBTTuningCheckBox.IsChecked = !DVBTTuningCheckBox.IsChecked;
                        break;

                    case "DVBT2":
                        DVBT2TuningCheckBox.IsChecked = !DVBT2TuningCheckBox.IsChecked;
                        break;


                }
            }
        }
    }
}