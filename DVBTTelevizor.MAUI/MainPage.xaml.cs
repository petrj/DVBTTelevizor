using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using LibVLCSharp.Shared;
using LoggerService;
using Microsoft.Maui.Layouts;


namespace DVBTTelevizor.MAUI
{
    public partial class MainPage : ContentPage, IOnKeyDown
    {
        private MainViewModel _viewModel;

        private ILoggingService _loggingService { get; set; }
        private IDriverConnector _driver { get; set; }
        private IDialogService _dialogService;
        private ITVCConfiguration _configuration;
        public string PublicDirectory { get; set; }

        private TestDVBTDriver _testDVBTDriver = null;
        private RemoteAccessService.RemoteAccessService _remoteAccessService;
        private List<string> _remoteDevicesConnected = new List<string>();

        private bool _firstAppearing = true;
        private DateTime _lastActionPlayTime = DateTime.MinValue;
        private Size _lastAllocatedSize = new Size(-1, -1);

        private Channel[] _lastPlayedChannels = new Channel[2];

        private KeyboardFocusableItemList _focusItems;

        private static SemaphoreSlim _semaphoreSlimForRefreshGUI = new SemaphoreSlim(1, 1);
        private bool _refreshGUIEnabled = true;
        private bool _checkStreamEnabled = true;

        private LibVLC? _LibVLC;
        private MediaPlayer? _mediaPlayer;
        private Media _media;

        private NavigationPage _settingsPage = null;

        private bool IsPortrait { get; set; } = false;

        // EPGDetailGrid
        private Rect LandscapeEPGDetailGridPosition { get; set; } = new Rect(1.0, 1.0, 0.3, 0.92);
        private Rect LandscapePreviewEPGDetailGridPosition { get; set; } = new Rect(1.0, 1.0, 0.3, 0.7);
        private Rect LandscapePlayingEPGDetailGridPosition { get; set; } = new Rect(1.0, 1.0, 0.3, 1.0);

        private Rect PortraitEPGDetailGridPosition { get; set; } = new Rect(1.0, 1.0, 1.0, 0.22);
        private Rect PortraitPreviewEPGDetailGridPosition { get; set; } = new Rect(1.0, 1.0, 1.0, 0.3);
        private Rect PortraitPlayingEPGDetailGridPosition { get; set; } = new Rect(1.0, 1.0, 1.0, 0.3);


        // VideoStackLayout
        private Rect LandscapePreviewVideoStackLayoutPosition { get; set; } = new Rect(1.0, 0.0, 0.3, 0.3);
        private Rect LandscapeVideoStackLayoutPositionWhenEPGDetailVisible { get; set; } = new Rect(0.0, 0.0, 0.7, 1.0);
        private Rect PortraitVideoStackLayoutPositionWhenEPGDetailVisible { get; set; } = new Rect(0.0, 0.0, 1.0, 0.7);
        private Rect PortraitPreviewVideoStackLayoutPosition { get; set; } = new Rect(1.0, 0.0, 0.5, 0.3);

        // VideoStackLayout must be visible when initializing VLC window!
        private Rect NoVideoStackLayoutPosition { get; set; } = new Rect(-10, -10, -5, -5);

        // RecordingLabel
        private Rect LandscapeRecordingLabelPosition { get; set; } = new Rect(1.0, 1.0, 0.1, 0.1);
        private Rect LandscapePreviewRecordingLabelPosition { get; set; } = new Rect(1.0, 0.25, 0.1, 0.1);
        private Rect LandscapeRecordingLabelPositionWhenEPGDetailVisible { get; set; } = new Rect(0.65, 1.0, 0.1, 0.1);
        private Rect PotraitRecordingLabelPosition { get; set; } = new Rect(1.0, 1.0, 0.1, 0.1);
        private Rect PortraitRecordingLabelPositionWhenEPGDetailVisible { get; set; } = new Rect(1.0, 0.65, 0.1, 0.1);
        private Rect PortraitPreviewRecordingLabelPosition { get; set; } = new Rect(1.0, 0.25, 0.1, 0.1);

        // ChannelsListView
        private Rect LandscapeChannelsListViewPositionWhenEPGDetailVisible { get; set; } = new Rect(0.0, 0.92, 0.7, 0.92);
        private Rect ChannelsListViewPositionWhenEPGDetailNOTVisible { get; set; } = new Rect(0, 0.92, 1, 0.92);
        private Rect PortraitChannelsListViewPositionWhenEPGDetailVisible { get; set; } = new Rect(0.0, 0.3, 1.0, 0.7);


