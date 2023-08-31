using LoggerService;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace DVBTTelevizor
{
    public class ChannelPageViewModel : BaseViewModel
    {
        private DVBTChannel _channel;
        private string _channelFrequencyAndMapPID;

        protected ChannelService _channelService;

        public Command UpCommand { get; set; }
        public Command DownCommand { get; set; }

        public bool Changed { get; set; } = false;

        private bool _signalStrengthVisible = false;
        private bool _streamBitRateVisible = false;
        private string _signalStrength = null;

        private bool _streamInfoVisible = false;
        private string _streamVideoSize = String.Empty;
        private string _bitrate = String.Empty;
        private string _streamAudioTracks = String.Empty;
        private string _streamSubTitles = String.Empty;

        public ObservableCollection<DVBTChannel> Channels { get; set; } = new ObservableCollection<DVBTChannel>();

        public async Task Reload(string channelFrequencyAndMapPID)
        {
            var channels = await _channelService.LoadChannels();

            Channels = new ObservableCollection<DVBTChannel>(channels.OrderBy(i => i.Number.PadLeft(4, '0')));

            _channelFrequencyAndMapPID = channelFrequencyAndMapPID;

            foreach (var channel in Channels)
            {
                if (channel.FrequencyAndMapPID ==  channelFrequencyAndMapPID)
                {
                    _channel = channel;
                    break;
                }
            }

            NotifyChannelChange();
        }

        public async Task SaveChannels()
        {
            await _channelService.SaveChannels(Channels);
        }

        public bool NumberUsed(string number)
        {
            foreach (var ch in Channels)
            {
                if (ch.FrequencyAndMapPID == _channelFrequencyAndMapPID)
                {
                    continue;
                }

                if (ch.Number == number.ToString())
                {
                    return true;
                }
            }

            return false;
        }

        public DVBTChannel Channel
        {
            get
            {
                return _channel;
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

        public bool SignalStrengthVisible
        {
            get
            {
                return _signalStrengthVisible;
            }
            set
            {
                _signalStrengthVisible = value;
            }
        }

        public bool StreamBitRateVisible
        {
            get
            {
                return _streamBitRateVisible;
            }
            set
            {
                _streamBitRateVisible = value;
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

        public string Bitrate
        {
            get
            {
                return _bitrate;
            }
            set
            {
                _bitrate = value;
            }
        }

        public string SignalStrength
        {
            get
            {
                return _signalStrength;
            }
            set
            {
                _signalStrength = value;
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
            OnPropertyChanged(nameof(SignalStrengthVisible));
            OnPropertyChanged(nameof(StreamBitRateVisible));
            OnPropertyChanged(nameof(SignalStrength));
            OnPropertyChanged(nameof(StreamVideoSize));
            OnPropertyChanged(nameof(StreamAudioTracks));
            OnPropertyChanged(nameof(StreamSubtitles));
            OnPropertyChanged(nameof(Bitrate));
        }

        public ChannelPageViewModel(ILoggingService loggingService, IDialogService dialogService, IDVBTDriverManager driver, DVBTTelevizorConfiguration config, ChannelService channelService)
            : base(loggingService, dialogService, driver, config)
        {
            _loggingService = loggingService;
            _dialogService = dialogService;

            UpCommand = new Command(async (itm) => await Up());
            DownCommand = new Command(async (itm) => await Down());
            _channelService = channelService;
        }

        private async Task Up()
        {
            try
            {
                _loggingService.Info($"Moving channel {Channel.Name} up");

                var index = Channels.IndexOf(Channel);

                if (index == -1 || index == 0)
                    return;

                var channelPrev = Channels[index - 1];

                // swap numbers
                var tempNum = channelPrev.Number;
                channelPrev.Number = Channel.Number;
                Channel.Number = tempNum;

                // swap channels
                var tmpCh = Channels[index - 1];
                Channels[index - 1] = Channels[index];
                Channels[index] = tmpCh;

                Changed = true;
                await SaveChannels();
                NotifyChannelChange();

            } catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
        }

        private async Task Down()
        {
            try
            {
                _loggingService.Info($"Moving channel {Channel.Name} down");

                var index = Channels.IndexOf(Channel);

                if (index == -1 || index == Channels.Count - 1)
                    return;

                var channelNext = Channels[index + 1];

                // swap numbers
                var tempNum = channelNext.Number;
                channelNext.Number = Channel.Number;
                Channel.Number = tempNum;

                // swap channels
                var tmpCh = Channels[index];
                Channels[index] = Channels[index + 1];
                Channels[index + 1] = tmpCh;

                Changed = true;
                await SaveChannels();
                NotifyChannelChange();
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
        }
    }
}
