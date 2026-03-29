using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.DBManager;
using DVBTTelevizor.MAUI.Messages;
using DVBTTelevizor.TV;
using LibVLCSharp.Shared;
using LoggerService;
using MPEGTS;
using Newtonsoft.Json;
using Plugin.InAppBilling;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using System.Windows.Input;

namespace DVBTTelevizor.MAUI
{
    public class MainViewModel : BaseViewModel
    {
        private static SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        public ObservableCollection<Channel> Channels { get; set; } = new ObservableCollection<Channel>();

        public Size PlayingChannelAspect { get; set; } = new Size(-1, -1);

        public EITManager EIT { get; set; }
        public PIDManager PID { get; set; }

        public bool IsRecording { get; set; } = false;

        private SledovaniTV.SledovaniTV _iptv;

        private PlayingStateEnum _playingState = PlayingStateEnum.Stopped;
        private ListViewSelector? _listViewSelector = null;
        private bool? _EPGDetailVisibleLastValue = null;

        private bool _EPGDetailEnabled = true;
        private bool _EPGDetailFocused = false;

        private Channel? _selectedChannel;
        private Channel _playingChannel;
        private Channel _recordingChannel;
        private bool _scanningEPG = false;

        private bool _refreshing = false;
        private bool _refreshed = false;

        private bool _doNotAutomaticallyShowEPGDetail = false;

        private bool? _videoStackLayoutvisible = null;

        private BackgroundWorker _recordingBackgroundWorker = new BackgroundWorker();

        public ICommand CommandPlay { get; set; }
        public ICommand CommandTune { get; set; }
        public ICommand CommandSettings { get; set; }
        public ICommand CommandAbout { get; set; }
        public ICommand CommandCloseMenu { get; set; }
        public ICommand CommandDriverState { get; set; }
        public ICommand CommandShowMenu { get; set; }
        public ICommand CommandInstallDriver { get; set; }
        public ICommand CommandQuit { get; set; }
        public ICommand RefreshCommand { get; set; }
        public Command CommandScanEPG { get; set; }

        public bool MainLayoutVisible { get; set; } = true;

        public MainViewModel(ILoggingService loggingService, IDriverConnector driver, SledovaniTV.SledovaniTV iptv, ITVConfiguration tvConfiguration, IPublicDirectoryProvider publicDirectoryProvider)
            :base(loggingService,driver, tvConfiguration, publicDirectoryProvider)
        {
            EIT = new EITManager(loggingService, publicDirectoryProvider, driver);
            PID = new PIDManager(loggingService, publicDirectoryProvider, driver);

            _iptv = iptv;

            _listViewSelector = new ListViewSelector(Channels);
            _listViewSelector.OnChannelChanged += delegate
            {
                if (_configuration != null && SelectedChannel != null)
                {
                    _configuration.LastSelectedChannelUniqueIdentifier = SelectedChannel.UniqueIdentifier;
                }
            };

            SubscribeMessages();

            InitCommands();

            Task.Run(async () =>
            {
                await CheckPendingPurchasesAsync();
            });

            _recordingBackgroundWorker.DoWork += _recordingBackgroundWorker_DoWork;

            BackgroundCommandWorker.RunInBackground(CommandScanEPG, 10, 6);
        }

        private void InitCommands()
        {
            CommandTune = new Command(() =>
            {
                WeakReferenceMessenger.Default.Send(new ShowTuneMessage(String.Empty));
            });

            CommandQuit = new Command(() =>
            {
                WeakReferenceMessenger.Default.Send(new QuitAppMessage(String.Empty));
            });

            CommandAbout = new Command(() =>
            {
                WeakReferenceMessenger.Default.Send(new ShowAboutMessage(String.Empty));
            });

            CommandShowMenu = new Command(() =>
            {
                WeakReferenceMessenger.Default.Send(new ShowMenuMessage(String.Empty));
            });

            CommandInstallDriver = new Command(() =>
            {
                WeakReferenceMessenger.Default.Send(new InstallDriverMessage(String.Empty));
            });

            CommandDriverState = new Command(() =>
            {
                WeakReferenceMessenger.Default.Send(new ShowDriverStateMessage(String.Empty));
            });

            CommandShowMenu = new Command(() =>
            {
                WeakReferenceMessenger.Default.Send(new ShowMenuMessage(String.Empty));
            });

            CommandScanEPG = new Command(() =>
            {
                Task.Run(async () =>
                {
                    await RefreshEPG();
                });
            });

            RefreshCommand = new Command(() =>
            {
                Task.Run(async () =>
                {
                    await RefreshChannels();
                });
            });
        }