        public MainPage(ILoggingProvider loggingProvider, IPublicDirectoryProvider publicDirectoryProvider, ITVCConfiguration tvConfiguration)
        {
            InitializeComponent();

            _loggingService = loggingProvider.GetLoggingService();

            _loggingService.Info("MainPage starting");

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                _loggingService.Error(e.ExceptionObject as Exception);
            };
            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                _loggingService.Error(e.Exception);
            };

            PublicDirectory = publicDirectoryProvider.GetPublicDirectoryPath();

            _configuration = tvConfiguration;
            _configuration.ConfigDirectory = PublicDirectory;
            _configuration.Load();

            var language = "cz";
            var languageFileName = Path.Join(PublicDirectory, "lng", $"{language}.lng");

            if (File.Exists(languageFileName))
            {
                Lng.LoadLanguage(languageFileName);
            }

            _dialogService = new DialogService(this);

            switch (_configuration.DVBTDriverType)
            {
                case DVBTDriverTypeEnum.AndroidDVBTDriver:
                    _driver = new DVBTDriverConnector(_loggingService);
                    break;
                case DVBTDriverTypeEnum.AndroidTestingDVBTDriver:
                    _driver = new DVBTDriverConnector(_loggingService);
                    break;
                case DVBTDriverTypeEnum.TestTuneDriver:
                    _driver = new TestTuneConnector(_loggingService);
                    break;
                default:
                    _driver = new TestTuneConnector(_loggingService);
                    break;
            }

            BindingContext = _viewModel = new MainViewModel(_loggingService, _driver, tvConfiguration, _dialogService, publicDirectoryProvider);

            _settingsPage = new NavigationPage(new SettingsPage(_loggingService, _driver, _configuration, _dialogService, publicDirectoryProvider));

            NavigationPage.SetHasNavigationBar(this, false);

            WeakReferenceMessenger.Default.Register<KeyDownMessage>(this, (r, m) =>
            {
                OnKeyDown(m.Value, m.Long);
            });

            BuildFocusableItems();

