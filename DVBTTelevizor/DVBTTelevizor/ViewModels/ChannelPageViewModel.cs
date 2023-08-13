using LoggerService;
using System;
using System.Collections.Generic;
using System.Text;

namespace DVBTTelevizor
{
    public class ChannelPageViewModel : BaseViewModel
    {
        private DVBTChannel _channel;
        private bool _streamInfoVisible = false;
        private string _streamVideoSize = String.Empty;
        private string _streamAudioTracks = String.Empty;
        private string _streamSubTitles = String.Empty;

        public DVBTChannel Channel
        {
            get
            {
                return _channel;
            }
            set
            {
                _channel = value;

                NotifyChannelChange();
            }
        }

        public bool StreamInfoVisible
        {
            get
            {
                return _streamInfoVisible;
            }
            set
            {
                _streamInfoVisible = value;
            }
        }

        public string StreamVideoSize
        {
            get
            {
                return _streamVideoSize;
            }
            set
            {
                _streamVideoSize = value;
            }
        }

        public string StreamAudioTracks
        {
            get
            {
                return _streamAudioTracks;
            }
            set
            {
                _streamAudioTracks = value;
            }
        }

        public string StreamSubtitles
        {
            get
            {
                return _streamSubTitles;
            }
            set
            {
                _streamSubTitles = value;
            }
        }

        public void NotifyChannelChange()
        {
            OnPropertyChanged(nameof(Channel));
            OnPropertyChanged(nameof(StreamInfoVisible));
            OnPropertyChanged(nameof(StreamVideoSize));
            OnPropertyChanged(nameof(StreamAudioTracks));
            OnPropertyChanged(nameof(StreamSubtitles));
        }

        public ChannelPageViewModel(ILoggingService loggingService, IDialogService dialogService, IDVBTDriverManager driver, DVBTTelevizorConfiguration config)
            :base(loggingService, dialogService, driver, config)
        {
            _loggingService = loggingService;
            _dialogService = dialogService;
        }
    }
}
