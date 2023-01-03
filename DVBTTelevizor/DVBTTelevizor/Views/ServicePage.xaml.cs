using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using System.IO;
using System.Threading;
using LoggerService;
using Xamarin.Forms.Xaml;

namespace DVBTTelevizor
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ServicePage : ContentPage, IOnKeyDown
    {
        private ServicePageViewModel _viewModel;
        protected ILoggingService _loggingService;
        protected IDialogService _dialogService;
        protected IDVBTDriverManager _driver;
        protected DVBTTelevizorConfiguration _config;
        private bool _toolBarFocused = false;
        private string _lastFocusedItem = null;
        private KeyboardFocusableItemList _focusItems;

        public ServicePage(ILoggingService loggingService, IDialogService dialogService, IDVBTDriverManager driver, DVBTTelevizorConfiguration config)
        {
            InitializeComponent();

            _loggingService = loggingService;
            _dialogService = dialogService;
            _driver = driver;
            _config = config;

            BindingContext = _viewModel = new ServicePageViewModel(_loggingService, _dialogService, _driver, _config);
            _viewModel.TuneFrequency = "626";
            _viewModel.SelectedDeliverySystemType = _viewModel.DeliverySystemTypes[1];

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

            Appearing += ServicePage_Appearing;

            BuildFocusableItems();
        }

        private void ServicePage_Appearing(object sender, EventArgs e)
        {
            _focusItems.FocusItem("GetStatus");
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
                    if (_lastFocusedItem != null)
                    {
                        _focusItems.FocusItem(_lastFocusedItem);
                    }
                }

                _viewModel.NotifyToolBarChange();
                _toolBarFocused = value;
            }
        }

        private void BuildFocusableItems()
        {
            _focusItems = new KeyboardFocusableItemList();

            _focusItems
                .AddItem(KeyboardFocusableItem.CreateFrom("GetStatus", new List<View>() { GetStatusButton }))
                .AddItem(KeyboardFocusableItem.CreateFrom("GetVersion", new List<View>() { GetVersionButton }))
                .AddItem(KeyboardFocusableItem.CreateFrom("GetCap", new List<View>() { GetCapButton }))

                .AddItem(KeyboardFocusableItem.CreateFrom("FrequencyMhz", new List<View>() { FrequencyMhzBoxView, EntryFrequency }))
                .AddItem(KeyboardFocusableItem.CreateFrom("FrequencyChannel", new List<View>() { FrequencyChannelBoxView }))
                .AddItem(KeyboardFocusableItem.CreateFrom("BandWith", new List<View>() { BandWithBoxView, EntryBandWidth }))
                .AddItem(KeyboardFocusableItem.CreateFrom("DeliverySystem", new List<View>() { DeliverySystemBoxView }))
                .AddItem(KeyboardFocusableItem.CreateFrom("Tune", new List<View>() { TuneButton }))

                .AddItem(KeyboardFocusableItem.CreateFrom("EntryPIDs", new List<View>() { EntryPIDs }))
                .AddItem(KeyboardFocusableItem.CreateFrom("SetPIDs", new List<View>() { SetPIDsButton }))

                .AddItem(KeyboardFocusableItem.CreateFrom("ScanPSI", new List<View>() { ScanPSIButton }))
                .AddItem(KeyboardFocusableItem.CreateFrom("ScanEIT", new List<View>() { ScanEITButton }))
                .AddItem(KeyboardFocusableItem.CreateFrom("Play", new List<View>() { PlayButton }))
                .AddItem(KeyboardFocusableItem.CreateFrom("Record", new List<View>() { RecordButton }))
                .AddItem(KeyboardFocusableItem.CreateFrom("StopRecord", new List<View>() { StopRecordButton }));

            _focusItems.OnItemFocusedEvent += ServicePage_OnItemFocusedEvent;
        }

        private void ServicePage_OnItemFocusedEvent(KeyboardFocusableItemEventArgs args)
        {
            _lastFocusedItem = args.FocusedItem.Name;

            // scroll to item
            ServicePageScrollView.ScrollToAsync(0, args.FocusedItem.MaxYPosition - Height / 2, false);
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

        protected override void OnAppearing()
        {
            base.OnAppearing();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
        }

        public void Done()
        {
            MessagingCenter.Unsubscribe<string>(this, BaseViewModel.MSG_UpdateDriverState);
            MessagingCenter.Unsubscribe<string>(this, BaseViewModel.MSG_DVBTDriverConfigurationFailed);
        }

        public async void OnKeyDown(string key, bool longPress)
        {
            _loggingService.Debug($"ServicePage OnKeyDown {key}");

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
                            case "TuneButton":
                                _viewModel.TuneCommand.Execute(null);
                                break;

                            case "GetStatus":
                                _viewModel.GetStatusCommand.Execute(null);
                                break;

                            case "GetVersion":
                                _viewModel.GetVersionCommand.Execute(null);
                                break;

                            case "GetCap":
                                _viewModel.GetCapabilitiesCommand.Execute(null);
                                break;

                            case "FrequencyMhz":
                                EntryFrequency.Focus();
                                break;

                            case "FrequencyChannel":
                                FrequencyChannelPicker.Focus();
                                break;

                            case "EntryPIDs":
                                EntryPIDs.Focus();
                                break;

                            case "BandWith":
                                EntryBandWidth.Focus();
                                break;

                            case "DeliverySystem":
                                DeliverySystemPicker.Focus();
                                break;

                            case "Tune":
                                _viewModel.TuneCommand.Execute(null);
                                break;

                            case "SetPIDs":
                                _viewModel.SetPIDsCommand.Execute(null);
                                break;

                            case "ScanPSI":
                                _viewModel.ScanPSICommand.Execute(null);
                                break;

                            case "ScanEIT":
                                _viewModel.ScanEITCommand.Execute(null);
                                break;

                            case "Play":
                                _viewModel.PlayCommand.Execute(null);
                                break;

                            case "Record":
                                _viewModel.StartRecordCommand.Execute(null);
                                break;

                            case "StopRecord":
                                _viewModel.StopRecordCommand.Execute(null);
                                break;
                        }
                    }
                    break;
            }
        }
    }
}