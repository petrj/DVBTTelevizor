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
    public partial class TunePage : ContentPage, IOnKeyDown
    {
        private TunePageViewModel _viewModel;
        protected ILoggingService _loggingService;
        protected IDialogService _dialogService;
        protected IDVBTDriverManager _driver;
        protected DVBTTelevizorConfiguration _config;

        private bool _toolBarFocused = false;

        private KeyboardFocusableItemList _focusItems;
        private string _previousFocusedItemsPart;
        private string _previousFocusedItem = null;

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
                UpdateFocusedPart(name, null);
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_CloseActualPage, (message) =>
            {
                Device.BeginInvokeOnMainThread(delegate
                {
                   Navigation.PopAsync();
                });
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
                .AddItem(KeyboardFocusableItem.CreateFrom("AutoManualTuning", new List<View>() { AutoManualTuningBoxView, AutoManualPicker }))
                .AddItem(KeyboardFocusableItem.CreateFrom("AutomaticTuningOptions", new List<View>() { EditFrequenciesButton,  }))
                //.AddItem(KeyboardFocusableItem.CreateFrom("BandWidtMHz", new List<View>() { AutoFrequencyBoxView }))
                //.AddItem(KeyboardFocusableItem.CreateFrom("Frequency", new List<View>() { FrequencyBoxView, EntryFrequency }))
                //.AddItem(KeyboardFocusableItem.CreateFrom("Channel", new List<View>() { ChannelBoxView, ChannelPicker }))

                .AddItem(KeyboardFocusableItem.CreateFrom("DVBT", new List<View>() { DVBTBoxView, DVBTTuningCheckBox }))
                .AddItem(KeyboardFocusableItem.CreateFrom("DVBT2", new List<View>() { DVBT2BoxView, DVBT2TuningCheckBox }))
                .AddItem(KeyboardFocusableItem.CreateFrom("TuneButton", new List<View>() { TuneButton }));

            /*
            _focusItemsManual
                .AddItem(KeyboardFocusableItem.CreateFrom("ManualTuning", new List<View>() { AutoManualTuningBoxView, AutoManualPicker }))
                .AddItem(KeyboardFocusableItem.CreateFrom("Frequency", new List<View>() { FrequencyBoxView, EntryFrequency }))
                .AddItem(KeyboardFocusableItem.CreateFrom("Channel", new List<View>() { ChannelBoxView, ChannelPicker }))
                .AddItem(KeyboardFocusableItem.CreateFrom("BandWith", new List<View>() { BandWithBoxView, EntryBandWidth }))
                .AddItem(KeyboardFocusableItem.CreateFrom("DVBT", new List<View>() { DVBTBoxView, DVBTTuningCheckBox }))
                .AddItem(KeyboardFocusableItem.CreateFrom("DVBT2", new List<View>() { DVBT2BoxView, DVBT2TuningCheckBox }))
                .AddItem(KeyboardFocusableItem.CreateFrom("TuneButton", new List<View>() { TuneButton }));
            */

            _focusItemsAbort.AddItem(KeyboardFocusableItem.CreateFrom("AbortButton", new List<View>() { AbortTuneButton }));
            _focusItemsDone.AddItem(KeyboardFocusableItem.CreateFrom("FinishButton", new List<View>() { FinishButton }));

            _focusItemsAuto.OnItemFocusedEvent += TunePage_OnItemFocusedEvent;
            _focusItemsManual.OnItemFocusedEvent += TunePage_OnItemFocusedEvent;
            _focusItemsAbort.OnItemFocusedEvent += TunePage_OnItemFocusedEvent;
            _focusItemsDone.OnItemFocusedEvent += TunePage_OnItemFocusedEvent;
        }

        private void EditFrequencies_Clicked(object sender, EventArgs e)
        {
            var automaticTuningParamsPage = new TuningOptionsPage(_loggingService, _dialogService, _driver, _config);
            Navigation.PushAsync(automaticTuningParamsPage);
        }

        private void TunePage_OnItemFocusedEvent(KeyboardFocusableItemEventArgs args)
        {
            _previousFocusedItem = args.FocusedItem.Name;
        }

        private void TunePage_Appearing(object sender, EventArgs e)
        {
            UpdateFocusedPart("AutoTuning", "TuneButton");

            Task.Run(async () =>
            {
                await _viewModel.SetChannelsRange();
            });
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

        private void UpdateFocusedPart(string part, string itemToFocus)
        {
            _previousFocusedItemsPart = part;

            switch (part)
            {
                case "ManualTuning":
                    _focusItems = _focusItemsManual;
                    break;
                case "AutoTuning":
                    _focusItems = _focusItemsAuto;
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

            if (itemToFocus != null)
            {
                _focusItems.FocusItem(itemToFocus);
            } else
            {
                _previousFocusedItem = null;
            }
        }

        private bool ToolBarSelected
        {
            get
            {
                return _toolBarFocused;
            }
            set
            {
                if (value)
                {
                    _viewModel.SelectedToolbarItemName = "ToolbarItemDriver";
                    _focusItems.DeFocusAll();
                }
                else
                {
                    _viewModel.SelectedToolbarItemName = null;
                    UpdateFocusedPart(_previousFocusedItemsPart, _previousFocusedItem);

                    if (_focusItems.FocusedItem == null)
                    {
                        // no item selected
                        switch (_previousFocusedItemsPart)
                        {
                            case "ManualTuning":
                            case "AutoTuning":
                                _focusItems.FocusItem("TuneButton");
                                break;

                            case "AbortButton":
                                _focusItems.FocusItem("AbortButton");
                                break;

                            case "FinishButton":
                                _focusItems.FocusItem("FinishButton");
                                break;
                        }
                    }
                }

                _viewModel.NotifyToolBarChange();
                _toolBarFocused = value;
            }
        }

        public async void OnKeyDown(string key, bool longPress)
        {
            _loggingService.Debug($"TunePage OnKeyDown {key}");

            var keyAction = KeyboardDeterminer.GetKeyAction(key);

            switch (keyAction)
            {
                case KeyboardNavigationActionEnum.Down:
                    if (ToolBarSelected)
                    {
                        ToolBarSelected = false;
                    }
                    else
                    {
                        _focusItems.FocusNextItem();
                    }
                    break;

                case KeyboardNavigationActionEnum.Up:
                    if (ToolBarSelected)
                    {
                        ToolBarSelected = false;
                    }
                    else
                    {
                        _focusItems.FocusPreviousItem();
                    }
                    break;

                case KeyboardNavigationActionEnum.Right:
                case KeyboardNavigationActionEnum.Left:
                    ToolBarSelected = !ToolBarSelected;
                    break;

                case KeyboardNavigationActionEnum.Back:
                    await Navigation.PopAsync();
                    break;

                case KeyboardNavigationActionEnum.OK:
                    if (ToolBarSelected)
                    {
                        ToolConnect_Clicked(this, null);
                    }
                    else
                    {
                        switch (_focusItems.FocusedItemName)
                        {
                            case "AutoManualTuning":
                                //_viewModel.ManualTuning = !_viewModel.ManualTuning;
                                AutoManualPicker.Focus();
                                //UpdateFocusedPart(_viewModel.ManualTuning ? "ManualTuning" : "AutoTuning", "ManualTuning");
                                break;

                            case "AutomaticTuningOptions":
                                //_viewModel.ManualTuning = !_viewModel.ManualTuning;
                                EditFrequencies_Clicked(this, null);
                                //UpdateFocusedPart(_viewModel.ManualTuning ? "ManualTuning" : "AutoTuning", "ManualTuning");
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
                                //ChannelPicker.Focus();
                                break;

                            case "Frequency":
                                //EntryFrequency.Focus();
                                break;

                            case "BandWith":
                                //EntryBandWidth.Focus();
                                break;

                            case "DVBT":
                                DVBTTuningCheckBox.IsToggled = !DVBTTuningCheckBox.IsToggled;
                                break;

                            case "DVBT2":
                                DVBT2TuningCheckBox.IsToggled = !DVBT2TuningCheckBox.IsToggled;
                                break;
                        }
                    }
                    break;
            }
        }
    }
}