            _remoteAccessService = new RemoteAccessService.RemoteAccessService(_loggingService);
            RestartRemoteAccessService();
        }

        private void OnRemoteMessageReceived(RemoteAccessService.RemoteAccessMessage message)
        {
            if (message == null)
                return;

            var senderFriendlyName = message.GetSenderFriendlyName();
            if (!_remoteDevicesConnected.Contains(senderFriendlyName))
            {
                _remoteDevicesConnected.Add(senderFriendlyName);
                var msg = "Remote device connected".Translated();
                if (!string.IsNullOrEmpty(senderFriendlyName))
                {
                    msg += $" ({senderFriendlyName})";
                }

                WeakReferenceMessenger.Default.Send(new ToastMessage(msg));
            }

            if (message.command == "keyDown")
            {
                // TODO: use Instrumentation().SendKeyDownUpSync(keyCode);
                OnKeyDown(message.commandArg1, false);
            }
            if (message.command == "sendText")
            {
                OnTextSent(message.commandArg1);
            }
        }

        private void RestartRemoteAccessService()
        {
            _loggingService.Info("RestartRemoteAccessService");

            if (_configuration.AllowRemoteAccessService)
            {
                if (_remoteAccessService.IsBusy)
                {
                    if (_remoteAccessService.ParamsChanged(_configuration.RemoteAccessServiceIP, _configuration.RemoteAccessServicePort, _configuration.RemoteAccessServiceSecurityKey))
                    {
                        _remoteAccessService.StopListening();
                        _remoteAccessService.SetConnection(_configuration.RemoteAccessServiceIP, _configuration.RemoteAccessServicePort, _configuration.RemoteAccessServiceSecurityKey);
                        _remoteAccessService.StartListening(OnRemoteMessageReceived, BaseViewModel.DeviceFriendlyName);
                    }
                }
                else
                {
                    _remoteAccessService.SetConnection(_configuration.RemoteAccessServiceIP, _configuration.RemoteAccessServicePort, _configuration.RemoteAccessServiceSecurityKey);
                    _remoteAccessService.StartListening(OnRemoteMessageReceived, BaseViewModel.DeviceFriendlyName);
                }
            }
            else
            {
                _remoteAccessService.StopListening();
            }
        }

        private void BuildFocusableItems()
        {
            _focusItems = new KeyboardFocusableItemList();

            _focusItems
                .AddItem(KeyboardFocusableItem.CreateFrom("ChannelsListView", new List<View>() { ChannelsListView }))
                .AddItem(KeyboardFocusableItem.CreateFrom("DVBTTelevizorButton", new List<View>() { DVBTTelevizorButton }))
                .AddItem(KeyboardFocusableItem.CreateFrom("DriverStateButton", new List<View>() { DriverStateButton }))
                .AddItem(KeyboardFocusableItem.CreateFrom("TuneButton", new List<View>() { TuneButton }))

                .AddItem(KeyboardFocusableItem.CreateFrom("MenuButton", new List<View>() { MenuButton }))
                .AddItem(KeyboardFocusableItem.CreateFrom("SettingsButton", new List<View>() { SettingsButton }));

            //_focusItems.OnItemFocusedEvent += Page_OnItemFocusedEvent;
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            //System.Diagnostics.Debug.WriteLine($"OnSizeAllocated: {width}/{height}");

            base.OnSizeAllocated(width, height);

            if (_lastAllocatedSize.Width == width &&
                _lastAllocatedSize.Height == height)
            {
                // no size changed
                return;
            }

            if (width > height)
            {
                IsPortrait = false;
            }
            else
            {
                IsPortrait = true;
            }

            _lastAllocatedSize.Width = width;
            _lastAllocatedSize.Height = height;

            //_viewModel.NotifyToolBarChange();

            RefreshGUI();
        }

        public void RefreshGUI()
        {
            if (!_refreshGUIEnabled)
                return;

            _loggingService.Debug("RefreshGUI");

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    await _semaphoreSlimForRefreshGUI.WaitAsync();

                    AbsoluteLayout.SetLayoutFlags(VideoStackLayout, AbsoluteLayoutFlags.All);
                    AbsoluteLayout.SetLayoutFlags(NoVideoStackLayout, AbsoluteLayoutFlags.All);

                    //_loggingService.Debug($"PlayingState: {PlayingState}");

                    switch (PlayingState)
                    {
                        case PlayingStateEnum.Playing:

                            //MessagingCenter.Send(System.String.Empty, BaseViewModel.MSG_EnableFullScreen);

                            // VideoStackLayout must be visible before changing Layout
                            VideoStackLayout.IsVisible = true;
                            NoVideoStackLayout.IsVisible = false;
                            //ChannelsListView.IsVisible = false;

                            if (IsPortrait)
                            {
                                if (_viewModel.EPGDetailVisible)
                                {
                                    AbsoluteLayout.SetLayoutBounds(EPGDetailGrid, PortraitPlayingEPGDetailGridPosition);
                                    //MainLayout.RaiseChild(EPGDetailGrid);

                                    AbsoluteLayout.SetLayoutBounds(VideoStackLayout, PortraitVideoStackLayoutPositionWhenEPGDetailVisible);
                                    AbsoluteLayout.SetLayoutBounds(NoVideoStackLayout, PortraitVideoStackLayoutPositionWhenEPGDetailVisible);
                                    AbsoluteLayout.SetLayoutBounds(RecordingLabel, PortraitRecordingLabelPositionWhenEPGDetailVisible);
                                }
                                else
                                {
                                    AbsoluteLayout.SetLayoutBounds(VideoStackLayout, new Rect(0, 0, 1, 1));
                                    AbsoluteLayout.SetLayoutBounds(NoVideoStackLayout, new Rect(0, 0, 1, 1));
                                    AbsoluteLayout.SetLayoutBounds(RecordingLabel, PotraitRecordingLabelPosition);
                                }
                            }
                            else
                            {
                                if (_viewModel.EPGDetailVisible)
                                {
                                    AbsoluteLayout.SetLayoutBounds(EPGDetailGrid, LandscapePlayingEPGDetailGridPosition);
                                    //MainLayout.RaiseChild(EPGDetailGrid);

                                    AbsoluteLayout.SetLayoutBounds(VideoStackLayout, LandscapeVideoStackLayoutPositionWhenEPGDetailVisible);
                                    AbsoluteLayout.SetLayoutBounds(NoVideoStackLayout, LandscapeVideoStackLayoutPositionWhenEPGDetailVisible);
                                    AbsoluteLayout.SetLayoutBounds(RecordingLabel, LandscapeRecordingLabelPositionWhenEPGDetailVisible);
                                }
                                else
                                {
                                    AbsoluteLayout.SetLayoutBounds(VideoStackLayout, new Rect(0, 0, 1, 1));
                                    AbsoluteLayout.SetLayoutBounds(NoVideoStackLayout, new Rect(0, 0, 1, 1));
                                    AbsoluteLayout.SetLayoutBounds(RecordingLabel, LandscapeRecordingLabelPosition);
                                }
                            }

                            //MainLayout.RaiseChild(VideoStackLayout);
                            //CheckStreamCommand.Execute(null);

                            break;
                        case PlayingStateEnum.PlayingInPreview:

                            if (!NavigationPage.GetHasNavigationBar(this))
                            {
                                NavigationPage.SetHasNavigationBar(this, true);
                            }

                            //ChannelsListView.IsVisible = true;

                            //if (!_config.Fullscreen)
                            //{
                            //    MessagingCenter.Send(System.String.Empty, BaseViewModel.MSG_DisableFullScreen);
                            //}

                            if (IsPortrait)
                            {
                                if (_viewModel.EPGDetailVisible)
                                {
                                    AbsoluteLayout.SetLayoutBounds(EPGDetailGrid, PortraitPreviewEPGDetailGridPosition);
                                    AbsoluteLayout.SetLayoutBounds(ChannelsListView, PortraitChannelsListViewPositionWhenEPGDetailVisible);
                                }
                                else
                                {
                                    AbsoluteLayout.SetLayoutBounds(ChannelsListView, new Rect(0, 0, 1, 1));
                                }

                                AbsoluteLayout.SetLayoutBounds(VideoStackLayout, PortraitPreviewVideoStackLayoutPosition);
                                AbsoluteLayout.SetLayoutBounds(NoVideoStackLayout, PortraitPreviewVideoStackLayoutPosition);
                                AbsoluteLayout.SetLayoutBounds(RecordingLabel, PortraitPreviewRecordingLabelPosition);
                            }
                            else
                            {
                                if (_viewModel.EPGDetailVisible)
                                {
                                    AbsoluteLayout.SetLayoutBounds(ChannelsListView, LandscapeChannelsListViewPositionWhenEPGDetailVisible);
                                    AbsoluteLayout.SetLayoutBounds(EPGDetailGrid, LandscapePreviewEPGDetailGridPosition);
                                }
                                else
                                {
                                    AbsoluteLayout.SetLayoutBounds(ChannelsListView, new Rect(0, 0, 1, 1));
                                }

                                AbsoluteLayout.SetLayoutBounds(VideoStackLayout, LandscapePreviewVideoStackLayoutPosition);
                                AbsoluteLayout.SetLayoutBounds(NoVideoStackLayout, LandscapePreviewVideoStackLayoutPosition);
                                AbsoluteLayout.SetLayoutBounds(RecordingLabel, LandscapePreviewRecordingLabelPosition);
                            }

                            //CheckStreamCommand.Execute(null);

                            break;
                        case PlayingStateEnum.Stopped:

                            //if (!NavigationPage.GetHasNavigationBar(this))
                            //{
                            //    NavigationPage.SetHasNavigationBar(this, true);
                            //}

                            //ChannelsListView.IsVisible = true;

                            //if (!_config.Fullscreen)
                            //{
                            //    MessagingCenter.Send(System.String.Empty, BaseViewModel.MSG_DisableFullScreen);
                            //}

                            //VideoStackLayout.IsVisible = false;
                            NoVideoStackLayout.IsVisible = false;

                            if (IsPortrait)
                            {
                                if (_viewModel.EPGDetailVisible)
                                {
                                    AbsoluteLayout.SetLayoutBounds(ChannelsListView, PortraitChannelsListViewPositionWhenEPGDetailVisible);
                                    AbsoluteLayout.SetLayoutBounds(EPGDetailGrid, PortraitEPGDetailGridPosition);
                                }
                                else
                                {
                                    AbsoluteLayout.SetLayoutBounds(ChannelsListView, ChannelsListViewPositionWhenEPGDetailNOTVisible);
                                }
                            }
                            else // landscape
                            {
                                if (_viewModel.EPGDetailVisible)
                                {
                                    AbsoluteLayout.SetLayoutBounds(ChannelsListView, LandscapeChannelsListViewPositionWhenEPGDetailVisible);
                                    AbsoluteLayout.SetLayoutBounds(EPGDetailGrid, LandscapeEPGDetailGridPosition);
                                }
                                else
                                {
                                    AbsoluteLayout.SetLayoutBounds(ChannelsListView, ChannelsListViewPositionWhenEPGDetailNOTVisible);
                                }
                            }

                            AbsoluteLayout.SetLayoutBounds(VideoStackLayout, NoVideoStackLayoutPosition);


                            break;
                    }

                    //_loggingService.Info("RefreshGUI completed");

                }
                catch (Exception ex)
                {
                    _loggingService.Error(ex);
                }
                finally
                {
                    _semaphoreSlimForRefreshGUI.Release();
                }
            });
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

                if (oldValue != value)
                {
                    RefreshGUI();
                }
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            _focusItems.DeFocusAll();

            _viewModel.OnAppearing();

            if (_firstAppearing)
            {
                _firstAppearing = false;

                InitializeVLC();

                ConnectDriver();

                Task.Run(async () =>
                {
                    await _viewModel.RefreshChannels();
                    await _viewModel.SelectFirstChannel();
                });

                //_viewModel.Import(Path.Join(PublicDirectory, "DVBTTelevizor.channels.json"));
            }
        }

        private void ConnectDriver()
        {
            switch (_configuration.DVBTDriverType)
            {
                case DVBTDriverTypeEnum.AndroidDVBTDriver:

                    _loggingService.Info("Sending connect message");
                    WeakReferenceMessenger.Default.Send(new DVBTDriverConnectMessage("Connect"));
                    break;

                case DVBTDriverTypeEnum.AndroidTestingDVBTDriver:

                    _testDVBTDriver = new TestDVBTDriver(_loggingService);
                    _testDVBTDriver.PublicDirectory = PublicDirectory;
                    _testDVBTDriver.Connect();

                    WeakReferenceMessenger.Default.Send(new DVBTDriverConnectedMessage(
                        new DVBTDriverConfiguration()
                        {
                            DeviceName = "Testing DVBT driver",
                            ControlPort = _testDVBTDriver.ControlIPEndPoint.Port,
                            TransferPort = _testDVBTDriver.TransferIPEndPoint.Port
                        }));
                    break;

                case DVBTDriverTypeEnum.TestTuneDriver:

                    WeakReferenceMessenger.Default.Send(new DVBTDriverConnectedMessage(
                        new DVBTDriverConfiguration()
                        {
                            DeviceName = "Test tune driver"
                        }));

                    break;
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            //_mediaPlayer?.Dispose();
            //_LibVLC?.Dispose();
        }

        private void InitializeVLC()
        {
            _loggingService.Info("Initializing LibVLC");

            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                _LibVLC = new LibVLC(/*enableDebugLogs: true*/);
                _mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_LibVLC);
                videoView.MediaPlayer = _mediaPlayer;
            }
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
                    if (DeviceInfo.Platform == DevicePlatform.Android)
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
                    }

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

        private async void SettingsButton_Clicked(object sender, EventArgs e)
        {
            if (_settingsPage.IsLoaded)
            {
                // preventing click when the settings page is just (or yet) loaded
                return;
            }
            await Navigation.PushAsync(_settingsPage);
        }

        private void TuneButton_Clicked_1(object sender, EventArgs e)
        {

        }

        private void DVBTTelevizorButton_Clicked(object sender, EventArgs e)
        {

        }

        public void OnKeyDown(string key, bool longPress)
        {
            _loggingService.Debug($"Main Page OnKeyDown {key}");

            var keyAction = KeyboardDeterminer.GetKeyAction(key);

            switch (keyAction)
            {
                case KeyboardNavigationActionEnum.Right:
                case KeyboardNavigationActionEnum.Down:
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        _focusItems.FocusNextItem();
                    });
                    break;

                case KeyboardNavigationActionEnum.Left:
                case KeyboardNavigationActionEnum.Up:
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        _focusItems.FocusPreviousItem();
                    });
                    break;

                case KeyboardNavigationActionEnum.Back:
                    //
                    break;

                case KeyboardNavigationActionEnum.OK:

                    switch (_focusItems.FocusedItemName)
                    {
                        case "ChannelsListView":
                            break;
                    }

                    break;
            }
        }

        public void OnTextSent(string text)
        {

        }
    }

}
