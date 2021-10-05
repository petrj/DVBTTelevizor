using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace DVBTTelevizor
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PlayerPage : ContentPage
    {
        LibVLC _libVLC = null;
        MediaPlayer _mediaPlayer;
        DVBTDriverManager _driver;
        Media _media = null;

        private PlayerPageViewModel _viewModel;

        public Command CheckStreamCommand { get; set; }

        public PlayerPage(DVBTDriverManager driver)
        {
            InitializeComponent();

            BindingContext = _viewModel = new PlayerPageViewModel();

            _driver = driver;
         
            Core.Initialize();

            _libVLC = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVLC) { EnableHardwareDecoding = true };
            videoView.MediaPlayer = _mediaPlayer;

            CheckStreamCommand = new Command(async () => await CheckStream());

            BackgroundCommandWorker.RunInBackground(CheckStreamCommand, 3, 5);
        }

        public bool Playing
        {
            get
            {
                return videoView.MediaPlayer.IsPlaying;
            }
        }

        public string Title
        {
            get
            {
                return _viewModel.Title;
            }
            set
            {
                _viewModel.Title = value;
            }
        }

        public void OnDoubleTapped(object sender, EventArgs e)
        {
            
        }

        private void SwipeGestureRecognizer_Swiped(object sender, SwipedEventArgs e)
        {
            // go back
            Navigation.PopModalAsync();
        }

        private void SwipeGestureRecognizer_Up(object sender, SwipedEventArgs e)
        {
            
        }

        private void SwipeGestureRecognizer_Down(object sender, SwipedEventArgs e)
        {
            
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (_media == null && _driver.VideoStream != null)
            {
                _media = new Media(_libVLC, _driver.VideoStream, new string[] { });
            }
            
            videoView.MediaPlayer.Play(_media);
            
        }        

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            videoView.MediaPlayer.Stop();

           Task.Run(async () =>
           {
               await _driver.Stop();
           });            
        }

        public void StopPlay()
        {
            videoView.MediaPlayer.Stop();
        }

        public void StartPlay()
        {
            _media = new Media(_libVLC, _driver.VideoStream, new string[] { });
            videoView.MediaPlayer.Play(_media);
        }

        private async Task CheckStream()
        {        
            Device.BeginInvokeOnMainThread(() =>
            {
                if (!videoView.MediaPlayer.IsPlaying)
                {
                    videoView.MediaPlayer.Play(_media);
                }

                if (
                        (_mediaPlayer.VideoTrackCount <= 0)             
                   )
                {
                    _viewModel.AudioViewVisible = true;
                }
                else
                {
                    _viewModel.AudioViewVisible = false;
                }
            });
        }

    }
}