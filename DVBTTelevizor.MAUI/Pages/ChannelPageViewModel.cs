using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using LibVLCSharp.Shared;
using LoggerService;
using Microsoft.Maui;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI
{
    public class ChannelPageViewModel : BaseViewModel
    {
        private Channel? _channel = null;

        public ObservableCollection<MediaTrack> AudioTracks { get; set; } = new ObservableCollection<MediaTrack>();
        public ObservableCollection<MediaTrack> Subtitles { get; set; } = new ObservableCollection<MediaTrack>();

        public ObservableCollection<Channel>? _channels{ get; set; } = new ObservableCollection<Channel>();

        public ChannelPageViewModel(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration, IPublicDirectoryProvider publicDirectoryProvider)
          : base(loggingService, driver, tvConfiguration, publicDirectoryProvider)
        {
        }

        public Channel? Channel
        {
            get
            {
                return _channel;
            }
            set
            {
                _channel = value;

                UpdateAutioAndSubtitles();

                NotifyChannelChange();
            }
        }

        public ObservableCollection<Channel>? Channels
        {
            get
            {
                return _channels;
            }
            set
            {
                _channels = value;
            }
        }

        public string? StreamVideoSize
        {
            get
            {
                if (_channel == null)
                    return String.Empty;

                return _channel.VideoSize;
            }
        }

        public void UpdateAutioAndSubtitles()
        {
            SetAudioTracks(_channel.AudioTracks, _channel.SelectedAudioTrack);
            SetSubtitleTracks(_channel.Subtitles, _channel.SelectedSubtitle);
        }

        private void SetAudioTracks(Dictionary<int, string> playingChannelAudioTracks, string activeId)
        {
            AudioTracks.Clear();

            foreach (var kvp in playingChannelAudioTracks)
            {
                if (kvp.Key == -1)
                    continue;

                AudioTracks.Add(new MediaTrack()
                {
                    Key = kvp.Key,
                    Value = kvp.Value,
                    Active = kvp.Key.ToString() == activeId
                });
            }

            OnPropertyChanged(nameof(AudioTracks));
        }

        private void SetSubtitleTracks(Dictionary<int, string> playingChannelSubtitles, string activeId)
        {
            Subtitles.Clear();

            foreach (var kvp in playingChannelSubtitles)
            {
                if (kvp.Key == -1)
                    continue;

                Subtitles.Add(new MediaTrack()
                {
                    Key = kvp.Key,
                    Value = kvp.Value,
                    Active = kvp.Key.ToString() == activeId
                });
            }

            OnPropertyChanged(nameof(Subtitles));
        }

        public void NotifyChannelChange()
        {
            OnPropertyChanged(nameof(Channel));

            OnPropertyChanged(nameof(AudioTracksButtonVisible));
            OnPropertyChanged(nameof(SubtitlesTracksButtonVisible));
        }

        public bool AudioTracksButtonVisible
        {
            get
            {
                return
                    _channel != null &&
                    _channel.AudioTracks != null &&
                    _channel.AudioTracks.Count > 0;
            }
        }

        public bool SubtitlesTracksButtonVisible
        {
            get
            {
                return
                    _channel != null &&
                    _channel.Subtitles != null &&
                    _channel.Subtitles.Count > 0;
            }
        }
    }
}

