using Xamarin.Forms;
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using LoggerService;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;
using System.Linq;

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

        private DVBTChannel _lastSelectedChannel = null;
        private DVBTChannel _selectedChannel;
        private DVBTChannel _recordingChannel;

        public bool DoNotScrollToChannel { get; set; } = false;

        public ObservableCollection<DVBTChannel> Channels { get; set; } = new ObservableCollection<DVBTChannel>();

        private PlayingStateEnum _playingState = PlayingStateEnum.Stopped;
        private DVBTChannel _playingChannel = null;

        private int _animePos = 2;
        private bool _animePosIncreasing = true;

        private bool _EPGDetailVisible = false;

        public enum SelectedPartEnum
        {
            ChannelsList = 0,
            EPGDetail = 1,
            ToolBar = 2
        }

        private SelectedPartEnum _selectedPart = SelectedPartEnum.ChannelsList;

        public MainPageViewModel(ILoggingService loggingService, IDialogService dialogService, IDVBTDriverManager driver, DVBTTelevizorConfiguration config, ChannelService channelService)
            : base(loggingService, dialogService, driver, config)
        {
            _channelService = channelService;

            RefreshCommand = new Command(async () => await Refresh());
            RefreshEPGCommand = new Command(async () => await RefreshEPG());
            LongPressCommand = new Command(async (itm) => await LongPress(itm));
            VideoLongPressCommand = new Command(async (itm) => await VideoLongPress());
            ShortPressCommand = new Command(ShortPress);
            ImportCommand = new Command(async (json) => await ImportList(json));

            AnimeIconCommand = new Command(async () => await Anime());

            BackgroundCommandWorker.RunInBackground(RefreshEPGCommand, 2, 10);
            BackgroundCommandWorker.RunInBackground(AnimeIconCommand, 1, 1);
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


        public bool DebugArrowVisible
        {
            get
            {
#if DEBUG
                return true;
#else
                return false;
#endif
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

        public bool EPGDetailVisible
        {
            get
            {
                // just debugging
                return true;

                return
                    _EPGDetailVisible &&
                    SelectedChannel != null &&
                    SelectedChannel.CurrentEventItem != null;
            }
            set
            {
                _EPGDetailVisible = value;
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

        public bool ShowServiceMenuToolItem
        {
            get
            {
                return _config.ShowServiceMenu;
            }
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
                    if (!ConfigViewModel.ChannelExists(chs, ch.Frequency, ch.ProgramMapPID))
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
                _loggingService.Error(ex, "Import failed");
                await _dialogService.Error($"Import failed");
            }
        }

        private async Task VideoLongPress()
        {

        }

        private async Task LongPress(object item)
        {
            var ch = item as DVBTChannel;
            if (ch == null)
                return;

            _loggingService.Info($"Long press on channel {ch.Name})");

            SelectedChannel = ch;
            await ShowChannelMenu(ch);
        }

        private async Task AnyKeyPressed(string key)
        {
            MessagingCenter.Send(key, BaseViewModel.MSG_KeyDown);
        }

        public async Task ShowChannelMenu(DVBTChannel ch = null)
        {
            if (ch == null)
            {
                ch = SelectedChannel;
            }

            if (ch == null)
            {
                await _dialogService.Information("No channel selected");
                return;
            }

            var actions = new List<string>();

            if (!ch.Recording)
            {
                actions.Add("Play");
                actions.Add("Scan EPG");
                actions.Add("Detail & edit");
                actions.Add("Record");
                actions.Add("Delete");
            }
            else
            {
                actions.Add("Show record location");
                actions.Add("Stop record");
            }

            var action = await _dialogService.DisplayActionSheet($"{ch.Name}", "Cancel", actions);

            switch (action)
            {
                case "Play":
                    await PlayChannel(ch);
                    break;
                case "Scan EPG":
                    await ScanEPG(ch);
                    break;
                case "Detail & edit":
                    MessagingCenter.Send(ch.ToString(), BaseViewModel.MSG_EditChannel);
                    break;
                case "Record":
                    await RecordChannel(ch, true);
                    break;
                case "Show record location":
                    await _dialogService.Information(_driver.RecordFileName);
                    break;
                case "Stop record":
                    await RecordChannel(ch, false);
                    break;
                case "Delete":
                    await DeleteChannel(ch);
                    break;
            }
        }

        private async Task RecordChannel(DVBTChannel channel, bool start)
        {
            if (channel == null)
            {
                channel = SelectedChannel;
                if (channel == null)
                    return;
            }

            _loggingService.Debug($"Recording channel {channel}: {start}");

            try
            {

                if (start)
                {
                    if (!_driver.Started)
                    {
                        MessagingCenter.Send($"Recording failed (device connection error)", BaseViewModel.MSG_ToastMessage);
                        return;
                    }

                    var playRes = await _driver.Play(channel.Frequency, channel.Bandwdith, channel.DVBTType, channel.PIDsArary, false);
                    if (!playRes.OK)
                    {
                        throw new Exception("Play returned false");
                    }

                    _recordingChannel = channel;
                    channel.Recording = true;

                    await _driver.StartRecording();

                    MessagingCenter.Send($"Recording started", BaseViewModel.MSG_ToastMessage);
                }
                else
                {
                    if (!_driver.Started)
                    {
                        MessagingCenter.Send($"Stop recording failed (device connection error)", BaseViewModel.MSG_ToastMessage);
                        return;
                    }

                    _driver.StopRecording();
                    await _driver.Stop();

                    _recordingChannel = null;
                    channel.Recording = false;

                    MessagingCenter.Send($"Recording stopped", BaseViewModel.MSG_ToastMessage);
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);

                MessagingCenter.Send($"Start/stop recording failed (device connection error)", BaseViewModel.MSG_ToastMessage);

                return;
            }

            channel.NotifyRecordingLabelChange();
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

                DoNotScrollToChannel = true;

                var ch = item as DVBTChannel;

                SelectedChannel = ch;

                _loggingService.Info($"Short press on channel {ch.Name})");

                Task.Run(async () => await PlayChannel(ch));
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

        public async Task SelectChannelByFrequencyAndMapPID(string frequencyAndMapPID)
        {
            _loggingService.Info($"Selecting channel by frequency and mapPID {frequencyAndMapPID}");

            await Task.Run(
                () =>
                {
                    DVBTChannel firstChannel = null;
                    DVBTChannel selectChannel = null;

                    if (Channels.Count == 0)
                        return;

                    foreach (var ch in Channels)
                    {
                        if (firstChannel == null)
                        {
                            firstChannel = ch;
                        }

                        if (ch.Frequency.ToString() + ch.ProgramMapPID.ToString() == frequencyAndMapPID)
                        {
                            selectChannel = ch;
                            break;
                        }
                    }

                    if (selectChannel == null)
                        selectChannel = firstChannel;

                    SelectedChannel = selectChannel;
                });
        }

        public string SelectedChannelName
        {
            get
            {
                if (SelectedChannel == null)
                return null;

                return SelectedChannel.Name;
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
                    OnPropertyChanged(nameof(SelectedChannelName));
                    OnPropertyChanged(nameof(SelectedChannelEPGTitle));
                    OnPropertyChanged(nameof(SelectedChannelEPGDescription));
                    OnPropertyChanged(nameof(SelectedChannelEPGTimeStart));
                    OnPropertyChanged(nameof(SelectedChannelEPGTimeFinish));
                    OnPropertyChanged(nameof(SelectedChannelEPGProgress));
                    OnPropertyChanged(nameof(EPGProgressBackgroundColor));
                    OnPropertyChanged(nameof(EPGDetailVisible));
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
                return Channels.Count == 0;
            }
        }

        public async Task ScanEPG(DVBTChannel channel)
        {
            if (channel == null)
            {
                channel = SelectedChannel;
                if (channel == null)
                    return;
            }

            if (_recordingChannel != null)
            {
                MessagingCenter.Send($"Cannot scan EPG (recording in progress)", BaseViewModel.MSG_ToastMessage);
                return;
            }

            _loggingService.Debug($"Scanning EPG for channel {channel}");

            try
            {
                if (!_driver.Started)
                {
                    MessagingCenter.Send($"Cannot scan EPG (device connection error)", BaseViewModel.MSG_ToastMessage);
                    return;
                }

                await Task.Run(async () =>
                   {
                       MessagingCenter.Send($"Scanning EPG ....", BaseViewModel.MSG_LongToastMessage);

                       var tuned = await _driver.TuneEnhanced(channel.Frequency, channel.Bandwdith, channel.DVBTType);

                       if (tuned.Result != SearchProgramResultEnum.OK )
                       {
                           MessagingCenter.Send($"Scanning EPG failed", BaseViewModel.MSG_ToastMessage);
                           return;
                       }

                       // setting PID 18 + PSI for each channel in the same multiplex:

                       var res = await _driver.ScanEPG(channel.Frequency, 5000);

                       await _driver.Stop();

                       await RefreshEPG();

                       var msg = String.Empty;

                       if (!res.OK)
                       {
                           msg += "EPG scan failed";
                       }

                       if (res.UnsupportedEncoding)
                       {
                           if (msg != String.Empty)
                           {
                               msg += ", unsupported encoding found";
                            }
                           else
                           {
                               msg = "Unsupported encoding found";
                           }
                       }

                       if (!string.IsNullOrEmpty(msg))
                       {
                           MessagingCenter.Send(msg, BaseViewModel.MSG_ToastMessage);
                       }
                   });
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, $"EPG scan failed");

                MessagingCenter.Send($"EPG scan failed", BaseViewModel.MSG_ToastMessage);
            }
        }

        public async Task PlayChannel(DVBTChannel channel = null)
        {
            if (channel == null)
            {
                channel = SelectedChannel;
                if (channel == null)
                    return;
            }

            var playInfo = new PlayStreamInfo
            {
                Channel = channel
            };

            if (_recordingChannel != null)
            {
                MessagingCenter.Send($"Playing {channel.Name} failed (recording in progress)", BaseViewModel.MSG_ToastMessage);
                return;
            }

            _loggingService.Debug($"Playing channel {channel}");

            try
            {
                if (!_driver.Started)
                {
                    MessagingCenter.Send($"Playing {channel.Name} failed (device connection error)", BaseViewModel.MSG_ToastMessage);
                    return;
                }

                IsRefreshing = true;

                var playRes = await _driver.Play(channel.Frequency, channel.Bandwdith, channel.DVBTType, channel.PIDsArary);
                if (!playRes.OK)
                {
                    throw new Exception("Play returned false");
                }

                playInfo.SignalStrengthPercentage = playRes.SignalStrengthPercentage;

                var eitManager = _driver.GetEITManager(channel.Frequency);
                if (eitManager != null)
                {
                    playInfo.CurrentEvent = eitManager.GetEvent(DateTime.Now, Convert.ToInt32(channel.ProgramMapPID));
                }

                MessagingCenter.Send(playInfo, BaseViewModel.MSG_PlayStream);

            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, $"Playing {channel.Name} failed");

                MessagingCenter.Send($"Playing {channel.Name} failed", BaseViewModel.MSG_ToastMessage);
            }
            finally
            {
                IsRefreshing = false;
            }

        }

        private async Task Refresh()
        {
            string selectedChanneFrequencyAndMapPID = null;

            try
            {
                IsRefreshing = true;

                await _semaphoreSlim.WaitAsync();

                DoNotScrollToChannel = true;

                _loggingService.Info($"Refreshing channels");

                if (SelectedChannel != null)
                {
                    selectedChanneFrequencyAndMapPID = SelectedChannel.Frequency.ToString() + SelectedChannel.ProgramMapPID.ToString();
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

                IsRefreshing = false;

                OnPropertyChanged(nameof(Channels));
                OnPropertyChanged(nameof(TunningButtonVisible));
                OnPropertyChanged(nameof(SelectedChannelEPGTitle));
                OnPropertyChanged(nameof(SelectedChannelEPGDescription));
                OnPropertyChanged(nameof(SelectedChannelEPGTimeStart));
                OnPropertyChanged(nameof(SelectedChannelEPGTimeFinish));
                OnPropertyChanged(nameof(SelectedChannelEPGProgress));
                OnPropertyChanged(nameof(EPGProgressBackgroundColor));
                OnPropertyChanged(nameof(EPGDetailVisible));

                await SelectChannelByFrequencyAndMapPID(selectedChanneFrequencyAndMapPID);

                DoNotScrollToChannel = false;
            }
        }

        private async Task RefreshEPG()
        {
            //_loggingService.Info($"Refreshing EPG");

            try
            {
                await _semaphoreSlim.WaitAsync();

                foreach (var channel in Channels)
                {
                    channel.ClearEPG();

                    var eitM = _driver.GetEITManager(channel.Frequency);

                    if (eitM != null)
                    {
                        var evs = eitM.GetEvents(DateTime.Now, 2);
                        var programMapPID = Convert.ToInt32(channel.ProgramMapPID);
                        if (evs != null && evs.ContainsKey(programMapPID))
                        {
                            if (evs[programMapPID] != null)
                            {
                                if (evs[programMapPID].Count > 0)
                                    channel.CurrentEventItem = evs[programMapPID][0];

                                if (evs[programMapPID].Count > 1)
                                    channel.NextEventItem = evs[programMapPID][1];

                                channel.NotifyEPGChanges();
                            }
                        }
                    }
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
                OnPropertyChanged(nameof(EPGDetailVisible));
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
