using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
        bool _fullscreen = false;
        bool _playInProgress = false;



        public Command CheckStreamCommand { get; set; }

        public PlayerPage(DVBTDriverManager driver, DVBTTelevizorConfiguration config)
        {
            InitializeComponent();

            BindingContext = _viewModel = new PlayerPageViewModel(config);

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
                return _playInProgress;

                //videoView.MediaPlayer.IsPlaying can be false in case of internet disconnection
            }
            set
            {
                _playInProgress = value;
            }
        }

        public PlayStreamInfo PlayStreamInfo
        {
            get
            {
                return _viewModel.PlayStreamInfo;
            }
            set
            {
                _viewModel.PlayStreamInfo = value;
            }
        }

        public void OnDoubleTapped(object sender, EventArgs e)
        {
            if (!_fullscreen)
            {
                MessagingCenter.Send(String.Empty, BaseViewModel.MSG_EnableFullScreen);
                _fullscreen = true;
            }
            else
            {
                MessagingCenter.Send(String.Empty, BaseViewModel.MSG_DisableFullScreen);
                _fullscreen = false;
            }
        }

        private void SwipeGestureRecognizer_Swiped(object sender, SwipedEventArgs e)
        {
            // go back
            Navigation.PopModalAsync();
        }

        private void SwipeGestureRecognizer_Up(object sender, SwipedEventArgs e)
        {
            MessagingCenter.Send(String.Empty, BaseViewModel.MSG_PlayNextChannel);
        }

        private void SwipeGestureRecognizer_Down(object sender, SwipedEventArgs e)
        {
            MessagingCenter.Send(String.Empty, BaseViewModel.MSG_PlayPreviousChannel);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            StartPlay();

            if (!_fullscreen)
            {
                OnDoubleTapped(this, null);
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            StopPlay();

            if (_fullscreen)
            {
                OnDoubleTapped(this, null);
            }
        }

        public void Resume()
        {
            if (Playing)
            {
                // workaround for black screen after resume (only audio is playing)
                // TODO: resume video without reinitializing

                Device.BeginInvokeOnMainThread(() =>
                {
                    if (_mediaPlayer.VideoTrack != -1)
                    {
                        //var pos = videoView.MediaPlayer.Position;
                        videoView.MediaPlayer.Stop();

                        VideoStackLayout.Children.Remove(videoView);
                        VideoStackLayout.Children.Add(videoView);

                        videoView.MediaPlayer.Play();
                        //videoView.MediaPlayer.Position = pos;
                    }
                });
            }
        }

        public void StartPlay()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                if (_driver.VideoStream != null)
                {
                    _media = new Media(_libVLC, _driver.VideoStream, new string[] { });
                    videoView.MediaPlayer.Play(_media);
                }

                Playing = true;
            });
        }

        public void StopPlay()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                Playing = false;
                videoView.MediaPlayer.Stop();
                Task.Run(async () =>
                {
                    if (!_driver.Recording)
                    {
                        await _driver.Stop();
                    }
                });
            });
        }


        private async Task CheckStream()
        {
            if (!Playing)
            {
                return;
            }

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

                // updating EPG
                _viewModel.NotifyEPGChange();
            });
        }
    }
}