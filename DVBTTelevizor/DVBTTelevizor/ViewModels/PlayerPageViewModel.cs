using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Xamarin.Forms;

namespace DVBTTelevizor
{
    public class PlayerPageViewModel : ConfigViewModel
    {
        private bool _videoViewVisible = true;
        private int _animePos = 2;
        private bool _animePosIncreasing = true;
        private PlayStreamInfo _playStreamInfo;

        public Command AnimeIconCommand { get; set; }

        public PlayerPageViewModel(DVBTTelevizorConfiguration config)
            :base(config)
        {
            AnimeIconCommand = new Command(Anime);

            BackgroundCommandWorker.RunInBackground(AnimeIconCommand, 1, 1);
        }

        public void NotifyEPGChange()
        {
            OnPropertyChanged(nameof(ChannelTitle));
            OnPropertyChanged(nameof(ProviderName));
            OnPropertyChanged(nameof(EPGProgress));
            OnPropertyChanged(nameof(EPGTimeStart));
            OnPropertyChanged(nameof(EPGTimeFinish));
            OnPropertyChanged(nameof(EPGTitle));
            OnPropertyChanged(nameof(EPGDescription));
        }

        public PlayStreamInfo PlayStreamInfo
        {
            get
            {
                return _playStreamInfo;
            }
            set
            {
                _playStreamInfo = value;

                NotifyEPGChange();
            }
        }

        public string ChannelTitle
        {
            get
            {
                return (_playStreamInfo == null) || (_playStreamInfo.Channel == null) ? String.Empty : _playStreamInfo.Channel.Name;
            }
        }

        public string ProviderName
        {
            get
            {
                return (_playStreamInfo == null) || (_playStreamInfo.Channel == null) ? String.Empty : _playStreamInfo.Channel.ProviderName;
            }
        }

        public double EPGProgress
        {
            get
            {
                if (_playStreamInfo == null ||
                    _playStreamInfo.CurrentEvent == null)
                {
                    return 0;
                }

                return _playStreamInfo.CurrentEvent.Progress;
            }
        }

        public string EPGTimeStart
        {
            get
            {
                if (_playStreamInfo == null ||
                    _playStreamInfo.CurrentEvent == null ||
                    _playStreamInfo.CurrentEvent.StartTime > DateTime.Now ||
                    _playStreamInfo.CurrentEvent.FinishTime < DateTime.Now )
                {
                    return String.Empty;
                }

                return _playStreamInfo.CurrentEvent.EPGTimeStartDescription;
            }
        }

        public string EPGTimeFinish
        {
            get
            {
                if (_playStreamInfo == null ||
                    _playStreamInfo.CurrentEvent == null ||
                    _playStreamInfo.CurrentEvent.StartTime > DateTime.Now ||
                    _playStreamInfo.CurrentEvent.FinishTime < DateTime.Now)
                {
                    return String.Empty;
                }

                return _playStreamInfo.CurrentEvent.EPGTimeFinishDescription;
            }
        }

        public string EPGTitle
        {
            get
            {
                if (_playStreamInfo == null ||
                    _playStreamInfo.CurrentEvent == null ||
                    _playStreamInfo.CurrentEvent.StartTime > DateTime.Now ||
                    _playStreamInfo.CurrentEvent.FinishTime < DateTime.Now)
                {
                    return String.Empty;
                }

                return _playStreamInfo.CurrentEvent.EventName;
            }
        }

        public string EPGDescription
        {
            get
            {
                if (_playStreamInfo == null ||
                    _playStreamInfo.CurrentEvent == null ||
                    _playStreamInfo.CurrentEvent.StartTime > DateTime.Now ||
                    _playStreamInfo.CurrentEvent.FinishTime < DateTime.Now)
                {
                    return String.Empty;
                }

                return _playStreamInfo.CurrentEvent.Text;
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
