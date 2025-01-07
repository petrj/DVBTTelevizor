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


    }

}
