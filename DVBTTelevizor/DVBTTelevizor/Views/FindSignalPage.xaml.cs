using LoggerService;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace DVBTTelevizor
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class FindSignalPage : ContentPage, IOnKeyDown
    {
        private TuneViewModel _viewModel;
        protected ILoggingService _loggingService;
        protected IDialogService _dialogService;
        protected IDVBTDriverManager _driver;
        protected DVBTTelevizorConfiguration _config;
        protected ChannelService _channelService;

        private bool _toolBarFocused = false;

        private KeyboardFocusableItemList _focusItems;
        private string _previousFocusedItem = null;
        private BackgroundWorker _signalStrengthBackgroundWorker = null;
        private bool _appeared = false;
        private bool _tuning = false;

        public FindSignalPage(ILoggingService loggingService, IDialogService dialogService, IDVBTDriverManager driver, DVBTTelevizorConfiguration config, ChannelService channelService)
        {
            InitializeComponent();

            _loggingService = loggingService;
            _dialogService = dialogService;
            _driver = driver;
            _config = config;
            _channelService = channelService;

            BindingContext = _viewModel = new TuneViewModel(_loggingService, _dialogService, _driver, _config, channelService);

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

            BuildFocusableItems();

            _signalStrengthBackgroundWorker = new BackgroundWorker();
            _signalStrengthBackgroundWorker.WorkerSupportsCancellation = true;
            _signalStrengthBackgroundWorker.DoWork += SignalStrengthBackgroundWorker_DoWork;


            Appearing += Page_Appearing;
            Disappearing += Page_Disappearing;

            DVBTPicker.SelectedIndexChanged += DVBTPicker_SelectedIndexChanged;
        }

        private void DVBTPicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            Task.Run(async () => await ReTune());
        }

        private async Task ReTune()
        {
            _loggingService.Info($"Retune {_viewModel.FrequencyKHz} KHz");

            if (_driver.Connected)
            {
                try
                {
                    _tuning = true;

                    var res = await _driver.SetPIDs(new List<long>() { 0, 17 });
                    if (res.SuccessFlag)
                    {
                        await _driver.Tune(_viewModel.FrequencyKHz * 1000, _viewModel.TuneBandWidthKHz * 1000, DVBTPicker.SelectedIndex);
                    }
                }
                catch (Exception ex)
                {
                    _loggingService.Error(ex);
                } finally
                {
                    _tuning = false;
                }

            }
        }

        private void SignalStrengthBackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            _loggingService.Info("Starting SignalStrengthBackgroundWorker_DoWork");

            while (!_signalStrengthBackgroundWorker.CancellationPending)
            {
                try
                {
                    if (_driver.Connected)
                    {
                        if (!_tuning)
                        {
                            Task.Run(async () =>
                            {
                                var status = await _driver.GetStatus();
                                if (status.SuccessFlag)
                                {
                                    _viewModel.SignalStrengthProgress = status.rfStrengthPercentage / 100.0;
                                }
                            }).Wait();
                        } else
                        {
                            _viewModel.SignalStrengthProgress = 0;
                        }
                    } else
                    {
                        _viewModel.SignalStrengthProgress = 0;
                    }
                } catch (Exception ex)
                {
                    _loggingService.Error(ex);
                }

                Thread.Sleep(1000);
            }

            _loggingService.Info("SignalStrengthBackgroundWorker_DoWork finished");
        }

        private void BuildFocusableItems()
        {
            _focusItems = new KeyboardFocusableItemList();

            _focusItems
                .AddItem(KeyboardFocusableItem.CreateFrom("EditBandWidth", new List<View>() { EditBandWidthButton }))
                .AddItem(KeyboardFocusableItem.CreateFrom("EditFrequency", new List<View>() { EditFrequencyButton }))
                .AddItem(KeyboardFocusableItem.CreateFrom("DVBT", new List<View>() { DVBTBoxView, DVBTPicker }));

            _focusItems.OnItemFocusedEvent += TuneOptionsPage_OnItemFocusedEvent;
        }

        private async void EditFrequencyButton_Clicked(object sender, EventArgs e)
        {
            var freqPage = new FrequencyPage(_loggingService, _dialogService, _driver, _config)
            {
                FrequencyKHz = _viewModel.FrequencyKHz,
                PageTitle = "Tuning frequency",
                MinFrequencyKHz = _viewModel.FrequencyMinKHz,
                MaxFrequencyKHz = _viewModel.FrequencyMaxKHz,
                FrequencyKHzDefault = _viewModel.FrequencyDefaultKHz,
                FrequencyKHzSliderStep = _viewModel.TuneBandWidthKHz
            };

            await Navigation.PushAsync(freqPage);

            freqPage.Disappearing += delegate
            {
                if (_viewModel.FrequencyKHz != freqPage.FrequencyKHz)
                {
                    _viewModel.FrequencyKHz = freqPage.FrequencyKHz;
                    Task.Run(async () => await ReTune());
                }
            };
        }

        private async void EditBandWidthButtton_Clicked(object sender, EventArgs e)
        {
            var bandWidthPage = new BandWidthPage(_loggingService, _dialogService, _driver, _config);
            bandWidthPage.BandWidth = _viewModel.TuneBandWidthKHz;

            await Navigation.PushAsync(bandWidthPage);

            bandWidthPage.Disappearing += delegate
            {
                if (bandWidthPage.BandWidth != _viewModel.TuneBandWidthKHz)
                {
                    _viewModel.TuneBandWidthKHz = bandWidthPage.BandWidth;
                    Task.Run(async () => await ReTune());
                }
            };
        }


        private void TuneButtton_Clicked(object sender, EventArgs e)
        {
            if (!_driver.Connected)
            {
                _dialogService.Error($"Device not connected");
                return;
            }
        }

        private void TuneOptionsPage_OnItemFocusedEvent(KeyboardFocusableItemEventArgs args)
        {
            _previousFocusedItem = args.FocusedItem.Name;

            // scroll to item
            TuneOptionsScrollView.ScrollToAsync(0, args.FocusedItem.MaxYPosition - Height / 2, false);
        }

        private void Page_Appearing(object sender, EventArgs e)
        {
            Task.Run(async () =>
            {
                if (!_appeared)
                {
                    _focusItems.DeFocusAll();

                    await _viewModel.SetFrequencies();
                    await ReTune();

                    _appeared = true;
                }

                _signalStrengthBackgroundWorker.RunWorkerAsync();
            });

            _viewModel.NotifyFontSizeChange();
        }

        private void Page_Disappearing(object sender, EventArgs e)
        {
            _signalStrengthBackgroundWorker.CancelAsync();
        }

        private async void ToolConnect_Clicked(object sender, EventArgs e)
        {
            if (_driver.Connected)
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
                    _previousFocusedItem = _focusItems.LastFocusedItemName;
                    _focusItems.DeFocusAll();
                }
                else
                {
                    _viewModel.SelectedToolbarItemName = null;
                    _focusItems.FocusItem(_previousFocusedItem);
                }

                _viewModel.NotifyToolBarChange();
                _toolBarFocused = value;
            }
        }

        public async void OnKeyDown(string key, bool longPress)
        {
            _loggingService.Debug($"FindSignalPage OnKeyDown {key}");

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
                            case "EditBandWidth":
                                EditBandWidthButtton_Clicked(this, null);
                                break;

                            case "EditFrequency":
                                EditFrequencyButton_Clicked(this, null);
                                break;

                            case "TuneButton":
                                TuneButtton_Clicked(this, null);
                                break;

                            case "DVBT":
                                DVBTPicker.Focus();
                                break;
                        }
                    }
                    break;
            }
        }

        public void OnTextSent(string text)
        {

        }
    }
}