        private void SubscribeMessages()
        {
            WeakReferenceMessenger.Default.Register<DriverHasBeenConnectedMessage>(this, (r, m) =>
            {
                ConnectDriver(m.Value);
            });

            WeakReferenceMessenger.Default.Register<DVBTDriverConnectionFailedMessage>(this, (r, m) =>
            {
                ConnectDriverFailed(m.Value);
            });

            WeakReferenceMessenger.Default.Register<DVBTDriverNotInstalledMessage>(this, (r, m) =>
            {
                DriverNotInstalled();
            });

            WeakReferenceMessenger.Default.Register<RTLSDRDriverNotInstalledMessage>(this, (r, m) =>
            {
                DriverNotInstalled();
            });

            WeakReferenceMessenger.Default.Register<DisConnectMessage>(this, (r, m) =>
            {
                DisconnectDriver();
            });

            WeakReferenceMessenger.Default.Register<ChannelsChangedMessage>(this, (r, m) =>
            {
                Task.Run(async () =>
                {
                    await RefreshChannels();
                });
            });

            WeakReferenceMessenger.Default.Register<ClearCacheMessage>(this, (r, m) =>
            {
                Task.Run(async () =>
                {
                    await ClearCache();
                });
            });
        }

        public async Task ClearCache()
        {
            _loggingService.Debug($"ClearCache");

            try
            {
                EIT.Clear();
                PID.Clear();

                _loggingService.Debug($"Cache cleared");

                WeakReferenceMessenger.Default.Send(new ToastMessage($"EPG and channel cache cleared".Translated()));
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
            finally
            {
                await RefreshEPG();
            }
        }

        public async Task<EPGCurrentEvent> GetChannelEPG(Channel channel)
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
                        channel.NotifyChanges();
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

        public async Task ScanEPG(Channel? channel, bool showIfFound, bool silent, int msRunTimeOut = 5000, int msScanTimeOut = 5000)
        {
            _loggingService.Debug($"ScanEPG {channel?.Name}");

            if (channel == null)
            {
                channel = SelectedChannel;
                if (channel == null)
                    return;
            }

            _loggingService.Debug($"Scanning EPG for channel {channel}");

            if (channel.ChannelType == ChannelTypeEnum.SledovaniTV)
            {
                return;
            }

            if ((_playingChannel != null) && (_playingChannel != channel))
            {
                if (!silent)
                {
                    WeakReferenceMessenger.Default.Send(new ToastMessage($"Cannot scan EPG (playing in progress)".Translated()));
                }
                return;
            }

            if ((_recordingChannel != null) && (_recordingChannel != channel))
            {
                if (!silent)
                {
                    WeakReferenceMessenger.Default.Send(new ToastMessage($"Cannot scan EPG (recording in progress)".Translated()));
                }
                return;
            }

            if (!_driver.Connected)
            {
                if (!silent)
                {
                    WeakReferenceMessenger.Default.Send(new ToastMessage($"Cannot scan EPG (device not connected)".Translated()));
                }
                return;
            }

            try
            {
                await Task.Run(async () =>
                {
                    await ScanEPGInternal(channel, showIfFound, silent, msRunTimeOut, msScanTimeOut);
                });
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, $"EPG scan failed");

                if (!silent)
                {
                    WeakReferenceMessenger.Default.Send(new ToastMessage($"EPG scan failed".Translated()));
                }
            }
        }

