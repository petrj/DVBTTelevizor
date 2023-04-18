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
    public partial class TuneOptionsPage : ContentPage, IOnKeyDown
    {
        private TuneViewModel _viewModel;
        protected ILoggingService _loggingService;
        protected IDialogService _dialogService;
        protected IDVBTDriverManager _driver;
        protected DVBTTelevizorConfiguration _config;
        protected ChannelService _channelService;

        private bool _toolBarFocused = false;
        private bool _firstAppearing = true;

        private KeyboardFocusableItemList _focusItems;
        private string _previousFocusedItemsPart;
        private string _previousFocusedItem = null;

        private KeyboardFocusableItemList _focusItemsAuto;
        private KeyboardFocusableItemList _focusItemsManual;

        public TuneOptionsPage(ILoggingService loggingService, IDialogService dialogService, IDVBTDriverManager driver, DVBTTelevizorConfiguration config, ChannelService channelService)
        {
            InitializeComponent();

            _loggingService = loggingService;
            _dialogService = dialogService;
            _driver = driver;
            _config = config;
            _channelService = channelService;

            BindingContext = _viewModel = new TuneViewModel(_loggingService, _dialogService, _driver, _config, channelService);

            Appearing += TuneOptionsPage_Appearing;

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

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_UpdateTuneOptionsPageFocus, (name) =>
            {
                UpdateFocusedPart(name, null);
            });

            BuildFocusableItems();
        }

        private void BuildFocusableItems()
        {
            _focusItemsAuto = new KeyboardFocusableItemList();
            _focusItemsManual = new KeyboardFocusableItemList();

            _focusItemsAuto
                .AddItem(KeyboardFocusableItem.CreateFrom("AutoManualTuning", new List<View>() { AutoManualTuningBoxView, AutoManualPicker }))
                .AddItem(KeyboardFocusableItem.CreateFrom("EditBandWidth", new List<View>() { EditBandWidthButton }))
                .AddItem(KeyboardFocusableItem.CreateFrom("EditFrequencyFrom", new List<View>() { EditFrequencyFromButton }))
                .AddItem(KeyboardFocusableItem.CreateFrom("EditFrequencyTo", new List<View>() { EditFrequencyToButton }))
                .AddItem(KeyboardFocusableItem.CreateFrom("DVBT", new List<View>() { DVBTBoxView, DVBTTuningCheckBox }))
                .AddItem(KeyboardFocusableItem.CreateFrom("DVBT2", new List<View>() { DVBT2BoxView, DVBT2TuningCheckBox }))
                .AddItem(KeyboardFocusableItem.CreateFrom("FastTuning", new List<View>() { FastTuningBoxView, FastTuningCheckBox }))
                .AddItem(KeyboardFocusableItem.CreateFrom("TuneButton", new List<View>() { TuneButton }));

            _focusItemsManual
                .AddItem(KeyboardFocusableItem.CreateFrom("AutoManualTuning", new List<View>() { AutoManualTuningBoxView, AutoManualPicker }))
                .AddItem(KeyboardFocusableItem.CreateFrom("EditBandWidth", new List<View>() { EditBandWidthButton }))
                .AddItem(KeyboardFocusableItem.CreateFrom("EditFrequency", new List<View>() { EditFrequencyButton }))
                .AddItem(KeyboardFocusableItem.CreateFrom("DVBT", new List<View>() { DVBTBoxView, DVBTTuningCheckBox }))
                .AddItem(KeyboardFocusableItem.CreateFrom("DVBT2", new List<View>() { DVBT2BoxView, DVBT2TuningCheckBox }))
                .AddItem(KeyboardFocusableItem.CreateFrom("FastTuning", new List<View>() { FastTuningBoxView, FastTuningCheckBox }))
                .AddItem(KeyboardFocusableItem.CreateFrom("TuneButton", new List<View>() { TuneButton }));

            _focusItemsAuto.OnItemFocusedEvent += TuneOptionsPage_OnItemFocusedEvent;
            _focusItemsManual.OnItemFocusedEvent += TuneOptionsPage_OnItemFocusedEvent;
        }

        private void EditFrequencyButton_Clicked(object sender, EventArgs e)
        {
            var freqPage = new FrequencyPage(_loggingService, _dialogService, _driver, _config)
            {
                FrequencyKHz = _viewModel.FrequencyKHz,
                PageTitle = "Tuning frequency",
                MinFrequencyKHz = _viewModel.FrequencyMinKHz,
                MaxFrequencyKHz = _viewModel.FrequencyMaxKHz,
                FrequencyKHzDefault = _viewModel.FrequencyDefaultKHz
            };

            Navigation.PushAsync(freqPage);

            freqPage.Disappearing += delegate
            {
                if (freqPage.Confirmed)
                {
                    _viewModel.FrequencyKHz = freqPage.FrequencyKHz;
                }
            };
        }

        private void EditBandWidthButtton_Clicked(object sender, EventArgs e)
        {
            var bandWidthPage = new BandWidthPage(_loggingService, _dialogService, _driver, _config);
            bandWidthPage.BandWidth = _viewModel.TuneBandWidthKHz;

            Navigation.PushAsync(bandWidthPage);

            bandWidthPage.Disappearing += delegate
            {
                if (bandWidthPage.Confirmed)
                {
                    _viewModel.TuneBandWidthKHz = bandWidthPage.BandWidth;
                }
            };
        }

        private void EditFrequencyFromButtton_Clicked(object sender, EventArgs e)
        {
            var freqPage = new FrequencyPage(_loggingService, _dialogService, _driver, _config)
            {
                FrequencyKHz = _viewModel.FrequencyFromKHz,
                PageTitle = "Tuning frequency from",
                MinFrequencyKHz = _viewModel.FrequencyMinKHz,
                MaxFrequencyKHz = _viewModel.FrequencyMaxKHz,
                FrequencyKHzDefault = _viewModel.FrequencyFromDefaultKHz
            };

            Navigation.PushAsync(freqPage);

            freqPage.Disappearing += delegate
            {
                if (freqPage.Confirmed)
                {
                    _viewModel.FrequencyFromKHz = freqPage.FrequencyKHz;
                }
            };
        }

        private void EditFrequencyToButtton_Clicked(object sender, EventArgs e)
        {
            var freqPage = new FrequencyPage(_loggingService, _dialogService, _driver, _config)
            {
                FrequencyKHz = _viewModel.FrequencyToKHz,
                PageTitle = "Tuning frequency to",
                MinFrequencyKHz = _viewModel.FrequencyMinKHz,
                MaxFrequencyKHz = _viewModel.FrequencyMaxKHz,
                FrequencyKHzDefault = _viewModel.FrequencyToDefaultKHz
            };

            Navigation.PushAsync(freqPage);

            freqPage.Disappearing += delegate
            {
                if (freqPage.Confirmed)
                {
                    _viewModel.FrequencyToKHz = freqPage.FrequencyKHz;
                }
            };
        }

        private void TuneButtton_Clicked(object sender, EventArgs e)
        {
            if (!_driver.Started)
            {
                _dialogService.Error($"Device not connected");
                return;
            }

            var page = new TuningPage(_loggingService, _dialogService, _driver, _config, _channelService)
            {
                BandWidthKHz = _viewModel.TuneBandWidthKHz,
                DVBTTuning = _viewModel.DVBTTuning,
                DVBT2Tuning = _viewModel.DVBT2Tuning
            };

            if (_viewModel.ManualTuning)
            {
                page.FrequencyFromKHz = _viewModel.FrequencyKHz;
                page.FrequencyToKHz = _viewModel.FrequencyKHz;
            } else
            {
                page.FrequencyFromKHz = _viewModel.FrequencyFromKHz;
                page.FrequencyToKHz = _viewModel.FrequencyToKHz;
            }

            Navigation.PushAsync(page);
        }

        private void TuneOptionsPage_OnItemFocusedEvent(KeyboardFocusableItemEventArgs args)
        {
            _previousFocusedItem = args.FocusedItem.Name;

            // scroll to item
            TuneOptionsScrollView.ScrollToAsync(0, args.FocusedItem.MaxYPosition - Height / 2, false);
        }

        private void TuneOptionsPage_Appearing(object sender, EventArgs e)
        {
            if (_firstAppearing)
            {
                _focusItemsAuto.DeFocusAll();
                _focusItemsManual.DeFocusAll();
                UpdateFocusedPart("AutoTuning", "TuneButton");

                Task.Run(async () =>
                {
                    await _viewModel.SetFrequencies();
                });

                _firstAppearing = false;
            }

            _viewModel.NotifyFontSizeChange();
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
                        }
                    }
                }

                _viewModel.NotifyToolBarChange();
                _toolBarFocused = value;
            }
        }

        public async void OnKeyDown(string key, bool longPress)
        {
            _loggingService.Debug($"TuneOptionsPage OnKeyDown {key}");

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
                                _viewModel.ManualTuning = !_viewModel.ManualTuning;
                                AutoManualPicker.Focus();
                                UpdateFocusedPart(_viewModel.ManualTuning ? "ManualTuning" : "AutoTuning", "AutoManualTuning");
                                break;

                            case "EditBandWidth":
                                EditBandWidthButtton_Clicked(this, null);
                                break;

                            case "EditFrequencyFrom":
                                EditFrequencyFromButtton_Clicked(this, null);
                                break;

                            case "EditFrequencyTo":
                                EditFrequencyToButtton_Clicked(this, null);
                                break;

                            case "EditFrequency":
                                EditFrequencyButton_Clicked(this, null);
                                break;

                            case "DVBT":
                                DVBTTuningCheckBox.IsToggled = !DVBTTuningCheckBox.IsToggled;
                                break;

                            case "DVBT2":
                                DVBT2TuningCheckBox.IsToggled = !DVBT2TuningCheckBox.IsToggled;
                                break;

                            case "FastTuning":
                                FastTuningCheckBox.IsToggled = !FastTuningCheckBox.IsToggled;
                                break;

                            case "TuneButton":
                                TuneButtton_Clicked(this, null);
                                break;
                        }
                    }
                    break;
            }
        }
    }
}