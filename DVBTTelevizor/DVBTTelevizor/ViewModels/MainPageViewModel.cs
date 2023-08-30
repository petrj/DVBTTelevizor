using DVBTTelevizor.Models;
using DVBTTelevizor.Services;
using LoggerService;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace DVBTTelevizor
{
    public class MainPageViewModel : BaseViewModel
    {
        ChannelService _channelService;

        private static SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        public Command RefreshCommand { get; set; }
        public Command RefreshEPGCommand { get; set; }
        public Command ImportCommand { get; set; }

        public Command AnimeIconCommand { get; set; }

        public Command LongPressCommand { get; set; }
        public Command ShortPressCommand { get; set; }

        public Command VideoLongPressCommand { get; set; }

        private DVBTChannel _selectedChannel;
        private DVBTChannel _recordingChannel;

        public bool DoNotScrollToChannel { get; set; } = false;

        public ObservableCollection<DVBTChannel> Channels { get; set; } = new ObservableCollection<DVBTChannel>();

        private PlayingStateEnum _playingState = PlayingStateEnum.Stopped;
        private DVBTChannel _playingChannel = null;

        private int _animePos = 2;
        private bool _animePosIncreasing = true;
        private bool _scanningEPG = false;

        private bool _EPGDetailEnabled = true;
        private bool? _EPGDetailVisibleLastValue = null;

        private int _refreshCounter = 0;
        private int _lastTeltetextPageNumber = 100;

        public Dictionary<int, string> PlayingChannelSubtitles { get; set; } = new Dictionary<int, string>();
        public Dictionary<int, string> PlayingChannelAudioTracks { get; set; } = new Dictionary<int, string>();
        public Size PlayingChannelAspect { get; set; } = new Size(-1, -1);

        public int AudioTrack { get; set; } = -100;
        public int Subtitles { get; set; } = -1;

        private bool _autoPlayProcessed = false;

        public EITManager EIT { get; set; }

        public enum SelectedPartEnum
        {
            ChannelsListOrVideo = 0,
            EPGDetail = 1,
            ToolBar = 2
        }

        public string RecordingFileName
        {
            get
            {
                if (_driver != null && _driver.Recording && !String.IsNullOrEmpty(_driver.RecordFileName))
                {
                    return _driver.RecordFileName;
                }

                return null;
            }
        }

        private SelectedPartEnum _selectedPart = SelectedPartEnum.ChannelsListOrVideo;

        public MainPageViewModel(ILoggingService loggingService, IDialogService dialogService, IDVBTDriverManager driver, DVBTTelevizorConfiguration config, ChannelService channelService)
            : base(loggingService, dialogService, driver, config)
        {
            _channelService = channelService;

            EIT = new EITManager(loggingService, driver);

            RefreshCommand = new Command(async () => await Refresh());
            RefreshEPGCommand = new Command(async () => await RefreshEPG());
            LongPressCommand = new Command(async (itm) => await LongPress(itm));
            VideoLongPressCommand = new Command(async (itm) => await VideoLongPress());
            ShortPressCommand = new Command(ShortPress);
            ImportCommand = new Command(async (json) => await ImportList(json));

            AnimeIconCommand = new Command(async () => await Anime());

            BackgroundCommandWorker.RunInBackground(RefreshEPGCommand, 2, 10);
            BackgroundCommandWorker.RunInBackground(AnimeIconCommand, 1, 1);

            RefreshCommand.Execute(null);
        }

        public void SelectchannelAfterStartup(int delaySeconds)
        {
            _loggingService.Info("SelectchannelAfterStartup");

            new Thread(async () =>
            {
                Thread.CurrentThread.IsBackground = true;

                Thread.Sleep(delaySeconds * 1000);

                Xamarin.Forms.Device.BeginInvokeOnMainThread(delegate
                {
                    var ch = SelectedChannel;
                    SelectedChannel = null;
                    SelectedChannel = ch;
                });

                await AutoPlay();
            }).Start();
        }

        public async Task AutoPlay()
        {
            if (_autoPlayProcessed)
                return;

            _loggingService.Info($"AutoPlay: {_config.ChannelAutoPlayedAfterStart}");

            if (string.IsNullOrEmpty(_config.ChannelAutoPlayedAfterStart))
            {
                _autoPlayProcessed = true;
                return;
            }

            if (_driver != null &&
                _driver.Connected &&
                Channels != null &&
                Channels.Count > 0)
            {
                _autoPlayProcessed = true;

                DVBTChannel _autoPlayChannel = null;

                var lastChannel = new DVBTChannel()
                {
                    Name = "<last channel>",
                    Frequency = 0,
                    ProgramMapPID = 0
                };

                if (lastChannel.FrequencyAndMapPID == _config.ChannelAutoPlayedAfterStart)
                {
                    //_viewModel.SelectedChannel = lastChannel;
                    _autoPlayChannel = await SelectChannelByFrequencyAndMapPID(_config.SelectedChannelFrequencyAndMapPID);
                }

                if (_autoPlayChannel == null)
                {
                    foreach (var ch in Channels)
                    {
                        if (ch.FrequencyAndMapPID == _config.ChannelAutoPlayedAfterStart)
                        {
                            _autoPlayChannel = ch;
                            break;
                        }
                    }
                }

                if (_autoPlayChannel != null)
                {
                    SelectedChannel = _autoPlayChannel;
                    MessagingCenter.Send(new PlayStreamInfo { Channel = SelectedChannel }, BaseViewModel.MSG_PlayStream);
                }
            }
        }

        public string RecordingLabel
        {
            get
            {
                if (_recordingChannel == null || _playingState == PlayingStateEnum.Stopped)
                    return string.Empty;

                return "\u25CF";
            }
        }

        public DVBTChannel RecordingChannel
        {
            get
            {
                return _recordingChannel;
            }
            set
            {
                _recordingChannel = value;

                OnPropertyChanged(nameof(RecordingLabel));
            }
        }

        public SelectedPartEnum SelectedPart
        {
            get
            {
                return _selectedPart;
            }
            set
            {
                // TODO: change ChannelsListView SelectedItem background color

                _selectedPart = value;

                OnPropertyChanged(nameof(EPGDescriptionBackgroundColor));
                NotifyToolBarChange();
            }
        }

        public Color EPGDescriptionBackgroundColor
        {
            get
            {
                if (SelectedPart == SelectedPartEnum.EPGDetail)
                    return Color.FromHex("005996");

                return Color.Black;
            }
        }

        public PlayingStateEnum PlayingState
        {
            get
            {
                return _playingState;
            }
            set
            {
                _playingState = value;
            }
        }

        public bool StandingOnEnd
        {
            get
            {
                try
                {
                    _semaphoreSlim.WaitAsync();

                    var item = SelectedChannel;

                    if (item == null)
                        return true;

                    DVBTChannel lastChannel = null;
                    foreach (var ch in Channels)
                    {
                        lastChannel = ch;
                    }

                    if (lastChannel == item)
                        return true;

                    return false;

                }
                finally
                {
                    _semaphoreSlim.Release();
                };
            }
        }

        public DVBTChannel PlayingChannel
        {
            get { return _playingChannel; }
            set
            {
                _playingChannel = value;
            }
        }

        private void NotifyEPGDetailVisibilityChange()
        {
            if (!_EPGDetailVisibleLastValue.HasValue || _EPGDetailVisibleLastValue.Value != EPGDetailVisible)
            {
                _EPGDetailVisibleLastValue = EPGDetailVisible;
                MessagingCenter.Send(String.Empty, BaseViewModel.MSG_EPGDetailVisibilityChange);
                OnPropertyChanged(nameof(EPGDetailVisible));
            }
        }

        public bool EPGDetailEnabled
        {
            get
            {
                return _EPGDetailEnabled;
            }
            set
            {
                _EPGDetailEnabled = value;
                OnPropertyChanged(nameof(EPGDetailVisible));
                NotifyEPGDetailVisibilityChange();
            }
        }

        public bool EPGDetailVisible
        {
            get
            {
                return
                    EPGDetailEnabled &&
                    SelectedChannel != null &&
                    SelectedChannel.CurrentEventItem != null;
            }
        }

        public string SelectedChannelEPGTitle
        {
            get
            {
                if (SelectedChannel == null || SelectedChannel.CurrentEventItem == null)
                    return String.Empty;

                return SelectedChannel.CurrentEventItem.EventName;
            }
        }

        public string SelectedChannelEPGDescription
        {
            get
            {
                if (SelectedChannel == null || SelectedChannel.CurrentEventItem == null)
                    return String.Empty;

                return SelectedChannel.CurrentEventItem.Text;
            }
        }

        public string SelectedChannelEPGTimeStart
        {
            get
            {
                if (SelectedChannel == null || SelectedChannel.CurrentEventItem == null)
                    return String.Empty;

                return SelectedChannel.CurrentEventItem.EPGTimeStartDescription;
            }
        }

        public string SelectedChannelEPGTimeFinish
        {
            get
            {
                if (SelectedChannel == null || SelectedChannel.CurrentEventItem == null)
                    return String.Empty;

                return SelectedChannel.CurrentEventItem.EPGTimeFinishDescription;
            }
        }

        public double SelectedChannelEPGProgress
        {
            get
            {
                if (SelectedChannel == null || SelectedChannel.CurrentEventItem == null)
                    return 0;

                return SelectedChannel.CurrentEventItem.Progress;
            }
        }

        public Color EPGProgressBackgroundColor
        {
            get
            {
                if (SelectedChannel == null || SelectedChannel.CurrentEventItem == null)
                    return Color.Black;

                return Color.White;
            }
        }

        public void NotifyMediaChange()
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                OnPropertyChanged(nameof(RecordingLabel));
                OnPropertyChanged(nameof(NoVideoTitle));
            });
        }

        private async Task ImportList(object json)
        {
            if (!(await _dialogService.Confirm("Are you sure to import channels list?")))
            {
                return;
            }

            try
            {
                _loggingService.Info($"Importing channels");

                var chs = await _channelService.LoadChannels();

                var importedChannels = JsonConvert.DeserializeObject<ObservableCollection<DVBTChannel>>(json as string);

                var count = 0;
                foreach (var ch in importedChannels)
                {
                    if (!ConfigViewModel.ChannelExists(chs, ch.FrequencyAndMapPID))
                    {
                        count++;
                        ch.Number = ConfigViewModel.GetNextChannelNumber(chs).ToString();
                        chs.Add(ch);
                    }
                }

                await _channelService.SaveChannels(chs);

                MessagingCenter.Send($"Imported channels count: {count}", BaseViewModel.MSG_ToastMessage);

                await Refresh();
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex,"Import failed");
                await _dialogService.Error($"Import failed");
            }
        }

        public async Task<EPGCurrentEvent> GetChannelEPG(DVBTChannel channel)
        {
            if (channel == null)
                return null;

            try
            {
                if (EIT != null)
                {
                    var currEv = EIT.GetEvent(DateTime.Now, channel.Frequency, channel.ProgramMapPID);
                    if (currEv != null)
                    {
                        channel.SetCurrentEvent(currEv);
                        channel.NotifyEPGChanges();
                        return currEv;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "GetChannelEPG error");

                return null;
            }
        }

        private async Task VideoLongPress()
        {
            MessagingCenter.Send($"{BaseViewModel.LongPressPrefix}enter", BaseViewModel.MSG_KeyDown);
        }

        private async Task LongPress(object item)
        {
            var ch = item as DVBTChannel;
            if (ch == null)
                return;

            _loggingService.Info($"Long press on channel {ch.Name})");

            try
            {
                if (ch != SelectedChannel)
                {
                    DoNotScrollToChannel = true;
                    SelectedChannel = ch;
                } else
                {
                    await ShowChannelMenu(ch);
                }
            }
            finally
            {
                DoNotScrollToChannel = false;
            }
        }

        public async Task ShowChannelMenu(DVBTChannel ch = null)
        {
            string title;

            if (ch == null)
            {
                ch = SelectedChannel;
            }

            if (ch == null)
            {
                title = "Menu";
            }
            else
            {
                if (PlayingChannel == null)
                {
                    title = ch.Name;
                } else
                {
                    title = PlayingChannel.Name;
                }
            }

            var actions = new List<string>();

            string selectedChannelDetailAction = "Detail...";

            if (ch != null)
            {
                if (PlayingChannel == null)
                {
                    if (RecordingChannel == null || RecordingChannel == ch)
                    {
                        actions.Add("Play");
                    }

                    actions.Add("Scan EPG");

                    if (RecordingChannel == null)
                    {
                        actions.Add("Delete");
                    }
                } else
                {
                    actions.Add("Stop");

                    actions.Add("Scan EPG");

                    if (PlayingChannelSubtitles.Count > 0)
                    {
                        actions.Add("Subtitles...");
                    }
                    if (PlayingChannelAudioTracks.Count > 0)
                    {
                        actions.Add("Audio track...");
                    }
                    if (PlayingChannelAspect.Width != -1)
                    {
                        actions.Add("Aspect ratio...");
                    }

                    actions.Add("Teletext...");
                }

                if (RecordingChannel == null)
                {
                    actions.Add("Record");
                } else
                {
                    actions.Add("Show record location");
                    actions.Add("Stop record");
                }

                if (SelectedChannel != null)
                {
                    if (PlayingChannel != null && SelectedChannel.FrequencyAndMapPID != PlayingChannel.FrequencyAndMapPID)
                    {
                        selectedChannelDetailAction = $"Detail ({SelectedChannel.Name})...";
                    } else
                    {
                        selectedChannelDetailAction = $"Detail...";
                    }
                    actions.Add(selectedChannelDetailAction);
                }

                if (ch.CurrentEventItem != null)
                {
                    if (!EPGDetailEnabled)
                    {
                        actions.Add("Show programme description");
                    } else
                    {
                        actions.Add("Hide programme description");
                    }
                }
            }

            actions.Add("Quit app");

            var action = await _dialogService.DisplayActionSheet(title, "Cancel", actions);

            switch (action)
            {
                case "Play":
                    MessagingCenter.Send(new PlayStreamInfo { Channel = SelectedChannel }, BaseViewModel.MSG_PlayStream);
                    break;
                case "Show programme description":
                    EPGDetailEnabled = true;
                    break;
                case "Hide programme description":
                    EPGDetailEnabled = false;
                    break;
                case "Stop":
                    MessagingCenter.Send("", BaseViewModel.MSG_StopStream);
                    break;
                case "Subtitles...":
                    await ShowSubtitlesMenu(ch);
                    break;
                case "Teletext...":
                    await TeletextMenu();
                    break;
                case "Audio track...":
                    await ShowAudioTrackMenu(ch);
                    break;
                case "Aspect ratio...":
                    await ShowAspectMenu(ch);
                    break;
                case "Scan EPG":
                    await ScanEPG(ch, false, false, 0, 5000);
                    break;
                case "Record":
                    MessagingCenter.Send(new PlayStreamInfo { Channel = SelectedChannel }, BaseViewModel.MSG_RecordStream);
                    break;
                case "Show record location":
                    await _dialogService.Information(RecordingFileName);
                    break;
                case "Stop record":
                    MessagingCenter.Send(string.Empty, BaseViewModel.MSG_StopRecord);
                    break;
                case "Delete":
                    await DeleteChannel(ch);
                    break;
                case "Quit app":
                    if (await _dialogService.Confirm("Are you sure to quit DVBT televizor?"))
                    {
                        MessagingCenter.Send(String.Empty, BaseViewModel.MSG_QuitApp);
                    }
                    break;
            }

            if (action == selectedChannelDetailAction)
            {
                MessagingCenter.Send(ch.ToString(), BaseViewModel.MSG_EditChannel);
            }
        }

        public async Task TeletextMenu()
        {
            string pageNumber = await _dialogService.GetNumberDialog("Set page number", "Teletext", _lastTeltetextPageNumber.ToString());
            int num;
            if (int.TryParse(pageNumber, out num))
            {
                _lastTeltetextPageNumber = num;
                MessagingCenter.Send(pageNumber, BaseViewModel.MSG_TeletextPageNumber);
            }
        }

        public async Task ShowAspectMenu(DVBTChannel ch)
        {
            var actions = new List<string>();

            actions.Add("16:9");
            actions.Add("4:3");
            actions.Add("Original");
            actions.Add("Fill");

            var action = await _dialogService.DisplayActionSheet("Aspect ratio", "Cancel", actions);

            if (action != "Cancel")
            {
                MessagingCenter.Send(action, BaseViewModel.MSG_ChangeAspect);
                MessagingCenter.Send($"Aspect ratio: {action}", BaseViewModel.MSG_ToastMessage);
            }
        }

        public async Task ShowAudioTrackMenu(DVBTChannel ch)
        {
            var actions = new List<string>();

            foreach (var kvp in PlayingChannelAudioTracks)
            {
                actions.Add(kvp.Value);
            }

            var action = await _dialogService.DisplayActionSheet("Audio track", "Cancel", actions);

            foreach (var kvp in PlayingChannelAudioTracks)
            {
                if (action == kvp.Value)
                {
                    MessagingCenter.Send($"Audio track: {kvp.Value}", BaseViewModel.MSG_ToastMessage);
                    MessagingCenter.Send(kvp.Key.ToString(), BaseViewModel.MSG_ChangeAudioTrackId);
                    break;
                }
            }
        }

        public async Task ShowSubtitlesMenu(DVBTChannel ch)
        {
            var actions = new List<string>();

            foreach (var kvp in PlayingChannelSubtitles)
            {
                actions.Add(kvp.Value);
            }

            var action = await _dialogService.DisplayActionSheet("Subtitles", "Cancel", actions);

            foreach (var kvp in PlayingChannelSubtitles)
            {
                if (action == kvp.Value)
                {
                    MessagingCenter.Send($"Subtitles: {kvp.Value}", BaseViewModel.MSG_ToastMessage);
                    MessagingCenter.Send(kvp.Key.ToString(), BaseViewModel.MSG_ChangeSubtitleId);
                    break;
                }
            }
        }

        private async Task DeleteChannel(DVBTChannel channel)
        {
            if (await _dialogService.Confirm($"Are you sure to delete channel {channel.Name}?"))
            {
                _loggingService.Info($"Deleting channel {channel.Name})");

                Channels.Remove(channel);
                await _channelService.SaveChannels(Channels);
                await Refresh();
            }
        }

        private void ShortPress(object item)
        {
            if (item != null && item is DVBTChannel)
            {
                // select and play

                //DoNotScrollToChannel = true;

                var ch = item as DVBTChannel;

                SelectedChannel = ch;

                _loggingService.Info($"Short press on channel {ch.Name})");

                MessagingCenter.Send(new PlayStreamInfo { Channel = ch }, BaseViewModel.MSG_PlayStream);
            }
        }

        public async Task SelectChannelByNumber(string number)
        {
            _loggingService.Info($"Selecting channel by number {number}");

            await Task.Run(
                () =>
                {
                    // looking for channel by its number:
                    foreach (var ch in Channels)
                    {
                        if (ch.Number == number)
                        {
                            SelectedChannel = ch;
                            break;
                        }
                    }
                });
        }

        public async Task<DVBTChannel> SelectChannelByFrequencyAndMapPID(string frequencyAndMapPID)
        {
            _loggingService.Info($"Selecting channel by frequency and mapPID {frequencyAndMapPID}");

            return await Task.Run<DVBTChannel>(
                () =>
                {
                    DVBTChannel firstChannel = null;
                    DVBTChannel selectChannel = null;

                    if (Channels.Count == 0)
                    {
                        SelectedChannel = null;
                        return null;
                    }

                    foreach (var ch in Channels)
                    {
                        if (firstChannel == null)
                        {
                            firstChannel = ch;
                        }

                        if (ch.FrequencyAndMapPID == frequencyAndMapPID)
                        {
                            selectChannel = ch;
                            break;
                        }
                    }

                    if (selectChannel == null)
                        selectChannel = firstChannel;

                    SelectedChannel = selectChannel;

                    return SelectedChannel;
                });
        }

        public string NoVideoTitle
        {
            get
            {
                if (PlayingChannel == null)
                {
                    if (SelectedChannel == null)
                        return null;

                    return SelectedChannel.Name;
                } else
                {
                    return PlayingChannel.Name;
                }
            }
        }

        public DVBTChannel SelectedChannel
        {
            get
            {
                _semaphoreSlim.WaitAsync();
                try
                {
                    return _selectedChannel;
                }
                finally
                {
                    _semaphoreSlim.Release();
                };
            }
            set
            {
                _semaphoreSlim.WaitAsync();
                try
                {
                    _selectedChannel = value;

                    OnPropertyChanged(nameof(SelectedChannel));
                    OnPropertyChanged(nameof(NoVideoTitle));
                    OnPropertyChanged(nameof(SelectedChannelEPGTitle));
                    OnPropertyChanged(nameof(SelectedChannelEPGDescription));
                    OnPropertyChanged(nameof(SelectedChannelEPGTimeStart));
                    OnPropertyChanged(nameof(SelectedChannelEPGTimeFinish));
                    OnPropertyChanged(nameof(SelectedChannelEPGProgress));
                    OnPropertyChanged(nameof(EPGProgressBackgroundColor));

                    if (value != null)
                    {
                        _config.SelectedChannelFrequencyAndMapPID = _selectedChannel.FrequencyAndMapPID;
                    }

                    NotifyEPGDetailVisibilityChange();
                }
                finally
                {
                    _semaphoreSlim.Release();
                };
            }
        }

        public bool TunningButtonVisible
        {
            get
            {
                if (_refreshCounter == 0)
                {
                    return false;
                }

                return (Channels == null || Channels.Count == 0);
            }
        }

        public async Task ShowActualPlayingMessage(PlayStreamInfo playStreamInfo = null)
        {
            if (playStreamInfo == null ||
                playStreamInfo.Channel == null)
            {
                if (SelectedChannel == null)
                    return;

                playStreamInfo = new PlayStreamInfo
                {
                    Channel = SelectedChannel
                };

                playStreamInfo.CurrentEvent = await GetChannelEPG(SelectedChannel);
            }

            var msg = playStreamInfo.ShortInfoWithoutChannelName ? "" : " \u25B6 " + playStreamInfo.Channel.Name;

            if (playStreamInfo.CurrentEvent != null && playStreamInfo.CurrentEvent.CurrentEventItem != null)
            {
                if (msg != "")
                {
                    msg += " - ";
                }
                msg += $"{playStreamInfo.CurrentEvent.CurrentEventItem.EventName}";
            }

            // showing signal percents only for the first time
            if (playStreamInfo.SignalStrengthPercentage > 0)
            {
                msg += $" (signal {playStreamInfo.SignalStrengthPercentage}%)";
                playStreamInfo.SignalStrengthPercentage = 0;
            }

            MessagingCenter.Send(msg, BaseViewModel.MSG_ToastMessage);
        }

        public async Task ScanEPG(DVBTChannel channel, bool showIfFound, bool silent, int msRunTimeOut = 5000, int msScanTimeOut = 5000)
        {
            if (channel == null)
            {
                channel = SelectedChannel;
                if (channel == null)
                    return;
            }

            _loggingService.Debug($"Scanning EPG for channel {channel}");

            if ((_playingChannel != null) && (_playingChannel != channel))
            {
                if (!silent)
                {
                    MessagingCenter.Send($"Cannot scan EPG (playing in progress)", BaseViewModel.MSG_ToastMessage);
                }
                return;
            }

            if ((_recordingChannel != null) && (_recordingChannel != channel))
            {
                if (!silent)
                {
                    MessagingCenter.Send($"Cannot scan EPG (recording in progress)", BaseViewModel.MSG_ToastMessage);
                }
                return;
            }

            if (!_driver.Connected)
            {
                if (!silent)
                {
                    MessagingCenter.Send($"Cannot scan EPG (device not connected)", BaseViewModel.MSG_ToastMessage);
                }
                return;
            }

            try
            {
                Task.Run(async () =>
                   {
                       if (_scanningEPG)
                       {
                           return;
                       }

                       try
                       {
                           _scanningEPG = true;

                           if (!silent)
                           {
                               MessagingCenter.Send($"Scanning EPG ....", BaseViewModel.MSG_LongToastMessage);
                           }

                           await Task.Delay(msRunTimeOut);

                           var justPlaying = ((_playingChannel == channel || _recordingChannel == channel));

                           if (!justPlaying)
                           {
                               var tuned = await _driver.TuneEnhanced(channel.Frequency, channel.Bandwdith, channel.DVBTType, new List<long>() { 0, 17, 18 }, false);

                               if (tuned.Result != SearchProgramResultEnum.OK)
                               {
                                   if (!silent)
                                   {
                                       MessagingCenter.Send($"Scanning EPG failed", BaseViewModel.MSG_ToastMessage);
                                   }
                                   return;
                               }
                           }

                           var res = await EIT.Scan(msScanTimeOut);

                           if (!justPlaying)
                           {
                               await _driver.Stop();
                           }

                           var msg = String.Empty;

                           if (!res.OK)
                           {
                               msg += "EPG scan failed";
                           }
                           else
                           {
                               msg += $"EPG scan completed";

                               await RefreshEPG();

                               if (showIfFound)
                               {
                                   var ev = await GetChannelEPG(channel);
                                   if (ev != null)
                                   {
                                       await ShowActualPlayingMessage(new PlayStreamInfo
                                       {
                                           Channel = channel,
                                           CurrentEvent = ev,
                                           ShortInfoWithoutChannelName = true
                                       });
                                   }
                               }
                           }

                           if (res.UnsupportedEncoding)
                           {
                               if (msg != String.Empty)
                               {
                                   msg += " (unsupported encoding!)";
                               }
                               else
                               {
                                   msg = "Unsupported encoding";
                               }
                           }

                           if (!string.IsNullOrEmpty(msg))
                           {
                               if (!silent)
                               {
                                   MessagingCenter.Send(msg, BaseViewModel.MSG_ToastMessage);
                               }
                           }
                       } finally
                       {
                           _scanningEPG = false;
                       }
                   });
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, $"EPG scan failed");

                if (!silent)
                {
                    MessagingCenter.Send($"EPG scan failed", BaseViewModel.MSG_ToastMessage);
                }
            }
        }

        public async Task Refresh()
        {
            string selectedChanneFrequencyAndMapPID = null;

            try
            {
                IsRefreshing = true;

                DoNotScrollToChannel = true;

                await _semaphoreSlim.WaitAsync();

                _loggingService.Info($"Refreshing channels");

                if (_refreshCounter == 0)
                {
                    selectedChanneFrequencyAndMapPID = _config.SelectedChannelFrequencyAndMapPID;
                }
                if (SelectedChannel != null)
                {
                    selectedChanneFrequencyAndMapPID = SelectedChannel.FrequencyAndMapPID;
                }

                Channels.Clear();

                ObservableCollection<DVBTChannel> channels = null;

                channels = await _channelService.LoadChannels();

                // sort chanels by number

                channels = new ObservableCollection<DVBTChannel>(channels.OrderBy(i => i.Number.PadLeft(4, '0')));

                // channels filter

                foreach (var ch in channels)
                {
                    if (ch.SimplifiedServiceType == DVBTServiceType.TV && !_config.ShowTVChannels)
                        continue;

                    if (ch.SimplifiedServiceType == DVBTServiceType.Radio && !_config.ShowRadioChannels)
                        continue;

                    if (ch.SimplifiedServiceType == DVBTServiceType.Other && !_config.ShowOtherChannels)
                        continue;

                    if (_recordingChannel != null &&
                        _recordingChannel.Frequency == ch.Frequency &&
                        _recordingChannel.Name == ch.Name &&
                        _recordingChannel.ProgramMapPID == ch.ProgramMapPID)
                    {
                        ch.Recording = true;
                    }

                    Channels.Add(ch);
                }

            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "Error while refreshing channels");
            }
            finally
            {
                _semaphoreSlim.Release();

                _refreshCounter++;
                IsRefreshing = false;

                OnPropertyChanged(nameof(Channels));
                OnPropertyChanged(nameof(TunningButtonVisible));
                OnPropertyChanged(nameof(SelectedChannelEPGTitle));
                OnPropertyChanged(nameof(SelectedChannelEPGDescription));
                OnPropertyChanged(nameof(SelectedChannelEPGTimeStart));
                OnPropertyChanged(nameof(SelectedChannelEPGTimeFinish));
                OnPropertyChanged(nameof(SelectedChannelEPGProgress));
                OnPropertyChanged(nameof(EPGProgressBackgroundColor));

                NotifyEPGDetailVisibilityChange();

                DoNotScrollToChannel = false;

                await SelectChannelByFrequencyAndMapPID(selectedChanneFrequencyAndMapPID);
            }
        }

        private async Task RefreshEPG()
        {
            //_loggingService.Debug($"Refreshing EPG");

            try
            {
                await _semaphoreSlim.WaitAsync();

                foreach (var channel in Channels)
                {
                    channel.ClearEPG();

                    var channelEv = EIT.GetEvent(DateTime.Now, channel.Frequency, channel.ProgramMapPID);
                    if (channelEv != null)
                    {
                        channel.SetCurrentEvent(channelEv);
                    }

                    channel.NotifyEPGChanges();
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "Refreshing EPG failed");
            }
            finally
            {
                _semaphoreSlim.Release();

                OnPropertyChanged(nameof(Channels));
                OnPropertyChanged(nameof(SelectedChannel));
                OnPropertyChanged(nameof(SelectedChannelEPGTitle));
                OnPropertyChanged(nameof(SelectedChannelEPGDescription));
                OnPropertyChanged(nameof(SelectedChannelEPGTimeStart));
                OnPropertyChanged(nameof(SelectedChannelEPGTimeFinish));
                OnPropertyChanged(nameof(SelectedChannelEPGProgress));
                OnPropertyChanged(nameof(EPGProgressBackgroundColor));
                NotifyEPGDetailVisibilityChange();
            }
        }

        public bool StandingOnStart
        {
            get
            {
                try
                {
                    _semaphoreSlim.WaitAsync();

                    if (SelectedChannel == null)
                        return true;

                    foreach (var ch in Channels)
                    {
                        if (ch == SelectedChannel)
                            return true;

                        return false;
                    }

                    return true;
                }
                finally
                {
                    _semaphoreSlim.Release();
                };
            }
        }

        public async Task SelectFirstOrLastChannel(bool first)
        {
            _loggingService.Info($"Selecting first/last channel");

            await Task.Run(
                async () =>
                {
                        if (Channels.Count == 0)
                            return;

                        if (first)
                        {
                            SelectedChannel = Channels[0];
                        }
                        else
                        {
                            SelectedChannel = Channels[Channels.Count - 1];
                        }
                });
        }

        public async Task SelectNextChannel(int step = 1)
        {
            _loggingService.Info($"Selecting next channel (step {step})");

            await Task.Run(
                () =>
                {

                    if (Channels.Count == 0)
                        return;

                    if (SelectedChannel == null)
                    {
                        SelectedChannel = Channels[0];
                    }
                    else
                    {
                        bool next = false;
                        var nextCount = 1;
                        for (var i = 0; i < Channels.Count; i++)
                        {
                            var ch = Channels[i];

                            if (next)
                            {
                                if (nextCount == step || i == Channels.Count - 1)
                                {
                                    SelectedChannel = ch;
                                    break;
                                }
                                else
                                {
                                    nextCount++;
                                }
                            }
                            else
                            {
                                if (ch == SelectedChannel)
                                {
                                    next = true;
                                }
                            }
                        }
                    }
                });
        }

        public async Task SelectPreviousChannel(int step = 1)
        {
            _loggingService.Info($"Selecting previous channel (step {step})");

            await Task.Run(
                () =>
                {

                    if (Channels.Count == 0)
                        return;

                    if (SelectedChannel == null)
                    {
                        SelectedChannel = Channels[Channels.Count - 1];
                    }
                    else
                    {
                        bool previous = false;
                        var previousCount = 1;

                        for (var i = Channels.Count - 1; i >= 0; i--)
                        {
                            if (previous)
                            {
                                if (previousCount == step || i == 0)
                                {
                                    SelectedChannel = Channels[i];
                                    break;
                                }
                                else
                                {
                                    previousCount++;
                                }
                            }
                            else
                            {
                                if (Channels[i] == SelectedChannel)
                                {
                                    previous = true;
                                }
                            }
                        }
                    }
                });
        }

        public string AudioIcon
        {
            get
            {
                return "Audio" + _animePos.ToString();
            }
        }

        private async Task Anime()
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

            try
            {
                OnPropertyChanged(nameof(AudioIcon));
            }
            catch
            {
                // UWP platform fix
            }
        }
    }
}