        private async Task ScanEPGInternal(Channel channel, bool showIfFound, bool silent, int msRunTimeOut = 5000, int msScanTimeOut = 5000)
        {
            _loggingService.Info("ScanEPGInternal");

            if (_scanningEPG)
            {
                return;
            }

            try
            {
                _scanningEPG = true;

                if (!silent)
                {
                    WeakReferenceMessenger.Default.Send(new ToastMessage($"Scanning EPG ....".Translated()));
                }

                await Task.Delay(msRunTimeOut);

                var justPlaying = ((_playingChannel == channel || _recordingChannel == channel));

                if (!justPlaying)
                {
                    var tuned = await _driver.TuneEnhanced(channel.Frequency, channel.Bandwdith, (int)channel.ChannelType, false);

                    if (tuned.Result != DVBTDriverSearchProgramResultEnum.OK)
                    {
                        if (!silent)
                        {
                            WeakReferenceMessenger.Default.Send(new ToastMessage($"Scanning EPG failed".Translated()));
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
                    msg += "EPG scan failed".Translated();
                }
                else
                {
                    msg += $"EPG scan completed".Translated();

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

                if (!string.IsNullOrEmpty(msg))
                {
                    if (!silent)
                    {
                        WeakReferenceMessenger.Default.Send(new ToastMessage(msg));
                    }
                }
            }
            finally
            {
                _scanningEPG = false;
            }
        }

        private async Task RefreshEPG()
        {
            _loggingService.Debug($"RefreshEPG");

            try
            {
                IsRefreshing = true;
                await _semaphoreSlim.WaitAsync();

                foreach (var channel in Channels)
                {
                    var channelEv = EIT.GetEvent(DateTime.Now, channel.Frequency, channel.ProgramMapPID);
                    if (channelEv != null)
                    {
                        channel.ClearEPG();
                        channel.SetCurrentEvent(channelEv);
                        channel.NotifyChanges();
                    }
                }

                if (_configuration.SledovaniTVEnabled)
                {
                    var epg = await _iptv.GetActualEPG();

                    foreach (var channel in Channels)
                    {
                        if ((channel.ChannelType == ChannelTypeEnum.SledovaniTV) &&
                            (channel.ChannelId != null) &&
                            (epg.ContainsKey(channel.ChannelId))
                            )
                        {
                            channel.ClearEPG();
                            channel.SetCurrentEvent(epg[channel.ChannelId]);
                            channel.NotifyChanges();
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

                NotifyChannelChange();
                NotifyEPGDetailVisibilityChange();

                IsRefreshing = false;
            }
        }


        public async Task SledovaniTVUpdateChannelUrls(ObservableCollection<Channel> channels)
        {
            _loggingService.Info("SledovaniTVUpdateChannelUrls");

            if (!_configuration.SledovaniTVEnabled)
                return;

            try
            {
                var iptvChannels = await _iptv.GetChannels();

                var updated = false;

                foreach (var iptvChannel in iptvChannels)
                {
                    // searching for online channel with the same id
                    foreach (var channel in channels)
                    {
                        if ((channel.ChannelType == ChannelTypeEnum.SledovaniTV) && (channel.ChannelId == iptvChannel.ChannelId))
                        {
                            // update

                            if (iptvChannel.Url != null)
                            {
                                updated = true;
                                channel.Url = iptvChannel.Url;
                            }
                            break;
                        }
                    }
                }

                if (updated)
                {
                    _loggingService.Info("SledovaniTVUpdateChannelUrls: saving channels");
                    _configuration.SaveChannels(channels);
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
        }

        public async Task RefreshChannels()
        {
            _loggingService.Debug($"Refreshing channels");

            string? uniqueIdentifier = null;
            Channel? firstChannel = null;
            Channel? lastChannel = null;
            Channel? channelToSelect = null;

            try
            {
                IsRefreshing = true;

                if (SelectedChannel != null)
                {
                    uniqueIdentifier = SelectedChannel.UniqueIdentifier;
                    SelectedChannel = null;
                }

                await _semaphoreSlim.WaitAsync();

                var channels = _configuration.GetChannels();

                _loggingService.Debug($"Clearing channels");

                var channelsToAdd = new ObservableCollection<Channel>();

                List<string> filteredChannels = new List<string>();
                if (!string.IsNullOrWhiteSpace(_configuration.FilteredMultiplexes))
                {
                    foreach (var f in _configuration.FilteredMultiplexes.Split(";"))
                    {
                        filteredChannels.Add(f);
                    }
                }

                foreach (var channel in channels)
                {
                    // apply filter:
                    if (!_configuration.ShowTVChannels && channel.ServiceType == DVBTDriverServiceType.TV)
                        continue;

                    if (!_configuration.ShowRadioChannels && channel.ServiceType == DVBTDriverServiceType.Radio)
                        continue;

                    if (!_configuration.ShowOtherChannels && channel.ServiceType == DVBTDriverServiceType.Other)
                        continue;

                    if (!_configuration.ShowNonFreeChannels && channel.NonFree)
                        continue;

                    if (channel.ProviderName != null)
                    {
                        if (filteredChannels.Contains(channel.ProviderName.Replace(";", ":")))
                            continue;
                    }

                    var ch = channel.Clone();
                    ch.Selected = false;

                    if (firstChannel == null)
                    {
                        firstChannel = ch;
                    }

                    if (uniqueIdentifier == ch.UniqueIdentifier)
                    {
                        channelToSelect = ch;
                    }

                    if (_configuration.LastSelectedChannelUniqueIdentifier == ch.UniqueIdentifier)
                    {
                        lastChannel = ch;
                    }

                    _loggingService.Debug($"Adding channel {ch.Name}");

                    channelsToAdd.Add(ch);
                }

                if (channelToSelect == null)
                {
                    if (lastChannel != null)
                    {
                        channelToSelect = lastChannel;
                    } else
                    if (firstChannel != null)
                    {
                        channelToSelect = firstChannel;
                    }
                }

                await SledovaniTVUpdateChannelUrls(channels);

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    //Channels.Clear();
                    Channels = channelsToAdd;
                    _listViewSelector?.SetChannels(Channels);

                    SelectedChannel = channelToSelect;

                    NotifyEPGDetailVisibilityChange();

                    IsRefreshing = false;
                    Refreshed = true;

                    NotifyChannelChange();
                });

                _loggingService.Debug($"Channels refreshed");
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "Refreshing channels failed");
            }
            finally
            {
                _semaphoreSlim.Release();

                WeakReferenceMessenger.Default.Send(new SelectedChannelChangedMessage(channelToSelect));

            }
        }

        public async Task SelectFirstChannel()
        {
            _loggingService.Info($"Selecting first channel");

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                _listViewSelector?.SelectFirstChannel();
                NotifyChannelChange();
            });
        }

        public async Task SelectLastChannel()
        {
            _loggingService.Info($"Selecting last channel");

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                _listViewSelector?.SelectLastChannel();
                NotifyChannelChange();
            });
        }

        public async Task<Channel?> SelectChannelByNumber(string num)
        {
            _loggingService.Info($"SelectChannelByNumber {num}");

            return await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var ch  = _listViewSelector?.SelectChannelByNumber(num);
                NotifyChannelChange();
                return ch;
            });
        }

