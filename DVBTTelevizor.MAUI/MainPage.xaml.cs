using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using LibVLCSharp.Shared;
using LoggerService;


namespace DVBTTelevizor.MAUI
{
    public partial class MainPage : ContentPage
    {
        private MainViewModel _viewModel;
        private ILoggingService _loggingService { get; set; }
        private IDriverConnector _driver { get; set; }
        private IDialogService _dialogService;
        private ITVCConfiguration _configuration;
        private bool _firstAppearing = true;
        private DateTime _lastActionPlayTime = DateTime.MinValue;

        private Channel[] _lastPlayedChannels = new Channel[2];

        private static SemaphoreSlim _semaphoreSlimForRefreshGUI = new SemaphoreSlim(1, 1);
        private bool _refreshGUIEnabled = true;
        private bool _checkStreamEnabled = true;

        private LibVLC? _LibVLC;
        private MediaPlayer? _mediaPlayer;
        private Media _media;

        public string PublicDirectory { get; set; }

        public MainPage(ILoggingProvider loggingProvider, IPublicDirectoryProvider publicDirectoryProvider, ITVCConfiguration tvConfiguration)
        {
            InitializeComponent();

            _loggingService = loggingProvider.GetLoggingService();

            _loggingService.Info("MainPage starting");

            _configuration = tvConfiguration;
            _configuration.Load();

            _dialogService = new DialogService(this);

            _driver = new DVBTDriverConnector(_loggingService);

            PublicDirectory = publicDirectoryProvider.GetPublicDirectoryPath();

            BindingContext = _viewModel = new MainViewModel(_loggingService, _driver, tvConfiguration, _dialogService);
            _viewModel.PublicDirectory = PublicDirectory;
        }

