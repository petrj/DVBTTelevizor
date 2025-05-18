using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using LibVLCSharp.Shared;
using LoggerService;
using Microsoft.Maui.Layouts;
using System.Windows.Input;
using DVBTTelevizor.TV;
using RTLSDR.Common;

namespace DVBTTelevizor.MAUI
{
    public partial class MainPage : ContentPage, IOnKeyDown
    {
        private MainViewModel _viewModel;
        private ILoggingService _loggingService { get; set; }
        private IDriverConnector _driver { get; set; }
        private IDialogService _dialogService;
        private ITVConfiguration _configuration;
        public string PublicDirectory { get; set; }

        private TestDVBTDriver _testDVBTDriver = null;
        private RemoteAccessService.RemoteAccessService _remoteAccessService;
        private List<string> _remoteDevicesConnected = new List<string>();

        private bool _firstAppearing = true;
        private DateTime _lastActionPlayTime = DateTime.MinValue;
        private Size _lastAllocatedSize = new Size(-1, -1);

        private Channel[] _lastPlayedChannels = new Channel[2];

        private KeyboardFocusableItemList _focusItems;
        private KeyboardFocusableItemList _focusMenuItems;

        private static SemaphoreSlim _semaphoreSlimForRefreshGUI = new SemaphoreSlim(1, 1);
        private bool _refreshGUIEnabled = true;
        private bool _checkStreamEnabled = true;

        private LibVLC? _LibVLC;
        private MediaPlayer? _mediaPlayer;
        private Media _media;

        private NavigationPage _settingsPage = null;
        private TuningWelcomePage _tuneWelcomePage = null;
        private NavigationPage _aboutPage = null;
        private NavigationPage _driverPage = null;

        private bool IsPortrait { get; set; } = false;

        // EPGDetailGrid
        private Rect LandscapeEPGDetailGridPosition { get; set; } = new Rect(1.0, 0.22, 0.3, 0.62); //new Rect(1.0, 1.0, 0.3, 0.92);
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