        public void SelectNextChannel()
        {
            _loggingService.Info($"Selecting next channel");

            _listViewSelector?.SelectNextChannel();
            NotifyChannelChange();

            _loggingService.Info($"... selected");
        }

        public void SelectPreiousChannel()
        {
            _loggingService.Info($"Selecting previous channel");

            _listViewSelector?.SelectPreviousChannel();
            NotifyChannelChange();
        }

        public Channel? GetChannelByUniqueidentifier(string uniqueidentifier)
        {
            _loggingService.Info($"Selecting channel by unique identifier {uniqueidentifier}");

            if (String.IsNullOrWhiteSpace(uniqueidentifier))
            {
                return null;
            }

            if (Channels.Count == 0)
            {
                return null;
            }

            foreach (var ch in Channels)
            {

                if (ch.UniqueIdentifier == uniqueidentifier)
                {
                    return ch;
                }
            }

            return null;
        }

        public async Task Import(string filename)
        {
            try
            {
                _loggingService.Info($"Importing channels from file");

                if (!File.Exists(filename))
                {
                    WeakReferenceMessenger.Default.Send(new ToastMessage("File {0} not found".Translated(filename)));
                    return;
                }

                var count = 0;

                foreach (var ch in
                    JsonConvert.DeserializeObject<ObservableCollection<Channel>>(File.ReadAllText(filename)))
                {
                    if (!ch.ChannelExists(Channels))
                    {
                        ch.Number = TuningProgressPageViewModel.GetNextFreeChannelNumber(Channels);
                        Channels.Add(ch);
                        count++;
                    }
                }

                _configuration.SaveChannels(Channels);

                await RefreshChannels();

                WeakReferenceMessenger.Default.Send(new ToastMessage("Imported channels count: {0}".Translated(count.ToString())));
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "Import failed");
                WeakReferenceMessenger.Default.Send(new ToastMessage("Import failed".Translated()));
            }
        }

        public bool EPGDetailFocused
        {
            get
            {
                return _EPGDetailFocused;
            }
            set
            {
                _EPGDetailFocused = value;

                NotifyEPGDetailVisibilityChange();
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

                NotifyEPGDetailVisibilityChange();
            }
        }

        public string EPGDetailGridLabelTextColor
        {
            get
            {
                if (EPGDetailFocused)
                {
                    return "White";
                } else
                {
                    return "#41b3ff";
                }
            }
        }

        public string EPGDetailGridLabelBackgroundColor
        {
            get
            {
                if (EPGDetailFocused)
                {
                    return "#007cd2";
                }
                else
                {
                    return "Transparent";
                }
            }
        }

        private void NotifyEPGDetailVisibilityChange()
        {
            OnPropertyChanged(nameof(EPGDetailVisible));
            OnPropertyChanged(nameof(EPGDetailFocused));
            OnPropertyChanged(nameof(EPGDetailGridLabelTextColor));
            OnPropertyChanged(nameof(EPGDetailGridLabelBackgroundColor));

            if (!_EPGDetailVisibleLastValue.HasValue || _EPGDetailVisibleLastValue.Value != EPGDetailVisible)
            {
                _EPGDetailVisibleLastValue = EPGDetailVisible;
                WeakReferenceMessenger.Default.Send(new RefreshGUIMessage(String.Empty));
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

        public bool TuneChannelsButtonVisible
        {
            get
            {
                //return false;
                return
                    (Channels.Count == 0) &&
                    Refreshed &&
                    (_driver!= null) &&
                    _driver.DriverInstalled;
            }
        }

        public bool InstallDriverButtonVisible
        {
            get
            {
                return
                    (Channels.Count == 0) &&
                    Refreshed &&
                    (_driver != null) &&
                    !_driver.DriverInstalled;
            }
        }

        public void UpdateDriverState()
        {
            NotifyChange();

            WeakReferenceMessenger.Default.Send(new DVBTDriverStateChangedMessages(String.Empty));
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

                //playStreamInfo.CurrentEvent = await GetChannelEPG(SelectedChannel);
            }

            var msg = playStreamInfo.ShortInfoWithoutChannelName ? "" : " \u25B6 " + playStreamInfo.Channel.Name;

            EventItem ev = null;
            if (playStreamInfo.CurrentEvent != null && playStreamInfo.CurrentEvent.CurrentEventItem != null)
            {
                ev = playStreamInfo.CurrentEvent.CurrentEventItem;
            }
            if (ev == null && (SelectedChannel != null))
            {
                ev = SelectedChannel.CurrentEventItem;
            }

            if (ev != null)
            {
                if (msg != "")
                {
                    msg += " - ";
                }
                msg += $"{ev.EventName}";
            }

            // showing signal percents only for the first time
            if (playStreamInfo.SignalStrengthPercentage > 0)
            {
                msg += Environment.NewLine + "(signal {0}%)".Translated(playStreamInfo.SignalStrengthPercentage.ToString());
                playStreamInfo.SignalStrengthPercentage = 0;
            }

            WeakReferenceMessenger.Default.Send(new ToastMessage(msg));
        }

        public void SetVideoStackLayoutvisible(bool? value)
        {
            _videoStackLayoutvisible = value;

            OnPropertyChanged(nameof(VideoStackLayoutVisible));
            OnPropertyChanged(nameof(NoVideoStackLayoutVisible));
        }

        public bool VideoStackLayoutVisible
        {
            get
            {
                return !_videoStackLayoutvisible.HasValue ? false : _videoStackLayoutvisible.Value;
            }
        }

        public bool NoVideoStackLayoutVisible
        {
            get
            {
                return !_videoStackLayoutvisible.HasValue ? false : !_videoStackLayoutvisible.Value;
            }
        }

        public void NotifyChannelChange()
        {
            //_loggingService.Info($"NotifyChannelChange (Current channel: {SelectedChannel.UniqueIdentifier}, thread id:{Thread.CurrentThread.ManagedThreadId})");

            if (SelectedChannel?.CurrentEventItem != null &&
                _playingState != PlayingStateEnum.Playing &&
                !_doNotAutomaticallyShowEPGDetail)
            {
                EPGDetailEnabled = true;
            }

            OnPropertyChanged(nameof(SelectedChannel));
            OnPropertyChanged(nameof(NoVideoTitle));
            OnPropertyChanged(nameof(SelectedChannelEPGTitle));
            OnPropertyChanged(nameof(SelectedChannelEPGDescription));
            OnPropertyChanged(nameof(SelectedChannelEPGTimeStart));
            OnPropertyChanged(nameof(SelectedChannelEPGTimeFinish));
            OnPropertyChanged(nameof(SelectedChannelEPGProgress));
            OnPropertyChanged(nameof(EPGProgressBackgroundColor));
            OnPropertyChanged(nameof(RecordingLabel));
            OnPropertyChanged(nameof(ChannelsListViewVisible));
            OnPropertyChanged(nameof(TuneChannelsButtonVisible));
            OnPropertyChanged(nameof(ChannelIcon));
            OnPropertyChanged(nameof(PlayingChannel));
            OnPropertyChanged(nameof(Channels));
        }

        public async void DisconnectDriver()
        {
            await _driver.Disconnect();

            UpdateDriverState();
        }


        /// <summary>
        /// Called only from one single place -> on message DriverHasBeenConnectedMessage received
        /// </summary>
        /// <param name="config"></param>
        private void ConnectDriver(DVBTDriverConfiguration config)
        {
            _loggingService.Info("Connecting device: " + config.DeviceName);

            if (_driver.Connected)
                return;

            _driver.DriverInstalled = true;

            WeakReferenceMessenger.Default.Send(new ToastMessage("Device found: {0}".Translated(config.DeviceName)));

            _driver.Configuration = config;
            _driver.PublicDirectory = _publicDirectory;
            _driver.Connect();

            UpdateDriverState();
        }

        private void ConnectDriverFailed(string message)
        {
            _loggingService.Info($"Connection failed: {message}");

            _driver.DriverInstalled = true;

            WeakReferenceMessenger.Default.Send(new ToastMessage("Connection failed: {0}".Translated(message)));

            UpdateDriverState();
        }

        private void DriverNotInstalled()
        {
            _loggingService.Info($"Driver is not installed");

            _driver.DriverInstalled = false;

            WeakReferenceMessenger.Default.Send(new ToastMessage("Driver is not installed".Translated()));

            UpdateDriverState();
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

        public string DriverIconImage
        {
            get
            {
                if (_driver == null ||!_driver.DriverInstalled)
                {
                    return "donglered.png";
                }


                if (_driver.Connected)
                {
                    return "donglegreen.png";

                }

                return "dongleorange.png";
            }
        }


        public string TuneIconImage
        {
            get
            {
                return "tune.png";
            }
        }


        public string SettingsIconImage
        {
            get
            {
                return "settings.png";
            }
        }

        public string MenuIconImage
        {
            get
            {
                return "menu.png";
            }
        }

        public Channel? SelectedChannel
        {
            get
            {
                _semaphoreSlim.WaitAsync();
                try
                {
                    return _listViewSelector?.GetSelectedChannel();
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
                    _listViewSelector?.SetSelectedChannel(value);

                    NotifyChannelChange();
                }
                finally
                {
                    _semaphoreSlim.Release();
                };
            }
        }

        public Channel PlayingChannel
        {
            get { return _playingChannel; }
            set
            {
                _playingChannel = value;
            }
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
                }
                else
                {
                    return PlayingChannel.Name;
                }
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
                    return Color.FromRgba(0, 0, 0, 255);

                return Color.FromRgba(255, 255, 255, 255);
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

        public string ChannelIcon
        {
            get
            {
                if (_playingChannel == null)
                {
                   if (SelectedChannel != null)
                   {
                       return SelectedChannel.Icon;
                   } else
                   {
                       return "other.png";
                    }
                }

                return _playingChannel.Icon;
            }
        }

        public Channel RecordingChannel
        {
            get
            {
                return _recordingChannel;
            }
            set
            {
                _recordingChannel = value;

                foreach (var ch in Channels)
                {
                    ch.Recording = false;
                }

                if (_recordingChannel != null)
                {
                    _recordingChannel.Recording = true;
                }

                OnPropertyChanged(nameof(RecordingLabel));
            }
        }

        public bool ChannelsListViewVisible
        {
            get
            {
                return Channels.Count > 0;
            }
        }

        public bool IsRefreshing
        {
            get
            {
                return _refreshing;
            }
            set
            {
                _refreshing = value;
                NotifyChange();
            }
        }

        private void NotifyChange()
        {
            OnPropertyChanged(nameof(IsRefreshing));
            OnPropertyChanged(nameof(Refreshed));
            OnPropertyChanged(nameof(NotRefreshed));
            OnPropertyChanged(nameof(TuneChannelsButtonVisible));
            OnPropertyChanged(nameof(InstallDriverButtonVisible));
            OnPropertyChanged(nameof(Channels));
            OnPropertyChanged(nameof(DriverIconImage));
        }

        public bool Refreshed
        {
            get
            {
                 return _refreshed;
            }
            set
            {
                _refreshed = value;
                NotifyChange();
            }
        }

        public bool NotRefreshed
        {
            get
            {
                return !_refreshed;
            }
        }

        public async Task CheckPendingPurchasesAsync()
        {
            _loggingService.Info("CheckPendingPurchasesAsync");

            if (!CrossInAppBilling.IsSupported)
            {
                _loggingService.Error("Billing system is not supported on this device");
                return;
            }

            var billing = CrossInAppBilling.Current;

            try
            {
                var connected = await billing.ConnectAsync();
                if (!connected)
                {
                    _loggingService.Error("Connection to billing failed");
                    return;
                }

                var purchases = await billing.GetPurchasesAsync(ItemType.InAppPurchase);
                if (purchases == null)
                {
                    _loggingService.Info("No purchases");
                    return;
                }

                foreach (var p in purchases)
                {
                    if (p.State == PurchaseState.Purchased)
                    {
                        _loggingService.Info("Consuming");
                        await billing.ConsumePurchaseAsync(p.ProductId, p.PurchaseToken);
                    }
                    else if (p.State == PurchaseState.PaymentPending)
                    {
                        _loggingService.Info("Donation still pending");
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
            finally
            {
                await billing.DisconnectAsync();
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
                }
                ;
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

                    Channel lastChannel = null;
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
                }
                ;
            }
        }

        public bool DoNotAutomaticallyShowEPGDetail
        {
            get => _doNotAutomaticallyShowEPGDetail;
            set => _doNotAutomaticallyShowEPGDetail = value;
        }

        public void SledovaniTVStartRecording()
        {
            _recordingBackgroundWorker.RunWorkerAsync();
        }

        private void _recordingBackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            _loggingService.Info($"_recordingBackgroundWorker_DoWork started");

            if (_recordingChannel == null)
                return;

            var outputFileName = Path.Combine(Config.OutputDirectory, $"{_recordingChannel.Name} {DateTime.Now.ToString("yyyy-MM-dd--HH-mm-ss")}.ts");

            if (!Directory.Exists(Config.OutputDirectory))
            {
                System.IO.Directory.CreateDirectory(Config.OutputDirectory);
            }

            using (var libvlc = new LibVLC())
            using (var mediaPlayer = new MediaPlayer(libvlc))
            {
                var media = new Media(libvlc, _recordingChannel.Url, FromType.FromLocation);

                media.AddOption(":sout=#file{dst=" + outputFileName + "}");
                media.AddOption(":sout-keep");

                // Start recording
                mediaPlayer.Play(media);

                do
                {
                    System.Threading.Thread.Sleep(500);

                    //var freespaceGB = Convert.ToInt64(Config.UsableSpace / 1000000000);

                    //if (freespaceGB < 1)
                    //{
                    //    throw new Exception("Nedosatatek volného místa");
                    //}

                } while (_recordingChannel != null);

                mediaPlayer.Stop();
            }
        }
    }
}