        public PlayingStateEnum PlayingState
        {
            get
            {
                return _viewModel.PlayingState;
            }
            set
            {
                var oldValue = _viewModel.PlayingState;
                _viewModel.PlayingState = value;

                //if (oldValue != value)
                //{
                //    RefreshGUI();
                //}
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            _viewModel.OnAppearing();

            if (_firstAppearing)
            {
                _firstAppearing = false;

                InitializeVLC();

                _loggingService.Info("First appearing - sending Connect message");

                WeakReferenceMessenger.Default.Send(new DVBTDriverTestConnectMessage("Connect"));

                Task.Run(async () =>
                {
                    await _viewModel.RefreshChannels();
                });

                //    WeakReferenceMessenger.Default.Send(new DVBTDriverConnectMessage("Connect"));
                //_viewModel.Import(Path.Join(PublicDirectory, "DVBTTelevizor.channels.json"));
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            _mediaPlayer?.Dispose();
            _LibVLC?.Dispose();
        }

        private void InitializeVLC()
        {
            _loggingService.Info("Initializing LibVLC");

            _LibVLC = new LibVLC(/*enableDebugLogs: true*/);
            _mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_LibVLC);
            videoView.MediaPlayer = _mediaPlayer;
        }

        private void TuneButton_Clicked(object sender, EventArgs e)
        {

        }

        private void DriverButton_Clicked(object sender, EventArgs e)
        {

        }

        private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
        {

        }

        private void TapGestureRecognizer_Tapped_1(object sender, TappedEventArgs e)
        {

        }

        private void SwipeGestureRecognizer_Swiped(object sender, SwipedEventArgs e)
        {

        }

        private void SwipeGestureRecognizer_Swiped_1(object sender, SwipedEventArgs e)
        {

        }

        private void SwipeGestureRecognizer_Swiped_2(object sender, SwipedEventArgs e)
        {

        }

        private void SwipeGestureRecognizer_Swiped_3(object sender, SwipedEventArgs e)
        {

        }

        private void SwipeGestureRecognizer_Swiped_4(object sender, SwipedEventArgs e)
        {

        }

        private void ConnectButton_Clicked(object sender, EventArgs e)
        {
            WeakReferenceMessenger.Default.Send(new DVBTDriverConnectMessage("Connect"));
        }

        private void CallWithTimeout(Action action, int miliseconds = 1000)
        {
            // https://github.com/ZeBobo5/Vlc.DotNet/issues/542
            var task = Task.Run(() =>
            {
                ThreadPool.QueueUserWorkItem(_ => action());
            });

            if (!task.Wait(TimeSpan.FromMilliseconds(miliseconds)))
            {
                _loggingService.Info("Action not completed!");
            }
        }

        public async Task ActionPlay(Channel channel = null)
        {
            _loggingService.Debug($"ActionPlay");

            try
            {
                _refreshGUIEnabled = false;
                _checkStreamEnabled = false;

                if (channel == null)
                    channel = _viewModel.SelectedChannel;

                if (channel == null)
                    return;

                _loggingService.Debug($"playing: {channel.Name} ({channel.Number})");

                if (!_driver.Connected)
                {
                    WeakReferenceMessenger.Default.Send(new ToastMessage("Playing {0} failed (device not connected)".Translated(channel.Name)));
                    return;
                }

                if (_viewModel.RecordingChannel != null && _viewModel.RecordingChannel != channel)
                {
                    WeakReferenceMessenger.Default.Send(new ToastMessage("Playing {0} failed (recording in progress)".Translated(channel.Name)));
                    return;
                }

                if (channel.NonFree)
                {
                    WeakReferenceMessenger.Default.Send(new ToastMessage("Playing {0} failed (non free channel)".Translated(channel.Name)));
                    return;
                }

                long? signalStrengthPercentage = null;

                var shouldDriverPlay = true;
                var shouldMediaPlay = true;
                var shouldMediaStop = false;

                // just playing  ?
                if (PlayingState != PlayingStateEnum.Stopped)
                {
                    if (_viewModel.PlayingChannel != channel)
                    {
                        // playing different channel
                        shouldMediaPlay = true;
                        shouldDriverPlay = true;
                        shouldMediaStop = true;
                    }
                    else
                    {
                        // playing the same channel
                        shouldDriverPlay = false;
                        shouldMediaPlay = false;
                        shouldMediaStop = false;
                    }
                }
                else
                {
                    if (_viewModel.RecordingChannel == channel)
                    {
                        shouldMediaPlay = true;
                        shouldDriverPlay = false;
                        shouldMediaStop = false;
                    }
                    else
                    {
                        shouldMediaPlay = true;
                        shouldDriverPlay = true;
                        shouldMediaStop = false;
                    }
                }

                if (shouldMediaStop && videoView.MediaPlayer.IsPlaying)
                {
                    //await _driver.Stop(); // setting no PID

                    CallWithTimeout(delegate
                    {
                        _loggingService.Debug("Stopping Media player");
                        videoView.MediaPlayer.Stop();
                    });
                }

                if (shouldDriverPlay)
                {
                    // tuning only when changing frequency, bandwidth or DVBTType

                    var tuneNeeded = true;

                    if (_viewModel.PlayingChannel != null &&
                        _viewModel.PlayingChannel.Frequency == channel.Frequency &&
                        _viewModel.PlayingChannel.Bandwdith == channel.Bandwdith &&
                        _viewModel.PlayingChannel.DVBTType == channel.DVBTType)
                    {
                        tuneNeeded = false;
                        WeakReferenceMessenger.Default.Send(new LongToastMessage("Tuning ....".Translated()));
                    }

                    if (tuneNeeded)
                    {
                        WeakReferenceMessenger.Default.Send(new ToastMessage("Tuning {0} ....".Translated(channel.FrequencyShortLabel)));

                        var tunedRes = await _driver.TuneEnhanced(channel.Frequency, channel.Bandwdith, channel.DVBTType, false);
                        if (tunedRes.Result != DVBTDriverSearchProgramResultEnum.OK)
                        {
                            switch (tunedRes.Result)
                            {
                                case DVBTDriverSearchProgramResultEnum.NoSignal:
                                    WeakReferenceMessenger.Default.Send(new ToastMessage("No signal".Translated()));
                                    break;
                                default:
                                    WeakReferenceMessenger.Default.Send(new ToastMessage("Playing failed".Translated()));
                                    break;
                            }

                            return;
                        }

                        signalStrengthPercentage = tunedRes.SignalState.rfStrengthPercentage;
                    }

                    //var cachedPIDs = _viewModel.PID.GetChannelPIDs(channel.Frequency, channel.ProgramMapPID);
                    var cachedPIDs = new List<long>();

                    if (cachedPIDs != null &&
                        cachedPIDs.Count > 0)
                    {
                        var setPIDres = await _driver.SetPIDs(cachedPIDs);

                        if (!setPIDres.SuccessFlag)
                        {
                            WeakReferenceMessenger.Default.Send(new ToastMessage("Playing failed".Translated()));
                            return;
                        }
                    }
                    else
                    {
                        var setupPIDsRes = await _driver.SetupChannelPIDs(channel.ProgramMapPID, false);

                        if (setupPIDsRes.Result != DVBTDriverSearchProgramResultEnum.OK)
                        {
                            WeakReferenceMessenger.Default.Send(new ToastMessage("Playing failed".Translated()));
                            return;
                        }

                        //_viewModel.PID.AddChannelPIDs(channel.Frequency, channel.ProgramMapPID, setupPIDsRes.PIDs);
                    }

                    _driver.StartStream();

                    _lastActionPlayTime = DateTime.Now;
                }

                if (shouldMediaPlay)
                {
                    switch (_driver.DVBTDriverStreamType)
                    {
                        case DVBTDriverStreamTypeEnum.UDP:
                            _media = new Media(_LibVLC, _driver.StreamUrl, FromType.FromLocation);
                            break;
                        case DVBTDriverStreamTypeEnum.Stream:
                            _media = new Media(_LibVLC, new StreamMediaInput(_driver.VideoStream), new string[] { });
                            break;
                    }

                    CallWithTimeout(delegate
                    {
                        videoView.MediaPlayer.Play(_media);
                    });

                    //SetSubtitles(-1);
                    //SetAudioTrack(-100);
                    //_viewModel.TeletextEnabled = false;
                }

                var playInfo = new PlayStreamInfo
                {
                    Channel = channel
                };

                if (signalStrengthPercentage.HasValue)
                {
                    playInfo.SignalStrengthPercentage = Convert.ToInt32(signalStrengthPercentage.Value);
                }

                _viewModel.SelectedChannel = channel;
                _viewModel.PlayingChannel = channel;
                _viewModel.PlayingChannelSubtitles.Clear();
                _viewModel.PlayingChannelAudioTracks.Clear();
                _viewModel.PlayingChannelAspect = new Size(-1, -1);
                _viewModel.EPGDetailEnabled = false;

                if (_lastPlayedChannels[1] != channel)
                {
                    _lastPlayedChannels[0] = _lastPlayedChannels[1];
                    _lastPlayedChannels[1] = channel;
                }

                PlayingState = PlayingStateEnum.Playing;

                _viewModel.NotifyChannelChange();

                /*playInfo.CurrentEvent = await _viewModel.GetChannelEPG(channel);

                if (playInfo.CurrentEvent == null || playInfo.CurrentEvent.CurrentEventItem == null)
                {
                    await _viewModel.ScanEPG(channel, true, true, 2000, 3000);
                }
                */
                await _viewModel.ShowActualPlayingMessage(playInfo);

                //if (_config.PlayOnBackground)
                //{
                //    MessagingCenter.Send<MainPage, PlayStreamInfo>(this, BaseViewModel.MSG_PlayInBackgroundNotification, playInfo);
                //}

            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
            finally
            {
                _refreshGUIEnabled = true;
                _checkStreamEnabled = true;
                //RefreshGUI();
            }
        }


        private async void DriverStateButton_Clicked(object sender, EventArgs e)
        {
            if (_viewModel.DriverInstalled)
            {
                if (_driver.Connected)
                {
                    if (!(await _dialogService.Confirm("Connected device: {0}".Translated(_driver.Configuration.DeviceName),
                        "Device status".Translated(),
                        "Back".Translated(),
                        "Disconnect".Translated())))
                    {
                        //await _viewModel.DisconnectDriver();
                    }
                }
                else
                {
                    if (await _dialogService.Confirm("Disconnected.".Translated(),
                        "Device status".Translated(), "Connect".Translated(), "Back".Translated()))
                    {
                        WeakReferenceMessenger.Default.Send(new DVBTDriverTestConnectMessage("Connect"));
                        //WeakReferenceMessenger.Default.Send(new DVBTDriverConnectMessage("Connect"));
                    }
                }
            }
            else
            {
                if (await _dialogService.Confirm($"DVB-T Driver not installed.".Translated(),
                    "Device status".Translated(),
                    "Install DVB-T Driver".Translated(),
                    "Back".Translated()))
                {
                    //InstallDriver_Clicked(this, null);
                }
            }
        }

        private async void ChannelsListView_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            _loggingService.Info("ChannelsListView_ItemTapped");

            _loggingService.Info($"{e.Item.GetType().FullName}");
            if (e.Item is Channel channel)
            {
                _loggingService.Info($"ChannelsListView_ItemTapped: {channel.Name}");
                MainThread.BeginInvokeOnMainThread( async () =>
                {
                    await ActionPlay(channel);
                });
            }
        }

        private void DriverStateButton_Clicked_1(object sender, EventArgs e)
        {

        }

        private void MenuButton_Clicked(object sender, EventArgs e)
        {

        }

        private void SettingsButton_Clicked(object sender, EventArgs e)
        {

        }

        private void TuneButton_Clicked_1(object sender, EventArgs e)
        {

        }

        private void DVBTTelevizorButton_Clicked(object sender, EventArgs e)
        {

        }
    }

}
