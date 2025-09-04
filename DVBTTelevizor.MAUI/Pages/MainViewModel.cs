using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.DBManager;
using DVBTTelevizor.MAUI.Messages;
using DVBTTelevizor.TV;
using LibVLCSharp.Shared;
using LoggerService;
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
        private bool _menuVisible = false;

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

        public MainViewModel(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration, IDialogService dialogService, IPublicDirectoryProvider publicDirectoryProvider)
            :base(loggingService,driver, tvConfiguration, dialogService, publicDirectoryProvider)
        {
            EIT = new EITManager(loggingService, publicDirectoryProvider, driver);
            PID = new PIDManager(loggingService, publicDirectoryProvider, driver);

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

            BackgroundCommandWorker.RunInBackground(CommandScanEPG, 5, 10);
        }

        private void InitCommands()
        {
            CommandTune = new Command(() =>
            {
                //MenuVisible = false;
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
            WeakReferenceMessenger.Default.Register<DVBTDriverConnectedMessage>(this, (r, m) =>
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

        public bool MenuVisible
        {
            get
            {
                return _menuVisible;
            }
            set
            {
                _menuVisible = value;

                OnPropertyChanged(nameof(MenuVisible));
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
                    var tuned = await _driver.TuneEnhanced(channel.Frequency, channel.Bandwdith, channel.DVBTType, false);

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
                    channel.ClearEPG();

                    var channelEv = EIT.GetEvent(DateTime.Now, channel.Frequency, channel.ProgramMapPID);
                    if (channelEv != null)
                    {
                        channel.SetCurrentEvent(channelEv);
                    }

                    channel.NotifyChanges();
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


        public async Task RefreshChannels()
        {
            _loggingService.Debug($"Refreshing channels");

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                string? uniqueIdentifier = null;
                Channel? firstChannel = null;
                Channel? channelToSelect = null;

                try
                {
                    IsRefreshing = true;
                    var anySelected = false;

                    if (SelectedChannel != null)
                    {
                        uniqueIdentifier = SelectedChannel.UniqueIdentifier;
                        SelectedChannel = null;
                    }

                    await _semaphoreSlim.WaitAsync();

                    var channels = _configuration.GetChannels();

                    _loggingService.Debug($"Clearing channels");

                    Channels.Clear();

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

                        var ch = channel.Clone();
                        ch.Selected = false;

                        if (firstChannel == null)
                        {
                            firstChannel = ch;
                        }

                        if (uniqueIdentifier == ch.UniqueIdentifier)
                        {
                            channelToSelect = ch;
                            anySelected = true;
                        }

                        Channels.Add(ch);
                    }

                    if (!anySelected && firstChannel != null)
                    {
                        channelToSelect = firstChannel;
                    }

                    _loggingService.Debug($"Channels refreshed");
                }
                catch (Exception ex)
                {
                    _loggingService.Error(ex, "Refreshing channels failed");
                }
                finally
                {
                    _semaphoreSlim.Release();

                    SelectedChannel = channelToSelect;

                    NotifyEPGDetailVisibilityChange();

                    IsRefreshing = false;
                    Refreshed = true;

                    NotifyChannelChange();
                }
            });
        }

        public void SelectFirstChannel()
        {
            _loggingService.Info($"Selecting first channel");

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                _listViewSelector?.SelectFirstChannel();
                NotifyChannelChange();
            });
        }

        public void SelectNextChannel()
        {
            _loggingService.Info($"Selecting next channel");

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                _listViewSelector?.SelectNextChannel();
                NotifyChannelChange();
            });
        }

        public void SelectPreiousChannel()
        {
            _loggingService.Info($"Selecting previous channel");

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                _listViewSelector?.SelectPreviousChannel();
                NotifyChannelChange();
            });
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
                    await _dialogService.Information("File {0} not found".Translated(filename));
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
                await _dialogService.Information("Import failed".Translated());
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

        public bool InstallDriverButtonVisible
        {
            get
            {
                return Refreshed && (_driver == null || !_driver.DriverInstalled);
            }
        }

        public void UpdateDriverState()
        {
            OnPropertyChanged(nameof(DriverIconImage));
            OnPropertyChanged(nameof(InstallDriverButtonVisible));

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
                msg += Environment.NewLine + "(signal {0}%)".Translated(playStreamInfo.SignalStrengthPercentage.ToString());
                playStreamInfo.SignalStrengthPercentage = 0;
            }

            WeakReferenceMessenger.Default.Send(new ToastMessage(msg));
        }

        public void NotifyChannelChange()
        {
            _loggingService.Info($"NotifyChannelChange (Current channel: {SelectedChannel}, thread id:{Thread.CurrentThread.ManagedThreadId})");

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

        private void ConnectDriver(DVBTDriverConfiguration config)
        {
            _loggingService.Info("Connecting device: " + config.DeviceName);

            _driver.DriverInstalled = true;

            WeakReferenceMessenger.Default.Send(new ToastMessage("Device found: {0}".Translated(config.DeviceName)));

            _driver.Configuration = config;
            _driver.PublicDirectory = _publicDirectory;
            _driver.Connect();

            if (_driver is RTLSDRTCPIPFMDriverConnector)
            {
                //WeakReferenceMessenger.Default.Send(new PlayRawAdioMessage(String.Empty));
            }

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

            WeakReferenceMessenger.Default.Send(new ToastMessage("DVBT driver is not installed".Translated()));

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
                    return "uninstalled.png";
                }


                if (_driver.Connected)
                {
                    return "connected.png";

                }

                return "disconnected.png";
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



        public void OnAppearing()
        {
            _loggingService.Info("OnAppearing");
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
                    return "- no channel -";

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
                       return "tv.png";
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

        public bool TuneChannelsButtonVisible
        {
            get
            {
                return Channels.Count == 0 && Refreshed && _driver.DriverInstalled;
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
    }
}

