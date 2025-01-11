using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using LibVLCSharp.Shared;


namespace DVBTTelevizor.MAUI
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        MainViewModel? _mainViewModel = null;

        public MainPage()
        {
            InitializeComponent();

            BindingContext = _mainViewModel = new MainViewModel();
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
    }

}
