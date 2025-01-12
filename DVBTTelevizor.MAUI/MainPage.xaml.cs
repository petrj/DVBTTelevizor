using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using LibVLCSharp.Shared;
using LoggerService;


namespace DVBTTelevizor.MAUI
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        MainViewModel? _mainViewModel = null;
        private ILoggingService _loggingService { get; set; }
        private IDVBTDriver _driver { get; set; }

        public MainPage(ILoggingProvider loggingProvider)
        {
            InitializeComponent();

            _loggingService = loggingProvider.GetLoggingService();

            _loggingService.Info("MainPage starting");

            _driver = new TestingDVBTDriver(_loggingService);

            BindingContext = _mainViewModel = new MainViewModel(_loggingService, _driver);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _mainViewModel?.OnAppearing();
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

        private void DriverStateButton_Clicked(object sender, EventArgs e)
        {
            if (!_driver.Connected)
            {
                WeakReferenceMessenger.Default.Send(new DVBTDriverConnectMessage("Connect"));
            }
        }
    }

}
