using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using LibVLCSharp.Shared;
using LoggerService;


namespace DVBTTelevizor.MAUI
{
    public partial class MainPage : ContentPage
    {
        private MainViewModel _mainViewModel;
        private ILoggingService _loggingService { get; set; }
        private IDVBTDriver _driver { get; set; }
        private IDialogService _dialogService;
        private bool _firstAppearing = true;

        public MainPage(ILoggingProvider loggingProvider)
        {
            InitializeComponent();

            _loggingService = loggingProvider.GetLoggingService();

            _loggingService.Info("MainPage starting");

            _dialogService = new DialogService(this);

            _driver = new TestingDVBTDriver(_loggingService);

            BindingContext = _mainViewModel = new MainViewModel(_loggingService, _driver);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _mainViewModel?.OnAppearing();

            if (_firstAppearing)
            {
                _firstAppearing = false;
                _loggingService.Info("First appearing - sending Connect message");

                WeakReferenceMessenger.Default.Send(new DVBTDriverConnectMessage("Connect"));
            }
        }

        private void VideoView_MediaPlayerChanged(object sender, MediaPlayerChangedEventArgs e)
        {
            _mainViewModel.OnVideoViewInitialized();
        }

        private void TuneButton_Clicked(object sender, EventArgs e)
        {

        }

        private void DriverButton_Clicked(object sender, EventArgs e)
        {

        }

        private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
        {

        }

        private void TapGestureRecognizer_Tapped_1(object sender, TappedEventArgs e)
        {

        }

        private void SwipeGestureRecognizer_Swiped(object sender, SwipedEventArgs e)
        {

        }

        private void SwipeGestureRecognizer_Swiped_1(object sender, SwipedEventArgs e)
        {

        }

        private void SwipeGestureRecognizer_Swiped_2(object sender, SwipedEventArgs e)
        {

        }

        private void SwipeGestureRecognizer_Swiped_3(object sender, SwipedEventArgs e)
        {

        }

        private void SwipeGestureRecognizer_Swiped_4(object sender, SwipedEventArgs e)
        {

        }

        private void ConnectButton_Clicked(object sender, EventArgs e)
        {
            WeakReferenceMessenger.Default.Send(new DVBTDriverConnectMessage("Connect"));
        }

        private async void DriverStateButton_Clicked(object sender, EventArgs e)
        {
            if (_mainViewModel.DriverInstalled)
            {
                if (_driver.Connected)
                {
                    if (!(await _dialogService.Confirm($"Connected device: {_driver.Configuration.DeviceName}.", $"Device status", "Back", "Disconnect")))
                    {
                        //await _viewModel.DisconnectDriver();
                    }
                }
                else
                {
                    if (await _dialogService.Confirm($"Disconnected.", $"Device status", "Connect", "Back"))
                    {
                        WeakReferenceMessenger.Default.Send(new DVBTDriverConnectMessage("Connect"));
                    }
                }
            }
            else
            {
                if (await _dialogService.Confirm($"DVB-T Driver not installed.", $"Device status", "Install DVB-T Driver", "Back"))
                {
                    //InstallDriver_Clicked(this, null);
                }
            }
        }
    }

}
