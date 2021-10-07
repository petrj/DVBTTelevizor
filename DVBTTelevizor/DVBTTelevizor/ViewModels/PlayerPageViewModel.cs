using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Xamarin.Forms;

namespace DVBTTelevizor
{
    public class PlayerPageViewModel : BaseNotifyPropertyModel
    {
        private bool _videoViewVisible = true;
        private string _title = String.Empty;
        private int _animePos = 2;
        private bool _animePosIncreasing = true;

        public Command AnimeIconCommand { get; set; }

        public PlayerPageViewModel(DVBTTelevizorConfiguration config)
            :base(config)
        {
            AnimeIconCommand = new Command(Anime);

            BackgroundCommandWorker.RunInBackground(AnimeIconCommand, 1, 1);
        }

        public string Title
        {
            get
            {
                return _title;
            }
            set
            {
                _title = value;
                OnPropertyChanged(nameof(Title));
            }
        }

        public string FontSizeForChannel
        {
            get
            {
                return "22";
            }
        }


        public bool VideoViewVisible
        {
            get
            {
                return _videoViewVisible;
            }
            set
            {
                _videoViewVisible = value;

                OnPropertyChanged(nameof(VideoViewVisible));
                OnPropertyChanged(nameof(AudioViewVisible));
            }
        }

        public bool AudioViewVisible
        {
            get
            {
                return !_videoViewVisible;
            }
            set
            {
                _videoViewVisible = !value;

                OnPropertyChanged(nameof(VideoViewVisible));
                OnPropertyChanged(nameof(AudioViewVisible));
            }
        }

        public string AudioIcon
        {
            get
            {
                return "Audio" + _animePos.ToString();
            }
        }

        public void Anime()
        {
            if (_animePosIncreasing)
            {
                _animePos++;
                if (_animePos > 3)
                {
                    _animePos = 2;
                    _animePosIncreasing = !_animePosIncreasing;
                }
            }
            else
            {
                _animePos--;
                if (_animePos < 0)
                {
                    _animePos = 1;
                    _animePosIncreasing = !_animePosIncreasing;
                }
            }

            OnPropertyChanged(nameof(AudioIcon));
        }

    }
}
