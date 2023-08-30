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
using static Android.Renderscripts.Sampler;

namespace DVBTTelevizor
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class FindSignalPage : ContentPage, IOnKeyDown
    {
        private FindSignalViewModel _viewModel;
        protected ILoggingService _loggingService;
        protected IDialogService _dialogService;
        protected IDVBTDriverManager _driver;
        protected DVBTTelevizorConfiguration _config;
        protected ChannelService _channelService;

        private bool _toolBarFocused = false;

        public FindSignalPage(ILoggingService loggingService, IDialogService dialogService, IDVBTDriverManager driver, DVBTTelevizorConfiguration config, ChannelService channelService)
        {
            InitializeComponent();

            _loggingService = loggingService;
            _dialogService = dialogService;
            _driver = driver;
            _config = config;
            _channelService = channelService;

            BindingContext = _viewModel = new FindSignalViewModel(_loggingService, _dialogService, _driver, _config, channelService);

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_UpdateDriverState, (message) =>
            {
                _viewModel.UpdateDriverState();
                if (_driver.Connected)
                {
                    Task.Run(async () => await _viewModel.Tune());
                }
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_DVBTDriverConfigurationFailed, (message) =>
            {
                Device.BeginInvokeOnMainThread(delegate
                {
                    _viewModel.UpdateDriverState();
                });
            });

            Appearing += Page_Appearing;
            Disappearing += Page_Disappearing;
        }

        public void SetFrequency(long freq, long bandWidth, int deliverySystem)
        {
            _viewModel.FrequencyKHz = freq;
            _viewModel.TuneBandWidthKHz = bandWidth;
            _viewModel.DeliverySystem = deliverySystem;
        }

        private void Page_Appearing(object sender, EventArgs e)
        {
            Task.Run(async () =>
            {
                await _viewModel.Tune();
                await _viewModel.Start();
            });

            _viewModel.NotifyFontSizeChange();
        }

        private void Page_Disappearing(object sender, EventArgs e)
        {
            Task.Run(async () => await _viewModel.Stop());
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
                }
                else
                {
                    _viewModel.SelectedToolbarItemName = null;
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
                    break;

                case KeyboardNavigationActionEnum.Up:
                    if (ToolBarSelected)
                    {
                        ToolBarSelected = false;
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
                    break;
            }
        }

        public void OnTextSent(string text)
        {

        }
    }
}