        public MainPage(ILoggingProvider loggingProvider, IPublicDirectoryProvider publicDirectoryProvider, ITVConfiguration tvConfiguration)
        {
            PublicDirectory = publicDirectoryProvider.GetPublicDirectoryPath();

            var language = "cz";
            var languageFileName = Path.Join(PublicDirectory, "lng", $"{language}.lng");

            if (File.Exists(languageFileName))
            {
                Lng.LoadLanguage(languageFileName);
            }

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

            _configuration = tvConfiguration;
            _configuration.ConfigDirectory = PublicDirectory;

            _dialogService = new DialogService(this);

            InitDVBTDriver();

            BindingContext = _viewModel = new MainViewModel(_loggingService, _driver, tvConfiguration, _dialogService, publicDirectoryProvider);

            _settingsPage = new NavigationPage(new SettingsPage(_loggingService, _driver, _configuration, _dialogService, publicDirectoryProvider));
            _tuneWelcomePage = new TuningWelcomePage(_loggingService, _driver, _configuration, _dialogService, publicDirectoryProvider);
            _aboutPage = new NavigationPage(new AboutPage(_loggingService, _driver, _configuration, _dialogService, publicDirectoryProvider));
            _driverPage = new NavigationPage(new DriverPage(_loggingService, _driver, _configuration, _dialogService, publicDirectoryProvider));

            NavigationPage.SetHasNavigationBar(this, false);

            WeakReferenceMessenger.Default.Register<KeyDownMessage>(this, (r, m) =>
            {
                OnKeyDown(m.Value, m.Long);
            });

            BuildFocusableItems();

            _remoteAccessService = new RemoteAccessService.RemoteAccessService(_loggingService);
            RestartRemoteAccessService();

            WeakReferenceMessenger.Default.Register<ConnectMessage>(this, (r, m) =>
            {
                ConnectDriver();
            });

            WeakReferenceMessenger.Default.Register<DVBTDriverChangedMessage>(this, (r, m) =>
            {
                InitDVBTDriver();
            });

            WeakReferenceMessenger.Default.Register<FinishTuningMessage>(this, (r, m) =>
            {
                _loggingService.Info($"FinishTuning");

                CloseAllPages();
            });

            WeakReferenceMessenger.Default.Register<PlayMessage>(this, (r, m) =>
            {
                Task.Run(async () =>
                {
                    await ActionPlay(_viewModel.SelectedChannel);
                });
            });

            WeakReferenceMessenger.Default.Register<ShowTuneMessage>(this, (r, m) =>
            {
                TuneButton_Clicked(this, null);
            });

            WeakReferenceMessenger.Default.Register<ShowSettingsMessage>(this, (r, m) =>
            {
                SettingsButton_Clicked(this, null);
            });

            WeakReferenceMessenger.Default.Register<ShowAboutMessage>(this, (r, m) =>
            {
                DVBTTelevizorButton_Clicked(this, null);
            });

            WeakReferenceMessenger.Default.Register<ShowDriverStateMessage>(this, (r, m) =>
            {
                DriverStateButton_Clicked(this, null);
            });
            WeakReferenceMessenger.Default.Register<ShowMenuMessage>(this, (r, m) =>
            {
                MenuButton_Clicked(this, null);
            });

            WeakReferenceMessenger.Default.Register<USBChangedMessage>(this, (r, m) =>
            {
                USBConnectOrDisconnect();
            });

            WeakReferenceMessenger.Default.Register<InstallDriverMessage>(this, (r, m) =>
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Browser.OpenAsync("https://play.google.com/store/apps/details?id=info.martinmarinov.dvbdriver", BrowserLaunchMode.External);
                });
            });

            WeakReferenceMessenger.Default.Register<PlayRawAdioMessage>(this, (r, m) =>
            {
                // Create Media from TCP stream
                var media = new Media(_LibVLC, $"udp://localhost:8012", FromType.FromLocation);
                media.AddOption(":demux=rawaud");
                media.AddOption(":rawaud-channels=1");
                media.AddOption(":rawaud-samplerate=96000");
                media.AddOption(":rawaud-fourcc=s16l");

                _mediaPlayer.Play(media);
            });


            _settingsPage.Disappearing += delegate
            {
                Task.Run( async () =>
                {
                    await _viewModel.RefreshChannels();
                });
            };
        }

        private async void USBConnectOrDisconnect()
        {
            switch (_driver.State)
            {
                case DVBTDriverStateEnum.Unknown:
                case DVBTDriverStateEnum.Disconnected:
                    {
                        ConnectDriver();
                        break;
                    }
                default:
                    {
                        // check driver state
                        Task.Run(async () =>
                        {
                            await CheckDriverState();
                        });
                        break;
                    }
            }
        }

        private async Task CheckDriverState()
        {
            try
            {
                var status = await _driver.CheckStatus();
                if (!status)
                {
                    _viewModel.DisconnectDriver();
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "Error while checking driver state");
            }
        }

        private void InitDVBTDriver()
        {
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
                case DVBTDriverTypeEnum.RTLSDRTCPIPFMDriver:
                    _driver = new RTLSDRTCPIPFMDriverConnector(_loggingService);
                    break;
                default:
                    _driver = new TestTuneConnector(_loggingService);
                    break;
            }
        }

        private void CloseAllPages()
        {
            var max = 5; // max 5 pages
            var current = 0;

            while (current < max)
            {
                var stack = Navigation.NavigationStack;
                if (stack.Count > 0)
                {
                    int i = 0;
                    _loggingService.Info($"Pages on stack:");
                    foreach (var p in stack)
                    {
                        _loggingService.Info($"{new string(' ', i * 2)}: {p.GetType().Name}");
                        i++;
                    }

                    var pageOnTop = stack[stack.Count - 1];

                    if (pageOnTop != this)
                    {
                        _loggingService.Info($"Closing page in top: {pageOnTop.GetType().Name}");
                        MainThread.BeginInvokeOnMainThread(async () =>
                        {
                            await Navigation.PopAsync();
                        });
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    _loggingService.Info($"No page on top");
                }

                current++;
            }
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
                WeakReferenceMessenger.Default.Send(new RemoteKeyPlatformActionMessage(message.commandArg1));
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
                .AddItem(KeyboardFocusableItem.CreateFrom("TuneQuickButton", new List<View>() { TuneQuickImgButton }));

            _focusItems.OnItemFocusedEvent += _focusItems_OnItemFocusedEvent;

            _focusMenuItems = new KeyboardFocusableItemList();

            _focusMenuItems
                .AddItem(KeyboardFocusableItem.CreateFrom("MenuButtonPlay", new List<View>() { MenuButtonPlay }))
                .AddItem(KeyboardFocusableItem.CreateFrom("MenuButtonStop", new List<View>() { MenuButtonStop }))
                .AddItem(KeyboardFocusableItem.CreateFrom("MenuButtonRecord", new List<View>() { MenuButtonRecord }))
                .AddItem(KeyboardFocusableItem.CreateFrom("MenuButtonStopRecord", new List<View>() { MenuButtonStopRecord }))
                .AddItem(KeyboardFocusableItem.CreateFrom("MenuButtonShowEPG", new List<View>() { MenuButtonShowEPG }))
                .AddItem(KeyboardFocusableItem.CreateFrom("MenuButtonHideEPG", new List<View>() { MenuButtonHideEPG }))
                .AddItem(KeyboardFocusableItem.CreateFrom("MenuButtonSubtitles", new List<View>() { MenuButtonSubtitles }))
                .AddItem(KeyboardFocusableItem.CreateFrom("MenuButtonTeletext", new List<View>() { MenuButtonTeletext }))
                .AddItem(KeyboardFocusableItem.CreateFrom("MenuButtonAudio", new List<View>() { MenuButtonAudio }))
                .AddItem(KeyboardFocusableItem.CreateFrom("MenuButtonAspect", new List<View>() { MenuButtonAspect }))
                .AddItem(KeyboardFocusableItem.CreateFrom("MenuButtonScanEPG", new List<View>() { MenuButtonScanEPG }))
                .AddItem(KeyboardFocusableItem.CreateFrom("MenuButtonSettings", new List<View>() { MenuButtonSettings }))
                .AddItem(KeyboardFocusableItem.CreateFrom("MenuButtonQuit", new List<View>() { MenuButtonQuit }))
                .AddItem(KeyboardFocusableItem.CreateFrom("MenuButtonClose", new List<View>() { MenuButtonClose }));
        }

        private void _focusItems_OnItemFocusedEvent(KeyboardFocusableItemEventArgs _args)
        {
            if (_focusItems.FocusedItemName == "ChannelsListView")
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    _viewModel.SelectFirstChannel();
                    ChannelsListView.ScrollTo(ChannelsListView.SelectedItem, ScrollToPosition.Center, animated: true);
                });
            }
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

                    if (IsPortrait)
                    {
                        DVBTTelevizorButton.BottomTitleText = "DVBT Televizor".Translated();
                        DriverStateButton.BottomTitleText = "Driver".Translated();
                        TuneButton.BottomTitleText = "Tune".Translated();
                        MenuButton.BottomTitleText = "Menu".Translated();

                        DVBTTelevizorButton.TopTitleText =
                        DriverStateButton.TopTitleText =
                        TuneButton.TopTitleText =
                        MenuButton.TopTitleText = "";

                    } else
                    {
                        DVBTTelevizorButton.TopTitleText = "DVBT Televizor".Translated();
                        DriverStateButton.TopTitleText = "Driver".Translated();
                        TuneButton.TopTitleText = "Tune".Translated();
                        MenuButton.TopTitleText = "Menu".Translated();

                        DVBTTelevizorButton.BottomTitleText =
                        DriverStateButton.BottomTitleText =
                        TuneButton.BottomTitleText =
                        MenuButton.BottomTitleText = "";
                    }

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

                            NavigationPage.SetHasNavigationBar(this, false);

                            ChannelsListView.IsVisible = true;

                            WeakReferenceMessenger.Default.Send(new ShowFullscreenMessage("Connect"));

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

                            NavigationPage.SetHasNavigationBar(this, false);

                            ChannelsListView.IsVisible = true;

                            WeakReferenceMessenger.Default.Send(new ShowFullscreenMessage(""));

                            VideoStackLayout.IsVisible = false;
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
                                    AbsoluteLayout.SetLayoutBounds(ChannelsListView, LandscapeChannelsListViewPositionWhenEPGDetailVisible); // 0,1,0.7,0.92
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
                WeakReferenceMessenger.Default.Send(new ShowFullscreenMessage(""));

                _firstAppearing = false;

                InitializeVLC();

                ConnectDriver();

                Task.Run(async () =>
                {
                    await _viewModel.RefreshChannels();
                });


                //MainThread.BeginInvokeOnMainThread(async () =>
                //{
                //    videoView.MediaPlayer.Play();
                //});

                    //_viewModel.Import(Path.Join(PublicDirectory, "DVBTTelevizor.channels.json"));
                }
        }

        private void ConnectDriver()
        {
            switch (_configuration.DVBTDriverType)
            {
                case DVBTDriverTypeEnum.AndroidDVBTDriver:

                    _loggingService.Info("Sending connect message");
                    WeakReferenceMessenger.Default.Send(new DVBTDriverConnectAndroidMessage("Connect"));
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

                case DVBTDriverTypeEnum.RTLSDRTCPIPFMDriver:

                    var cfg = new RTLSDR.DriverSettings()
                    {
                        Port = _configuration.SDRDriverPort,
                        Streamport = _configuration.SDRDriverStreamPort,
                        SDRSampleRate = _configuration.SDRSampleRate
                    };

                    WeakReferenceMessenger.Default.Send(new RTLSDRDriverConnectAndroidMessage(cfg));

                    WeakReferenceMessenger.Default.Send(new NotifyAudioChangeMessage(""));  // starting audio reciever in MainActivity

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

                //var media = new Media(_LibVLC, new Uri("http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4"));
                //_mediaPlayer.Media = media;
            }
        }

        private async void TuneButton_Clicked(object sender, EventArgs e)
        {
            if (_tuneWelcomePage.IsLoaded)
            {
                // preventing click when the settings page is just (or yet) loaded
                return;
            }

            await Navigation.PushAsync(_tuneWelcomePage);
        }

        private void DriverButton_Clicked(object sender, EventArgs e)
        {

        }

        private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
        {

        }

        private void SwipeGestureRecognizer_Swiped(object sender, SwipedEventArgs e)
        {

        }

        private void VideoSwiped_Left(object sender, SwipedEventArgs e)
        {
            Task.Run(async () =>
            {
                await ActionStop(true);
            });
        }

        private void VideoSwiped_Right(object sender, SwipedEventArgs e)
        {
            Task.Run(async () =>
            {
                await ActionStop(false);
            });
        }

        private void SwipeGestureRecognizer_Swiped_3(object sender, SwipedEventArgs e)
        {

        }

        private void SwipeGestureRecognizer_Swiped_4(object sender, SwipedEventArgs e)
        {

        }

        public async Task ActionStop(bool force)
        {
            _loggingService.Debug($"ActionStop (Force: {force}, PlayingState: {PlayingState})");

            if (_media == null || videoView == null || videoView.MediaPlayer == null)
                return;

            //_viewModel.SelectedPart = SelectedPartEnum.ChannelsListOrVideo;
            //_viewModel.EPGDetailEnabled = false;

            if (!force && (PlayingState == PlayingStateEnum.Playing))
            {
                if (_viewModel.EPGDetailVisible)
                {
                    _viewModel.EPGDetailEnabled = false;
                }
                else
                {
                    PlayingState = PlayingStateEnum.PlayingInPreview;
                    _viewModel.EPGDetailEnabled = true;
                }
            }
            else
            {

                CallWithTimeout(delegate
                {
                    videoView.MediaPlayer.Stop();

                    if (_viewModel.RecordingChannel == null)
                    {
                        _driver.Stop();
                    }
                });


                PlayingState = PlayingStateEnum.Stopped;

                _lastActionPlayTime = DateTime.MinValue;

                _viewModel.PlayingChannelSubtitles.Clear();
                _viewModel.PlayingChannelAudioTracks.Clear();
                _viewModel.PlayingChannelAspect = new Size(-1, -1);
                _viewModel.PlayingChannel = null;

                //MessagingCenter.Send("", BaseViewModel.MSG_StopPlayInBackgroundNotification);
            }

            //_viewModel.SelectedToolbarItemName = null;
            //_viewModel.SelectedPart = SelectedPartEnum.ChannelsListOrVideo;
            //_viewModel.NotifyMediaChange();
        }

        private void CallWithTimeout(Action action, int miliseconds = 1000)
        {
            // https://github.com/ZeBobo5/Vlc.DotNet/issues/542
            var task = Task.Run(() =>
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    ThreadPool.QueueUserWorkItem(_ => action());
                });
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

                if (
                    (_configuration.DVBTDriverType == DVBTDriverTypeEnum.RTLSDRFMDriver) ||
                    (  _configuration.DVBTDriverType == DVBTDriverTypeEnum.RTLSDRTCPIPFMDriver)
                    )
                {
                    shouldMediaStop = false;
                    shouldMediaPlay = false;
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
                    } else
                    if (DeviceInfo.Platform == DevicePlatform.WinUI)
                    {
                        var udpStreamer = new UDPStreamer(_loggingService,"127.0.0.1", 8012);
                        var url = $"udp://@{udpStreamer.IP}:{udpStreamer.Port}";

                        udpStreamer.SendStream(_driver.VideoStream);

                        VLCLauncher.RunInWindows(url);
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

                if (
    (_configuration.DVBTDriverType == DVBTDriverTypeEnum.RTLSDRFMDriver) ||
    (_configuration.DVBTDriverType == DVBTDriverTypeEnum.RTLSDRTCPIPFMDriver)
    )
                {
                    PlayingState = PlayingStateEnum.PlayingInPreview;
                } else
                {
                    PlayingState = PlayingStateEnum.Playing;
                }

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
                RefreshGUI();
            }
        }

        private async void DriverStateButton_Clicked(object sender, EventArgs e)
        {
            if (_driverPage.IsLoaded)
            {
                // preventing click when the settings page is just (or yet) loaded
                return;
            }
            await Navigation.PushAsync(_driverPage);
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

        private async void MenuButton_Clicked(object sender, EventArgs e)
        {
            if (!_viewModel.MenuVisible)
            {
                BuildMenu();
            }
            _viewModel.MenuVisible = !_viewModel.MenuVisible;
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

        private async void DVBTTelevizorButton_Clicked(object sender, EventArgs e)
        {
            if (_aboutPage.IsLoaded)
            {
                // preventing click when the settings page is just (or yet) loaded
                return;
            }
            await Navigation.PushAsync(_aboutPage);
        }

        public void OnKeyDown(string key, bool longPress)
        {
            _loggingService.Debug($"Main Page OnKeyDown {key}");

            var stack = Navigation.NavigationStack;
            if (stack[stack.Count - 1].GetType() != typeof(MainPage))
            {
                // different page on navigation top

                var pageOnTop = stack[stack.Count - 1];
                if (pageOnTop is NavigationPage np)
                {
                    pageOnTop = np.CurrentPage;
                }

                if (pageOnTop is IOnKeyDown)
                {
                    (pageOnTop as IOnKeyDown).OnKeyDown(key, longPress);
                }

                return;
            }

            var keyAction = KeyboardDeterminer.GetKeyAction(key);

            if (_viewModel.MenuVisible)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    OnMenuKeyDown(keyAction);
                });
                return;
            }

            switch (keyAction)
            {
                case KeyboardNavigationActionEnum.Right:

                    if (_focusItems.FocusedItemName == "ChannelsListView")
                    {
                        MainThread.BeginInvokeOnMainThread(async () =>
                        {
                            _focusItems.FocusItem("DVBTTelevizorButton");
                        });
                    }
                    else
                    {
                        MainThread.BeginInvokeOnMainThread(async () =>
                        {
                            _focusItems.FocusNextItem(true);
                        });
                    }
                    break;

                case KeyboardNavigationActionEnum.Down:

                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        if ((new List<string>() { null, "DVBTTelevizorButton", "DriverStateButton", "TuneButton", "MenuButton", "SettingsButton" }).Contains(_focusItems.FocusedItemName) &&
                        _viewModel.ChannelsListViewVisible)
                        {
                            _focusItems.FocusItem("ChannelsListView");
                        }
                        else
                    if (_focusItems.FocusedItemName == "ChannelsListView")
                        {
                            _viewModel.SelectNextChannel();
                            ChannelsListView.ScrollTo(ChannelsListView.SelectedItem, ScrollToPosition.Center, animated: true);
                        }
                        else
                        {
                            _focusItems.FocusNextItem(true);
                        }
                    });
                    break;

                case KeyboardNavigationActionEnum.Left:
                case KeyboardNavigationActionEnum.Up:
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        _focusItems.FocusPreviousItem(true);
                    });
                    break;

                case KeyboardNavigationActionEnum.Back:
                    //
                    break;

                case KeyboardNavigationActionEnum.OK:

                    switch (_focusItems.FocusedItemName)
                    {
                        case "ChannelsListView":
                            Task.Run(async () =>
                            {
                                await ActionPlay();
                            });
                            break;
                        case "SettingsButton":
                            MainThread.BeginInvokeOnMainThread(async () =>
                            {
                                SettingsButton_Clicked(this, new EventArgs());
                            });
                            break;
                        case "TuneButton":
                            MainThread.BeginInvokeOnMainThread(async () =>
                            {
                                TuneButton_Clicked(this, new EventArgs());
                            });
                            break;
                        case "DVBTTelevizorButton":
                            MainThread.BeginInvokeOnMainThread(async () =>
                            {
                                DVBTTelevizorButton_Clicked(this, new EventArgs());
                            });
                            break;
                    }

                    break;
            }
        }

        public void OnTextSent(string text)
        {

        }

        public static void SetToolBarColors(NavigationPage navigationPage, Color textColor, Color background)
        {
            if (navigationPage != null)
            {
                navigationPage.BarBackgroundColor = background;
                navigationPage.BarTextColor = textColor;
            }
        }

        private void ChannelsListView_ItemTapped(object sender, SelectedItemChangedEventArgs e)
        {
            _loggingService.Debug("ChannelsListView_ItemTapped");
        }

        private void ChannelsListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            _loggingService.Debug("ChannelsListView_ItemSelected");
        }

        private void OnMenuKeyDown(KeyboardNavigationActionEnum keyAction)
        {
            switch (keyAction)
            {
                case KeyboardNavigationActionEnum.Right:
                case KeyboardNavigationActionEnum.Down:
                    _focusMenuItems.FocusNextItem(true);
                    break;

                case KeyboardNavigationActionEnum.Left:
                case KeyboardNavigationActionEnum.Up:
                     _focusItems.FocusPreviousItem(true);
                    break;

                case KeyboardNavigationActionEnum.Back:
                    _viewModel.MenuVisible = false;
                    break;

                case KeyboardNavigationActionEnum.OK:

                    switch (_focusMenuItems.FocusedItemName)
                    {
                        case "MenuButtonClose":
                            _viewModel.MenuVisible = false;
                            break;
                        case "MenuButtonSettings":
                            _viewModel.CommandSettings.Execute(null);
                            break;
                    }

                    break;
            }
        }

        private void BuildMenu()
        {
            var menuItems = new KeyboardFocusableItemList();

            if ((_viewModel.Channels.Count > 0) && (_viewModel.SelectedChannel != null))
            {
                if (_viewModel.PlayingState == PlayingStateEnum.Playing)
                {
                    menuItems.AddItem(_focusMenuItems.GetItemByName("MenuButtonStop"));

                    menuItems.AddItem(_focusMenuItems.GetItemByName("MenuButtonSubtitles"));
                    menuItems.AddItem(_focusMenuItems.GetItemByName("MenuButtonAudio"));
                    menuItems.AddItem(_focusMenuItems.GetItemByName("MenuButtonAspect"));
                    menuItems.AddItem(_focusMenuItems.GetItemByName("MenuButtonTeletext"));
                }
                else
                {
                    menuItems.AddItem(_focusMenuItems.GetItemByName("MenuButtonPlay"));
                }

                if (_viewModel.RecordingChannel == null)
                {
                    menuItems.AddItem(_focusMenuItems.GetItemByName("MenuButtonRecord"));
                }
                else
                {
                    menuItems.AddItem(_focusMenuItems.GetItemByName("MenuButtonStopRecord"));
                }
            }

            if (_viewModel.EPGDetailVisible)
            {
                menuItems.AddItem(_focusMenuItems.GetItemByName("MenuButtonHideEPG"));
            } else
            {
                menuItems.AddItem(_focusMenuItems.GetItemByName("MenuButtonShowEPG"));
            }

            menuItems.AddItem(_focusMenuItems.GetItemByName("MenuButtonSettings"));
            menuItems.AddItem(_focusMenuItems.GetItemByName("MenuButtonQuit"));

            menuItems.AddItem(_focusMenuItems.GetItemByName("MenuButtonClose"));

            var pos = 0.1;
            var step = 1.0 / menuItems.Items.Count;

            _focusMenuItems.VisibleAll(false);

            AbsoluteLayout.SetLayoutBounds(MenuFrame, new Rect(0.5, 0.5, 0.65, 0.65));

            foreach (var item in menuItems.Items)
            {
                item.IsVisible = true;

                var view = _focusMenuItems.GetFirstViewByItemName(item.Name);

                AbsoluteLayout.SetLayoutFlags(view, AbsoluteLayoutFlags.All);

                if (item.Name == "MenuButtonClose")
                {
                    AbsoluteLayout.SetLayoutBounds(view, new Rect(0.5, pos, 0.35, step*0.7));
                } else
                {
                    AbsoluteLayout.SetLayoutBounds(view, new Rect(0.5, pos, 0.85, step * 0.7));
                }

                if (item.Name == "MenuButtonHideEPG" || item.Name == "MenuButtonShowEPG")
                {
                    //step += 0.02;
                }

                pos += step;
            }
        }
    }

}
