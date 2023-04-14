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

            _focusItemsAuto
                .AddItem(KeyboardFocusableItem.CreateFrom("AutoManualTuning", new List<View>() { AutoManualTuningBoxView, AutoManualPicker }))
                .AddItem(KeyboardFocusableItem.CreateFrom("EditBandWidth", new List<View>() { EditBandWidthButton }))
                .AddItem(KeyboardFocusableItem.CreateFrom("EditFrequencyFrom", new List<View>() { EditFrequencyFromButton }))
                .AddItem(KeyboardFocusableItem.CreateFrom("EditFrequencyTo", new List<View>() { EditFrequencyToButton }))
                .AddItem(KeyboardFocusableItem.CreateFrom("DVBT", new List<View>() { DVBTBoxView, DVBTTuningCheckBox }))
                .AddItem(KeyboardFocusableItem.CreateFrom("DVBT2", new List<View>() { DVBT2BoxView, DVBT2TuningCheckBox }))
                .AddItem(KeyboardFocusableItem.CreateFrom("TuneButton", new List<View>() { TuneButton }));

            _focusItemsManual
                .AddItem(KeyboardFocusableItem.CreateFrom("AutoManualTuning", new List<View>() { AutoManualTuningBoxView, AutoManualPicker }))
                .AddItem(KeyboardFocusableItem.CreateFrom("EditBandWidth", new List<View>() { EditBandWidthButton }))
                .AddItem(KeyboardFocusableItem.CreateFrom("EditFrequency", new List<View>() { EditFrequencyButton }))
                .AddItem(KeyboardFocusableItem.CreateFrom("DVBT", new List<View>() { DVBTBoxView, DVBTTuningCheckBox }))
                .AddItem(KeyboardFocusableItem.CreateFrom("DVBT2", new List<View>() { DVBT2BoxView, DVBT2TuningCheckBox }))
                .AddItem(KeyboardFocusableItem.CreateFrom("TuneButton", new List<View>() { TuneButton }));

            _focusItemsAuto.OnItemFocusedEvent += TunePage_OnItemFocusedEvent;
            _focusItemsManual.OnItemFocusedEvent += TunePage_OnItemFocusedEvent;
        }

        private void EditFrequencyButton_Clicked(object sender, EventArgs e)
        {
            var freqPage = new FrequencyPage(_loggingService, _dialogService, _driver, _config)
            {
                FrequencyKHz = _viewModel.TuningFrequencyKHz,
                PageTitle = "Tuning frequency",
                MinFrequencyKHz = _viewModel.AutoTuningMinFrequencyKHz,
                MaxFrequencyKHz = _viewModel.AutoTuningMaxFrequencyKHz,
                FrequencyKHzDefault = _viewModel.FrequencyKHzDefaultValue
            };

            Navigation.PushAsync(freqPage);

            freqPage.Disappearing += delegate
            {
                _viewModel.TuningFrequencyKHz = freqPage.FrequencyKHz;
            };
        }

        private void EditBandWidthButtton_Clicked(object sender, EventArgs e)
        {
            var bandWidthPage = new BandWidthPage(_loggingService, _dialogService, _driver, _config);
            bandWidthPage.BandWidth = _viewModel.TuneBandWidthKHz;

            Navigation.PushAsync(bandWidthPage);

            bandWidthPage.Disappearing += delegate
            {
                _viewModel.TuneBandWidthKHz = bandWidthPage.BandWidth;
            };
        }

        private void EditFrequencyFromButtton_Clicked(object sender, EventArgs e)
        {
            var freqPage = new FrequencyPage(_loggingService, _dialogService, _driver, _config)
            {
                FrequencyKHz = _viewModel.AutoTuningFrequencyFromKHz,
                PageTitle = "Tuning frequency from",
                MinFrequencyKHz = _viewModel.AutoTuningMinFrequencyKHz,
                MaxFrequencyKHz = _viewModel.AutoTuningMaxFrequencyKHz,
                FrequencyKHzDefault = _viewModel.AutoTuningFrequencyFromKHz
            };

            Navigation.PushAsync(freqPage);

            freqPage.Disappearing += delegate
            {
                _viewModel.AutoTuningFrequencyFromKHz = freqPage.FrequencyKHz;
            };
        }

        private void EditFrequencyToButtton_Clicked(object sender, EventArgs e)
        {
            var freqPage = new FrequencyPage(_loggingService, _dialogService, _driver, _config)
            {
                FrequencyKHz = _viewModel.AutoTuningFrequencyToKHz,
                PageTitle = "Tuning frequency to",
                MinFrequencyKHz = _viewModel.AutoTuningMinFrequencyKHz,
                MaxFrequencyKHz = _viewModel.AutoTuningMaxFrequencyKHz,
                FrequencyKHzDefault = _viewModel.AutoTuningFrequencyToKHz
            };

            Navigation.PushAsync(freqPage);

            freqPage.Disappearing += delegate
            {
                _viewModel.AutoTuningFrequencyToKHz = freqPage.FrequencyKHz;
            };
        }

        private void TuneButtton_Clicked(object sender, EventArgs e)
        {
            var page = new TuningPage(_loggingService, _dialogService, _driver, _config, _channelService)
            {
                FrequencyFromKHz = _viewModel.AutoTuningFrequencyFromKHz,
                FrequencyToKHz = _viewModel.AutoTuningFrequencyToKHz,
                BandWidthKHz = _viewModel.TuneBandWidthKHz,
                DVBTTuning = _viewModel.DVBTTuning,
                DVBT2Tuning = _viewModel.DVBT2Tuning
            };

            Navigation.PushAsync(page);
        }

        private void TunePage_OnItemFocusedEvent(KeyboardFocusableItemEventArgs args)
        {
            _previousFocusedItem = args.FocusedItem.Name;
        }

        private void TunePage_Appearing(object sender, EventArgs e)
        {
            if (_firstAppearing)
            {
                UpdateFocusedPart("AutoTuning", "TuneButton");

                Task.Run(async () =>
                {
                    await _viewModel.SetChannelsRange();
                });

                _firstAppearing = false;
            }
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