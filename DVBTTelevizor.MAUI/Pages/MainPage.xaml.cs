using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using DVBTTelevizor.TV;
using LibVLCSharp.Shared;
using LoggerService;
using Microsoft.Maui;
using Microsoft.Maui.Animations;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Layouts;
using NLog.LayoutRenderers.Wrappers;
using RTLSDR;
using RTLSDR.Audio;
using RTLSDR.Common;
using RTLSDR.DAB;
using RTLSDR.FM;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;
using System.Windows.Input;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace DVBTTelevizor.MAUI
{
    public partial class MainPage : ContentPage, IOnKeyDown
    {
        private MainViewModel _viewModel;
        private ILoggingService _loggingService { get; set; }
        private IDriverConnector _driver { get; set; }
        private ITVConfiguration _configuration;
        private IRTLSDRDriverPlatformImplementation _sdrDriverPlatformImplementation;
        public string PublicDirectory { get; set; }
        private string _currentTeletextNum = null;
        private bool _fixVideoNeeded = false;
        private bool _lastTimeHome = false;

        private VLCMediaInput _vlcRawAudioInput = null;

        private TestDVBTDriver _testDVBTDriver = null;
        private RemoteAccessService.RemoteAccessService _remoteAccessService;
        private List<string> _remoteDevicesConnected = new List<string>();

        private IPublicDirectoryProvider _publicDirectoryProvider = null;

        private bool _firstAppearing = true;
        private DateTime _lastActionPlayTime = DateTime.MinValue;
        private bool _lastDataAnimation = false;
        private Size _lastAllocatedSize = new Size(-1, -1);

        private DateTime _lastBackPressedTime = DateTime.MinValue;
        private DateTime _lastNumPressedTime = DateTime.MinValue;
        private DateTime _lastLongTappedTime = DateTime.MinValue;
        private string _numberPressed = System.String.Empty;

        private Channel[] _lastPlayedChannels = new Channel[2];

        private KeyboardFocusableItemList _focusItems;
        private List<MenuItem> _menuItems = new List<MenuItem>();
        private List<MenuItem> _subtitleMenuItems = new List<MenuItem>();
        private List<MenuItem> _audioMenuItems = new List<MenuItem>();
        private List<MenuItem> _aspectMenuItems = new List<MenuItem>();
        private List<MenuItem> _teletextMenuItems = new List<MenuItem>();

        private List<MenuItem> _activeMenuItems = null;

        private static SemaphoreSlim _semaphoreSlimForRefreshGUI = new SemaphoreSlim(1, 1);
        private bool _refreshGUIEnabled = true;
        private bool _menuShowEnabled = true;

        private bool _checkStreamEnabled = true;
        public Microsoft.Maui.Controls.Command CommandCheckStream { get; set; }
        public Microsoft.Maui.Controls.Command CommandUpdateDriverState { get; set; }

        private LibVLC? _LibVLC;
        private LibVLCSharp.Shared.MediaPlayer? _mediaPlayer;
        private Media _media;
        private SledovaniTV.SledovaniTV _iptv;

        private SettingsPage _settingsPage = null;
        private TuningDriverPage _tuningPage = null;
        private AboutPage _aboutPage = null;
        private SelectDriverPage _driverPage = null;
        private ChannelPage _channelPage = null;
        private FilterPage _filterPage = null;

        private AppMenu _appMenu = null;

        private bool IsPortrait { get; set; } = false;

        // Menu: 0.0,0,1.0,0.1

        private Rect FullScreenVideoPosition { get; } = new Rect(0.5, 0.5, 1.0, 1.0);

        // EPGDetailGrid
        private Rect EPGDetailGridLandscapePosition { get; } = new Rect(1.0, 1.0, 0.3, 0.9);
        private Rect EPGDetailGridLandscapePositionForPreview { get; } = new Rect(1.0, 0.24, 0.3, 0.6);
        private Rect EPGDetailGridLandscapePositionForPlay { get; } = new Rect(1.0, 1.0, 0.3, 1.0);

        private Rect EPGDetailGridPortraitPosition { get; } = new Rect(0.0, 1.0, 1.0, 0.3);
        private Rect EPGDetailGridPortraitPositionForPreview { get; } = new Rect(0.0, 0.75, 1.0, 0.2);
        private Rect EPGDetailGridPortraitPositionForPlay { get; } = new Rect(0.0, 1.0, 1.0, 0.3);


        // VideoStackLayout
        private Rect LandscapePreviewVideoStackLayoutPosition { get; } = new Rect(1.0, 1.0, 0.3, 0.3);
        private Rect VideoStackLayoutLandscapePositionWhenEPGDetailVisibleForPreview { get; } = new Rect(1, 1, 0.3, 0.3);
        private Rect VideoStackLayoutLandscapePositionWhenEPGDetailVisibleForPlay { get; } = new Rect(0.0, 0.0, 0.7, 1.0);
        private Rect VideoStackLayoutLandscapePositionWhenEPGDetailNotVisibleForPreview { get; } = new Rect(1.0, 1.0, 0.3, 0.9);
        private Rect VideoStackLayoutPortraitPositionWhenEPGDetailVisibleForPreview { get; } = new Rect(0.0, 1.0, 1.0, 0.2);
        private Rect VideoStackLayoutPortraitPositionWhenEPGDetailVisibleForPlay { get; } = new Rect(0.0, 0.0, 1.0, 0.7);
        private Rect VideoStackLayoutPortraitPositionForPreview { get; } = new Rect(0.0, 1.0, 1.0, 0.3);

        // VideoStackLayout must be visible when initializing VLC window!
        private Rect NoVideoStackLayoutPosition { get; set; } = new Rect(-10, -10, -5, -5);

        // ChannelsListView
        private Rect ChannelsListViewLandscapePositionWhenEPGDetailVisibleForPreview { get; } = new Rect(0.0, 1.0, 0.7, 0.9);
        private Rect ChannelsListViewPositionWhenEPGDetailNOTVisible { get; } = new Rect(0.5, 1, 1, 0.9);
        private Rect ChannelsListViewPortraitPositionWhenEPGDetailVisible { get; } = new Rect(0.5, 0.24, 1.0, 0.6);
        private Rect ChannelsListViewPortraitPositionWhenEPGDetailNOTVisible { get; } = new Rect(0.5, 1, 1.0, 0.9);
        private Rect ChannelsListPortraitPositionForPreview { get; } = new Rect(0.5,0.25,1.0,0.46);
        private Rect ChannelsListViewPortraitPositionWhenEPGDetailVisibleForPreview { get; } = new Rect(0.5, 0.2, 1.0, 0.5);

        private Rect? LastVideoStackLayoutPosition { get; set; }

        public MainPage(ILoggingProvider loggingProvider, IPublicDirectoryProvider publicDirectoryProvider, ITVConfiguration tvConfiguration, IRTLSDRDriverPlatformImplementation sdrDriverPlatformImplementation)
        {
            _publicDirectoryProvider = publicDirectoryProvider;
            _sdrDriverPlatformImplementation = sdrDriverPlatformImplementation;
            PublicDirectory = publicDirectoryProvider.GetPublicDirectoryPath();

            _configuration = tvConfiguration;

            Task.Run(async () =>
            {
                if (!_configuration.UpdatedTo2026)
                {
                    await ExtractAssetFile("Arabic_AI.lng");
                    await ExtractAssetFile("Azerbaijani_AI.lng");
                    await ExtractAssetFile("Bengali_AI.lng");
                    await ExtractAssetFile("Czech.lng");
                    await ExtractAssetFile("French_AI.lng");
                    await ExtractAssetFile("German_AI.lng");
                    await ExtractAssetFile("Polish_AI.lng");
                    await ExtractAssetFile("Italian_AI.lng");
                    await ExtractAssetFile("Hindi_AI.lng");
                    await ExtractAssetFile("Indonesian_AI.lng");
                    await ExtractAssetFile("MandarinChinese_AI.lng");
                    await ExtractAssetFile("Portuguese_AI.lng");
                    await ExtractAssetFile("Spanish_AI.lng");
                    await ExtractAssetFile("Ukrainian_AI.lng");
                    await ExtractAssetFile("Urdu_AI.lng");
                    await ExtractAssetFile("Japanese_AI.lng");
                    await ExtractAssetFile("Vietnamese_AI.lng");
                    await ExtractAssetFile("Greek_AI.lng");
                    await ExtractAssetFile("Thai_AI.lng");
                    await ExtractAssetFile("Russian_AI.lng");

                    _configuration.UpdatedTo2026 = true;
                }

                // language
                Lng.LoadLanguages(Path.Join(PublicDirectory, "lng"));

                if (!System.String.IsNullOrEmpty(_configuration.Language))
                {
                    var languageFileName = Path.Join(PublicDirectory, "lng", $"{_configuration.Language}.lng");

                    if (File.Exists(languageFileName))
                    {
                        Lng.LoadLanguage(languageFileName);
                    }
                }
            });

            InitializeComponent();

            _appMenu = new AppMenu(MainMenu);
            _appMenu.FontSize = _configuration.AppFontSize;
            _appMenu.MenuVisibleChanged += _appMenu_MenuVisibleChanged;

            var forceLoggingWhenDebug = false;

#if DEBUG
            forceLoggingWhenDebug = true;
#endif

            if (forceLoggingWhenDebug || _configuration.EnableLogging)
            {
                _loggingService = loggingProvider.GetLoggingService();
            } else
            {
                _loggingService = new DummyLoggingService();
            }

            _loggingService.Info("MainPage starting");

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                _loggingService.Error(e.ExceptionObject as Exception);
            };
            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                _loggingService.Error(e.Exception);
            };

            _configuration.ConfigDirectory = PublicDirectory;

            InitDriver();

            _iptv = new SledovaniTV.SledovaniTV(_loggingService);
            _iptv.SetCredentials(_configuration.SledovaniTVUserName, _configuration.SledovaniTVPassword, _configuration.SledovaniTVPIN);
            _iptv.SetDeviceCredential(_configuration.SledovaniTVDeviceID, _configuration.SledovaniTVDevicePassword);
            if (_configuration.ShowNonFreeChannels)
            {
                Task.Run(async () => { await _iptv.Unlock(); });
            }

            BindingContext = _viewModel = new MainViewModel(_loggingService, _driver, _iptv, tvConfiguration, publicDirectoryProvider);

            _settingsPage = new SettingsPage(_loggingService, _driver, _iptv, _configuration, publicDirectoryProvider);
            _aboutPage = new AboutPage(_loggingService, _driver, _configuration, publicDirectoryProvider);
            _driverPage = new SelectDriverPage(_loggingService, _driver, _configuration, publicDirectoryProvider);

            _channelPage = new ChannelPage(_loggingService, _driver, _configuration, publicDirectoryProvider);
            _tuningPage = new TuningDriverPage(_loggingService, _driver, _configuration, _publicDirectoryProvider);
            _filterPage = new FilterPage(_loggingService, _driver, _configuration, publicDirectoryProvider);

            _channelPage.Disappearing += _channelPage_Disappearing;
            _filterPage.Disappearing += _filterPage_Disappearing;
            _tuningPage.Disappearing += _tuneWelcomePage_Disappearing;

            NavigationPage.SetHasNavigationBar(this, false);

            BuildFocusableItems();

            _remoteAccessService = new RemoteAccessService.RemoteAccessService(_loggingService);

            RestartRemoteAccessService();

            SubscribeMessages();

             _settingsPage.Disappearing += delegate
            {
                Task.Run(async () =>
                {
                    await _viewModel.RefreshChannels();
                });
            };

            CommandCheckStream = new Microsoft.Maui.Controls.Command(() =>
            {
                Task.Run(async () =>
                {
                    await CheckStream();
                });
            });

            CommandUpdateDriverState = new Microsoft.Maui.Controls.Command(() =>
            {
                Task.Run(async () =>
                {
                    await UpdateDriverState();
                });
            });

            if (!System.String.IsNullOrEmpty(_configuration.LoggingUDPIP))
            {
                Task.Run(async () =>
                {
                    await Task.Delay(11000); // wait to ensure the MainActivity has subsribed the message, log from first 10 seconds can be found in Public directory

                    _loggingService.Info($"Setting UDP logging IP: {_configuration.LoggingUDPIP}");
                    var addr = $"udp4://{_configuration.LoggingUDPIP}:9999";
                    WeakReferenceMessenger.Default.Send(new SetUDPLoggingIPMessage(addr));
                });
            }

            if (_configuration.WriteToExternalDevice && !string.IsNullOrWhiteSpace(_configuration.ExternalDevicePathUri))
            {
                Task.Run(async () =>
                {
                    await Task.Delay(10000); // wait to ensure the MainActivity has subsribed the message
                    WeakReferenceMessenger.Default.Send(new ExternalDeviceWriteAccessRestore(_configuration.ExternalDevicePathUri));
                });
            }

            if (_configuration.EnableLogging)
            {
                Task.Run(async () =>
                {
                    await Task.Delay(10000); // wait to ensure the MainActivity has subsribed the message, log from first 10 seconds can be found in Public directory

                    WeakReferenceMessenger.Default.Send(new EnableLoggingMessage(System.String.Empty));
                });
            }

            if (_configuration.SledovaniTVEnabled)
            {
                Task.Run(async () =>
                {
                    await Task.Delay(2000);

                    await _iptv.GetChannels(); // just for logging to service
                });
            }

            BackgroundCommandWorker.RunInBackground(CommandCheckStream, 3, 10);
            BackgroundCommandWorker.RunInBackground(CommandUpdateDriverState, 3, 5);
        }

        private void _appMenu_MenuVisibleChanged(object? sender, MenuVisibleChangedEventArgs e)
        {
            //_viewModel.MenuVisible = e.IsVisible;
        }

        public void SetFixVideNeeded()
        {
            _fixVideoNeeded = true;
        }

        private void _tuneWelcomePage_Disappearing(object? sender, EventArgs e)
        {
            Task.Run(async () =>
            {
                await _viewModel.RefreshChannels();
            });
        }

        private void _filterPage_Disappearing(object? sender, EventArgs e)
        {
            Task.Run(async () =>
            {
                await _viewModel.RefreshChannels();
            });
        }

        private async Task ExtractAssetFile(string sourceFileName)
        {
            try
            {
                string lngFolder = Path.Combine(PublicDirectory, "lng");
                if (!Directory.Exists(lngFolder))
                {
                    Directory.CreateDirectory(lngFolder);
                }

                string destPath = Path.Combine(lngFolder, sourceFileName);

                // always update
                if (File.Exists(destPath))
                {
                    File.Delete(destPath);
                }

                using var stream = await FileSystem.OpenAppPackageFileAsync(sourceFileName);
                using var destStream = File.Create(destPath);
                await stream.CopyToAsync(destStream);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error extracting asset file: {ex}");
            }
        }

        public ITVConfiguration Configuration
        {
            get
            {
                return _configuration;
            }
        }

        private void SubscribeMessages()
        {
            WeakReferenceMessenger.Default.Register<SelectedChannelChangedMessage>(this, (r, m) =>
            {
                Task.Run(async () => await FocusSelectedChannel());
            });

            WeakReferenceMessenger.Default.Register<ToastMessage>(this, (r, m) =>
            {
                WeakReferenceMessenger.Default.Send(new SizedToastMessage(
                    new SizedToast
                    {
                        Message = m.Value,
                        AppFontSize = _configuration.AppFontSize
                    }));
            });

            WeakReferenceMessenger.Default.Register<KeyDownMessage>(this, (r, m) =>
            {
                OnKeyDown(m.Value, m.Long);
            });

            WeakReferenceMessenger.Default.Register<SendConnectDriverRequestMessage>(this, (r, m) =>
            {
                SendConnectDriverRequest();
            });

            WeakReferenceMessenger.Default.Register<InitDriverMessage>(this, (r, m) =>
            {
                InitDriver();
                InitializeVLC();
                SendConnectDriverRequest();
            });

            WeakReferenceMessenger.Default.Register<FinishTuningMessage>(this, (r, m) =>
            {
                _loggingService.Info($"FinishTuning");

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Navigation.PopToRootAsync();
                });

                Task.Run(async () => await _viewModel.RefreshChannels());
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
                Task.Run(async () => await CheckDriver());
            });

            WeakReferenceMessenger.Default.Register<RefreshGUIMessage>(this, (r, m) =>
            {
                RefreshGUI();
            });

            WeakReferenceMessenger.Default.Register<InstallDriverMessage>(this, (r, m) =>
            {
                WeakReferenceMessenger.Default.Send(new OpenURLMessage("https://play.google.com/store/apps/details?id=info.martinmarinov.dvbdriver"));
            });

            WeakReferenceMessenger.Default.Register<ChangedWindowPositionMessage>(this, (r, m) =>
            {
                UpdateVideoWindowPosition();
            });

            WeakReferenceMessenger.Default.Register<SetAudioTrackMessage>(this, (r, m) =>
            {
                SetAudio(m.Value);
            });

            WeakReferenceMessenger.Default.Register<SetSubtitlesMessage>(this, (r, m) =>
            {
                SetSubtitles(m.Value);
            });

            WeakReferenceMessenger.Default.Register<DriverChangedMessage>(this, (r, m) =>
            {
                UpdateMenu();
            });
        }

        private async Task UpdateDriverState()
        {
            //_loggingService.Debug($"Updating bitrate");

            try
            {
                long bitrate = 0;
                DVBTDriverStatus state = new DVBTDriverStatus()
                {
                    rfStrengthPercentage = 0
                };

                if (_driver != null)
                {
                    bitrate = _driver.Bitrate;
                }

                WeakReferenceMessenger.Default.Send(new DriverUpdateStateMessage(
                    new DriverState()
                    {
                        BitRate = DVBTDriverConnector.GetHumanReadableBitRate(_driver == null ? 0 : bitrate),
                        Frequency = DVBTDriverConnector.GetHumanReadableFrequency(_driver == null ? null : _driver.LastTunedFreq)
                    }
                    ));
            } catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
        }

        private async Task CheckStream()
        {
            if (!_checkStreamEnabled || (PlayingState == PlayingStateEnum.Stopped))
                return;

            _loggingService.Debug($"CheckStream");

            try
            {
                if (videoView.MediaPlayer == null)
                    return;

                // checking stopped stream
                if (!videoView.MediaPlayer.IsPlaying)
                {
                    _loggingService.Debug($"CheckStream - stopped media detected");
                    videoView.MediaPlayer.Stop();
                    videoView.MediaPlayer.Play();
                }

                // checking no video
                var videoTracksCount = videoView.MediaPlayer.VideoTrackCount;

                //status += $"   V: {videoTracksCount} ({videoView.MediaPlayer.VideoTrack})" + Environment.NewLine;
                //status += $"   A: {videoView.MediaPlayer.AudioTrackCount} ({videoView.MediaPlayer.AudioTrack})" + Environment.NewLine;
                //status += $"   S: {videoView.MediaPlayer.SpuCount} ({videoView.MediaPlayer.Spu})" + Environment.NewLine;

                //_loggingService.Debug(status);

                if (videoTracksCount <= 0)
                {
                    if (_viewModel.VideoStackLayoutVisible)
                    {
                        _loggingService.Debug($"CheckStream - no video detected");

                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            _viewModel.SetVideoStackLayoutvisible(false);
                            VideoStackLayout.IsVisible = false;
                            NoVideoStackLayout.IsVisible = true;
                        });
                    }
                }
                else
                {
                    if (_viewModel.NoVideoStackLayoutVisible)
                    {
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            _loggingService.Debug($"CheckStream - video detected");

                            _viewModel.SetVideoStackLayoutvisible(true);
                            VideoStackLayout.IsVisible = true;
                            NoVideoStackLayout.IsVisible = false;
                        });
                    }

                    if (_viewModel.PlayingChannel != null)
                    {
                        foreach (var track in _mediaPlayer.Media.Tracks)
                        {
                            if (track.TrackType == TrackType.Video)
                            {
                                _viewModel.PlayingChannel.VideoSize = $"{track.Data.Video.Width}x{track.Data.Video.Height}";
                            }
                        }
                    }

                    //PreviewVideoBordersFix();
                }


                // check do data from driver

                if (_lastActionPlayTime != DateTime.MinValue)
                {
                    var dataAvailable = true;

                    if (_viewModel.PlayingChannel != null &&
                        _viewModel.PlayingChannel.ChannelType != ChannelTypeEnum.SledovaniTV)
                    {
                        dataAvailable = _driver.DriverStreamDataAvailable;
                    }

                    if (!dataAvailable)
                    {
                        var timeFromPlayMSecs = (DateTime.Now - _lastActionPlayTime).TotalMilliseconds;
                        if (timeFromPlayMSecs > 10000)
                        {
                            _loggingService.Info($"CheckStream - no data for {timeFromPlayMSecs} ms");
                            /*
                            MessagingCenter.Send("", BaseViewModel.MSG_StopStream);
                            MessagingCenter.Send($"Error - no data from device", BaseViewModel.MSG_ToastMessage);
                            */
            }
                        else if (timeFromPlayMSecs > 5000)
                        {
                            _loggingService.Info($"CheckStream - no data for {timeFromPlayMSecs} ms");
                        }
                    } else
                    {
                        _lastDataAnimation = !_lastDataAnimation;
                        await NoVideoImage.ScaleTo(_lastDataAnimation ? 1.2 : 0.8, 3000);
                    }
                }

                var actualSubtitleTrack = videoView.MediaPlayer.Spu;
                var actualAudioTrack = videoView.MediaPlayer.AudioTrack;

                //_loggingService.Debug($"CheckStream - actual subtitle track: {actualSubtitleTrack}");
                //_loggingService.Debug($"CheckStream - actual audio track: {actualAudioTrack}");

                if (_viewModel.PlayingChannel != null)
                {
                    int firstAudioTrackId = -1;
                    foreach (var desc in videoView.MediaPlayer.VideoTrackDescription)
                    {
                        if (!_viewModel.PlayingChannel.VideoTracks.ContainsKey(desc.Id))
                        {
                            _loggingService.Debug($"     - video found: {desc.Name} [{desc.Id}]");
                            _viewModel.PlayingChannel.VideoTracks.Add(desc.Id, desc.Name);
                        }
                    }
                    foreach (var desc in videoView.MediaPlayer.SpuDescription)
                    {
                        if (!_viewModel.PlayingChannel.Subtitles.ContainsKey(desc.Id))
                        {
                            _loggingService.Debug($"     - subtitles found: {desc.Name} [{desc.Id}]");
                            _viewModel.PlayingChannel.Subtitles.Add(desc.Id, desc.Name);
                        }
                    }
                    foreach (var desc in videoView.MediaPlayer.AudioTrackDescription)
                    {
                        firstAudioTrackId = desc.Id;
                        if (!_viewModel.PlayingChannel.AudioTracks.ContainsKey(desc.Id))
                        {
                            _loggingService.Debug($"     - audio track found: {desc.Name} [{desc.Id}]");
                            _viewModel.PlayingChannel.AudioTracks.Add(desc.Id, desc.Name);
                        }
                    }

                    // check audio track

                    if (actualAudioTrack == -1 &&
                        firstAudioTrackId != -1 &&
                        _viewModel.PlayingChannel.SelectedAudioTrack != "-1") // when user set NO audio
                    {
                        _loggingService.Debug($"CheckStream - invalid current audio track");

                        int trackId;
                        if (!int.TryParse(_viewModel.PlayingChannel.SelectedAudioTrack, out trackId))
                        {
                            trackId = firstAudioTrackId;
                        }

                        videoView.MediaPlayer.SetAudioTrack(trackId);
                    }
                }

                //var videoBounds = AbsoluteLayout.GetLayoutBounds(VideoStackLayout);
                //_loggingService.Info($"Video bounds: {videoBounds}");

                if (_viewModel.PlayingChannelAspect.Width == -1)
                {
                    // setting aspect ratio

                    var videoTrack = GetVideoTrack();
                    if (videoTrack.HasValue)
                    {
                        _viewModel.PlayingChannelAspect = new Size(videoTrack.Value.Data.Video.Width, videoTrack.Value.Data.Video.Height);
                        _loggingService.Debug($"CheckStream - Video size: {_viewModel.PlayingChannelAspect.Width}:{_viewModel.PlayingChannelAspect.Height}");
                    }
                }

                /*
                if ((!_viewModel.TeletextEnabled) && (actualSubtitleTrack != _viewModel.Subtitles))
                {
                    _loggingService.Debug($"CheckStream - invalid subtitles {actualSubtitleTrack}, setting {_viewModel.Subtitles}");
                    videoView.MediaPlayer.SetSpu(_viewModel.Subtitles);
                }
                */

                // check video position

                var pos = videoView.Bounds;
                _loggingService.Debug($"VideoStackLayout.Bounds: {VideoStackLayout.Bounds}");

            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "CheckStream general error");
            }
        }

        private LibVLCSharp.Shared.MediaTrack? GetVideoTrack()
        {
            if (_media != null &&
                _media.Tracks != null &&
                _media.Tracks.Length > 0 &&
                _mediaPlayer != null &&
                _mediaPlayer.VideoTrackCount > 0 &&
                _mediaPlayer.VideoTrack != -1)
            {
                foreach (var track in _media.Tracks)
                {
                    if (track.Data.Video.Width == 0 ||
                        track.Data.Video.Height == 0)
                        continue;

                    return track;
                }

                return null;
            }
            else
            {
                return null;
            }
        }

        private LibVLCSharp.Shared.MediaTrack? GetTeletextTrack()
        {
            if (_media != null &&
                _media.Tracks != null &&
                _media.Tracks.Length > 0)
            {
                foreach (var track in _media.Tracks)
                {
                    if (track.TrackType == TrackType.Text)
                    {
                        return track;
                    }
                }
            }

            return null;
        }

        private void _channelPage_Disappearing(object? sender, EventArgs e)
        {
            _viewModel.RefreshChannels();
        }

        private async Task CheckDriver()
        {
            switch (_driver.State)
            {
                case DVBTDriverStateEnum.Unknown:
                case DVBTDriverStateEnum.Disconnected:
                    {
                        SendConnectDriverRequest();
                        break;
                    }
                default:
                    {
                        // check driver state
                        await CheckDriverState();
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

        private void InitDriver()
        {
            switch (_configuration.DVBTDriverType)
            {
                case DriverTypeEnum.AndroidDVBTDriver:
                    _driver = new DVBTDriverConnector(_loggingService);
                    break;
                case DriverTypeEnum.AndroidTestingDVBTDriver:
                    _driver = new DVBTDriverConnector(_loggingService);
                    break;
                case DriverTypeEnum.TestTuneDriver:
                    _driver = new TestTuneConnector(_loggingService);
                    break;
                case DriverTypeEnum.RTLSDRDriverFM:

                    var fmDemodulator = new FMDemodulator(_loggingService);

                    _driver = new RTLSDRFMDriverConnector(_loggingService,
                        _sdrDriverPlatformImplementation.GetRTLSDRDriver(),
                        fmDemodulator);

                        fmDemodulator.Start();
                    break;
                case DriverTypeEnum.RTLSDRDriverDAB:
                        var dabDemoduator = new DABProcessor(_loggingService);

                        dabDemoduator.OnServiceFound += DabDemoduator_OnServiceFound;
                        //dabDemoduator.OnServicePlayed += DABProcessor_OnServicePlayed;

                        _driver = new RTLSDRDABDriverConnector(_loggingService,
                            _sdrDriverPlatformImplementation.GetRTLSDRDriver(),
                            dabDemoduator);

                        dabDemoduator.Start();
                        _driver.SetGain(GainEnum.Auto);
                        _driver.Tune(199360000, 1024, 0);
                    break;
                default:
                    _driver = new TestTuneConnector(_loggingService);
                    break;
            }

            _driver.OnRawAudioDemodulated += _driver_OnRawAudioDemodulated;

            WeakReferenceMessenger.Default.Send(new DriverChangedMessage(_driver));
        }

        private void DabDemoduator_OnServiceFound(object? sender, EventArgs e)
        {
            if (e is DABServiceFoundEventArgs de)
            {
                _loggingService.Info($"DAB service found: {de.Service}");
            }
        }

        private void ProcessAACAudioData(AACDataDemodulatedEventArgs ed)
        {
            try
            {
                // Combine ADTS header and AAC payload into a single buffer before sending to the player/UDP.
                var adtsHeaderLength = ed.ADTSHeader?.Length ?? 0;
                var dataLength = ed.Data?.Length ?? 0;
                var adtsFrame = new byte[adtsHeaderLength + dataLength];
                if (ed.ADTSHeader != null)
                {
                    Buffer.BlockCopy(ed.ADTSHeader, 0, adtsFrame, 0, adtsHeaderLength);
                }
                if (ed.Data != null)
                {
                    Buffer.BlockCopy(ed.Data, 0, adtsFrame, adtsHeaderLength, dataLength);
                }

                if (_vlcRawAudioInput == null)
                {
                    _vlcRawAudioInput = new VLCMediaInput();

                    var mediaOptions = new[]
                        {
                        ":demux=aac",
                        ":live-caching=0",
                        ":network-caching=0",
                        ":file-caching=0",
                        ":sout-mux-caching=0"
                    };

                    _media = new Media(_LibVLC, _vlcRawAudioInput, mediaOptions);

                    _mediaPlayer.Play(_media);
                }

                _vlcRawAudioInput.PushData(adtsFrame);

            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
        }


        private void ProcessPCMAudioData(DataDemodulatedEventArgs ed)
        {
            try
            {
                    if (_vlcRawAudioInput ==null)
                    {
                    _vlcRawAudioInput = new VLCMediaInput();

                    var mediaOptions = new[]
                            {
                            ":demux=rawaud",
                            $":rawaud-channels={ed.AudioDescription.Channels}",
                            $":rawaud-samplerate={ed.AudioDescription.SampleRate}",
                            ":live-caching=50",
                            ":file-caching=50",
                            ":clock-jitter=0",
                            ":clock-synchro=0",
                            ":rawaud-fourcc=s16l"
                        };

                    _media = new Media(_LibVLC, _vlcRawAudioInput, mediaOptions);

                    _mediaPlayer.Play(_media);
                }

                _vlcRawAudioInput.PushData(ed.Data);

            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
        }

        private void _driver_OnRawAudioDemodulated(object sender, EventArgs e)
        {
            if (_LibVLC == null)
                return;

            if (e is AACDataDemodulatedEventArgs ed)
            {
                if (ed.Data == null || ed.Data.Length == 0)
                {
                    return;
                }

                ProcessAACAudioData(ed);
            }
            else
            {
                if (e is DataDemodulatedEventArgs dd)
                {

                    if (dd.Data == null || dd.Data.Length == 0)
                    {
                        return;
                    }

                    ProcessPCMAudioData(dd);
                }
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

            try
            {

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
            } catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
        }

        private void BuildFocusableItems()
        {
            _focusItems = new KeyboardFocusableItemList();

            _focusItems
                .AddItem(KeyboardFocusableItem.CreateFrom("DVBTTelevizorButton", new List<View>() { DVBTTelevizorButton }))
                .AddItem(KeyboardFocusableItem.CreateFrom("ChannelsListView", new List<View>() { ChannelsListView }))
                .AddItem(KeyboardFocusableItem.CreateFrom("InstallDriverButton", new List<View>() { QuickDriverButton }))
                .AddItem(KeyboardFocusableItem.CreateFrom("QuickTuneButton", new List<View>() { QuickTuneButton }))
                .AddItem(KeyboardFocusableItem.CreateFrom("EPGDetailGrid", new List<View>() { EPGDetailGrid }))
                .AddItem(KeyboardFocusableItem.CreateFrom("DriverStateButton", new List<View>() { DriverStateButton }))
                .AddItem(KeyboardFocusableItem.CreateFrom("TuneButton", new List<View>() { TuneButton }))
                .AddItem(KeyboardFocusableItem.CreateFrom("MenuButton", new List<View>() { MenuButton }));

            _focusItems.OnItemFocusedEvent += _focusItems_OnItemFocusedEvent;
        }

        public async Task FocusSelectedChannel()
        {
            _viewModel.NotifyChannelChange();


            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (_viewModel.SelectedChannel == null)
                {
                    return;
                }

                _viewModel.SelectedChannel.Focused = true;
                _viewModel.SelectedChannel.NotifyChanges();

                ChannelsListView.ScrollTo(_viewModel.SelectedChannel, ScrollToPosition.MakeVisible, false);
            });
        }

        private void _focusItems_OnItemFocusedEvent(KeyboardFocusableItemEventArgs _args)
        {
            if (_focusItems.FocusedItemName == "ChannelsListView")
            {
                Task.Run(async () => await FocusSelectedChannel());
            }

            _viewModel.EPGDetailFocused = (_focusItems.FocusedItemName == "EPGDetailGrid");
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            _loggingService.Debug($"OnSizeAllocated: {width}/{height}");

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

            WeakReferenceMessenger.Default.Send(new ChangedSizeMessage(new Size(width, height)));

            RefreshGUI();
        }

        private void UpdateMenu()
        {
            var driverName = BaseViewModel.GetDVBTDriverShortName(_configuration.DVBTDriverType);

            if (IsPortrait)
            {
                DVBTTelevizorButton.BottomTitleText = "DVBT Televizor".Translated();
                DriverStateButton.BottomTitleText = driverName;
                TuneButton.BottomTitleText = "Tune".Translated();
                MenuButton.BottomTitleText = "Menu".Translated();

                DVBTTelevizorButton.TopTitleText =
                DriverStateButton.TopTitleText =
                TuneButton.TopTitleText =
                MenuButton.TopTitleText = "";

            }
            else
            {
                DVBTTelevizorButton.TopTitleText = "DVBT Televizor".Translated();
                DriverStateButton.TopTitleText = driverName;
                TuneButton.TopTitleText = "Tune".Translated();
                MenuButton.TopTitleText = "Menu".Translated();

                DVBTTelevizorButton.BottomTitleText =
                DriverStateButton.BottomTitleText =
                TuneButton.BottomTitleText =
                MenuButton.BottomTitleText = "";
            }
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
                    //await _semaphoreSlimForRefreshGUI.WaitAsync();

                    UpdateMenu();

                    AbsoluteLayout.SetLayoutFlags(VideoStackLayout, AbsoluteLayoutFlags.All);
                    AbsoluteLayout.SetLayoutFlags(NoVideoStackLayout, AbsoluteLayoutFlags.All);

                    //_loggingService.Debug($"PlayingState: {PlayingState}");

                    switch (PlayingState)
                    {
                        case PlayingStateEnum.Playing:

                            //MessagingCenter.Send(System.String.Empty, BaseViewModel.MSG_EnableFullScreen);
                            WeakReferenceMessenger.Default.Send(new ShowFullscreenMessage(true));

                            // VideoStackLayout must be visible before changing Layout
                            //VideoStackLayout.IsVisible = true;
                            //NoVideoStackLayout.IsVisible = false;

                            _viewModel.SetVideoStackLayoutvisible(true);
                            VideoStackLayout.IsVisible = true;
                            NoVideoStackLayout.IsVisible = false;

                            ChannelsListView.IsVisible = false;
                            MainToolBar.IsVisible = false;

                            if (IsPortrait)
                            {
                                if (_viewModel.EPGDetailVisible)
                                {
                                    AbsoluteLayout.SetLayoutBounds(EPGDetailGrid, EPGDetailGridPortraitPositionForPlay);
                                    //MainLayout.RaiseChild(EPGDetailGrid);

                                    AbsoluteLayout.SetLayoutBounds(VideoStackLayout, VideoStackLayoutPortraitPositionWhenEPGDetailVisibleForPlay);
                                    AbsoluteLayout.SetLayoutBounds(NoVideoStackLayout, VideoStackLayoutPortraitPositionWhenEPGDetailVisibleForPlay);

                                    LastVideoStackLayoutPosition = VideoStackLayoutPortraitPositionWhenEPGDetailVisibleForPlay;
                                }
                                else
                                {
                                    AbsoluteLayout.SetLayoutBounds(VideoStackLayout, FullScreenVideoPosition);
                                    AbsoluteLayout.SetLayoutBounds(NoVideoStackLayout, FullScreenVideoPosition);

                                    LastVideoStackLayoutPosition = FullScreenVideoPosition;
                                }
                            }
                            else
                            {
                                // landscape

                                if (_viewModel.EPGDetailVisible)
                                {
                                    AbsoluteLayout.SetLayoutBounds(EPGDetailGrid, EPGDetailGridLandscapePositionForPlay);
                                    //MainLayout.RaiseChild(EPGDetailGrid);

                                    AbsoluteLayout.SetLayoutBounds(VideoStackLayout, VideoStackLayoutLandscapePositionWhenEPGDetailVisibleForPlay);
                                    AbsoluteLayout.SetLayoutBounds(NoVideoStackLayout, VideoStackLayoutLandscapePositionWhenEPGDetailVisibleForPlay);

                                    LastVideoStackLayoutPosition = VideoStackLayoutLandscapePositionWhenEPGDetailVisibleForPlay;
                                }
                                else
                                {
                                    AbsoluteLayout.SetLayoutBounds(VideoStackLayout, FullScreenVideoPosition);
                                    AbsoluteLayout.SetLayoutBounds(NoVideoStackLayout, FullScreenVideoPosition);

                                    LastVideoStackLayoutPosition = FullScreenVideoPosition;
                                }
                            }

                            //MainLayout.RaiseChild(VideoStackLayout);
                            //CheckStreamCommand.Execute(null);
                            //NoVideoStackLayout.IsVisible = false;

                            break;
                        case PlayingStateEnum.PlayingInPreview:

                            //NavigationPage.SetHasNavigationBar(this, false);
                            //VideoStackLayout.IsVisible = true;
                            //NoVideoStackLayout.IsVisible = false;

                            _viewModel.SetVideoStackLayoutvisible(true);
                            VideoStackLayout.IsVisible = true;
                            NoVideoStackLayout.IsVisible = false;

                            ChannelsListView.IsVisible = true;
                            _viewModel.MainLayoutVisible = true;
                            MainToolBar.IsVisible = true;

                            if (!_configuration.Fullscreen)
                            {
                                WeakReferenceMessenger.Default.Send(new ShowFullscreenMessage(false));
                            }

                            if (IsPortrait)
                            {
                                if (_viewModel.EPGDetailVisible)
                                {
                                    AbsoluteLayout.SetLayoutBounds(VideoStackLayout, VideoStackLayoutPortraitPositionWhenEPGDetailVisibleForPreview);
                                    AbsoluteLayout.SetLayoutBounds(NoVideoStackLayout, VideoStackLayoutPortraitPositionWhenEPGDetailVisibleForPreview);
                                    AbsoluteLayout.SetLayoutBounds(EPGDetailGrid, EPGDetailGridPortraitPositionForPreview);
                                    AbsoluteLayout.SetLayoutBounds(ChannelsListView, ChannelsListViewPortraitPositionWhenEPGDetailVisibleForPreview);

                                    LastVideoStackLayoutPosition = VideoStackLayoutPortraitPositionWhenEPGDetailVisibleForPreview;
                                }
                                else
                                {
                                    AbsoluteLayout.SetLayoutBounds(VideoStackLayout, VideoStackLayoutPortraitPositionForPreview);
                                    AbsoluteLayout.SetLayoutBounds(NoVideoStackLayout, VideoStackLayoutPortraitPositionForPreview);
                                    AbsoluteLayout.SetLayoutBounds(ChannelsListView, ChannelsListPortraitPositionForPreview);

                                    LastVideoStackLayoutPosition = VideoStackLayoutPortraitPositionForPreview;
                                }
                            }
                            else
                            {
                                AbsoluteLayout.SetLayoutBounds(ChannelsListView, ChannelsListViewLandscapePositionWhenEPGDetailVisibleForPreview);

                                if (_viewModel.EPGDetailVisible)
                                {
                                    AbsoluteLayout.SetLayoutBounds(VideoStackLayout, VideoStackLayoutLandscapePositionWhenEPGDetailVisibleForPreview);
                                    AbsoluteLayout.SetLayoutBounds(NoVideoStackLayout, VideoStackLayoutLandscapePositionWhenEPGDetailVisibleForPreview);
                                    AbsoluteLayout.SetLayoutBounds(EPGDetailGrid, EPGDetailGridLandscapePositionForPreview);

                                    LastVideoStackLayoutPosition = VideoStackLayoutLandscapePositionWhenEPGDetailVisibleForPreview;
                                }
                                else
                                {
                                    AbsoluteLayout.SetLayoutBounds(VideoStackLayout, VideoStackLayoutLandscapePositionWhenEPGDetailNotVisibleForPreview);
                                    AbsoluteLayout.SetLayoutBounds(NoVideoStackLayout, VideoStackLayoutLandscapePositionWhenEPGDetailNotVisibleForPreview);

                                    LastVideoStackLayoutPosition = VideoStackLayoutLandscapePositionWhenEPGDetailNotVisibleForPreview;
                                }
                            }

                            //NoVideoStackLayout.IsVisible = false;
                            //CheckStreamCommand.Execute(null);

                            break;
                        case PlayingStateEnum.Stopped:

                            NavigationPage.SetHasNavigationBar(this, false);

                            //ChannelsListView.IsVisible = _viewModel.ChannelsListViewVisible;
                            ChannelsListView.IsVisible = true;
                            MainToolBar.IsVisible = true;

                            _viewModel.SetVideoStackLayoutvisible(null);
                            VideoStackLayout.IsVisible = false;
                            NoVideoStackLayout.IsVisible = false;

                            if (!_configuration.Fullscreen)
                            {
                                WeakReferenceMessenger.Default.Send(new ShowFullscreenMessage(false));
                            }

                            VideoStackLayout.IsVisible = false;
                            NoVideoStackLayout.IsVisible = false;

                            if (IsPortrait)
                            {
                                if (_viewModel.EPGDetailVisible)
                                {
                                    AbsoluteLayout.SetLayoutBounds(ChannelsListView, ChannelsListViewPortraitPositionWhenEPGDetailVisible);
                                    AbsoluteLayout.SetLayoutBounds(EPGDetailGrid, EPGDetailGridPortraitPosition);
                                }
                                else
                                {
                                    AbsoluteLayout.SetLayoutBounds(ChannelsListView, ChannelsListViewPortraitPositionWhenEPGDetailNOTVisible);
                                }
                            }
                            else // landscape
                            {
                                if (_viewModel.EPGDetailVisible)
                                {
                                    AbsoluteLayout.SetLayoutBounds(ChannelsListView, ChannelsListViewLandscapePositionWhenEPGDetailVisibleForPreview);
                                    AbsoluteLayout.SetLayoutBounds(EPGDetailGrid, EPGDetailGridLandscapePosition);
                                }
                                else
                                {
                                    AbsoluteLayout.SetLayoutBounds(ChannelsListView, ChannelsListViewPositionWhenEPGDetailNOTVisible);
                                }
                            }

                            AbsoluteLayout.SetLayoutBounds(VideoStackLayout, NoVideoStackLayoutPosition);
                            AbsoluteLayout.SetLayoutBounds(NoVideoStackLayout, NoVideoStackLayoutPosition);

                            LastVideoStackLayoutPosition = NoVideoStackLayoutPosition;

                            break;
                    }

                    if (_fixVideoNeeded && PlayingState != PlayingStateEnum.Stopped)
                    {
                        _fixVideoNeeded = false;
                        await FixVideo(true);
                    }

                    _loggingService.Info("RefreshGUI completed");

                }
                catch (Exception ex)
                {
                    _loggingService.Error(ex);
                }
                finally
                {
                    //_semaphoreSlimForRefreshGUI.Release();

                    UpdateVideoWindowPosition();
                }
            });
        }

        private void UpdateVideoWindowPosition()
        {
            switch (_viewModel.PlayingState)
            {
                case PlayingStateEnum.Playing:
                    WeakReferenceMessenger.Default.Send(
                    new ChangedVideoPositionMessage(
                        new Rect(0, 0, this.Width, this.Height)));
                    break;

                //case PlayingStateEnum.PlayingInPreview:
                default:
                    WeakReferenceMessenger.Default.Send(
                    new ChangedVideoPositionMessage(
                        new Rect((0.70) * Width, (0.78) * Height,
                                 (0.30) * Width, (0.22) * Height)));
                    break;
            }
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

        private async Task AutoPlay()
        {
            _loggingService.Debug("AutoPlay");

            if ((System.String.IsNullOrWhiteSpace(_configuration.AutoPlayedChannelUniqueID)) ||
                (_configuration.AutoPlayedChannelUniqueID == Channel.GetDefaultUniqueIdentifier("")))
            {
                return;
            }

            var lastId = Channel.GetDefaultUniqueIdentifier("last");
            if (_configuration.AutoPlayedChannelUniqueID == Channel.GetDefaultUniqueIdentifier("last"))
            {
                // last channel
                await ActionPlay();
            } else
            {
                var ch = _viewModel.GetChannelByUniqueidentifier(_configuration.AutoPlayedChannelUniqueID);
                if (ch == null)
                {
                    return;
                }
                _viewModel.SelectedChannel = ch;
                await ActionPlay(ch);
            }
        }

        protected override void OnAppearing()
        {
            _loggingService.Debug("OnAppearing");

            if (PlayingState != PlayingStateEnum.Stopped)
            {
                Task.Run(async ()=> await FixVideo(true));
            }
            else
            {
                SetFixVideNeeded();
            }

            base.OnAppearing();


            if (_firstAppearing)
            {
                if (_configuration.Fullscreen)
                {
                    WeakReferenceMessenger.Default.Send(new ShowFullscreenMessage(true));
                }

                _firstAppearing = false;

                InitializeVLC();

                //ConnectDriver();

                Task.Run(async () =>
                {
                    await _viewModel.RefreshChannels();

                    await AutoPlay();

                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        if (_viewModel.Channels.Count > 0)
                        {
                            _focusItems.FocusItem("ChannelsListView");
                        }
                        else
                        {
                            _focusItems.FocusItem("QuickTuneButton");
                        }
                    });
                });
            }
            _focusItems.DeFocusAll();

        }

        private void SendConnectDriverRequest()
        {
            if (_driver.Connected)
                return;

            switch (_configuration.DVBTDriverType)
            {
                case DriverTypeEnum.AndroidDVBTDriver:

                    _loggingService.Info("Sending connect message");
                    WeakReferenceMessenger.Default.Send(new DVBTDriverConnectAndroidMessage("Connect"));
                    break;

                case DriverTypeEnum.AndroidTestingDVBTDriver:

                    _testDVBTDriver = new TestDVBTDriver(_loggingService);
                    _testDVBTDriver.PublicDirectory = PublicDirectory;
                    _testDVBTDriver.Connect();

                    WeakReferenceMessenger.Default.Send(new DriverHasBeenConnectedMessage(
                        new DVBTDriverConfiguration()
                        {
                            DeviceName = "Testing DVBT driver",
                            ControlPort = _testDVBTDriver.ControlIPEndPoint.Port,
                            TransferPort = _testDVBTDriver.TransferIPEndPoint.Port
                        }));
                    break;

                case DriverTypeEnum.TestTuneDriver:

                    WeakReferenceMessenger.Default.Send(new DriverHasBeenConnectedMessage(
                        new DVBTDriverConfiguration()
                        {
                            DeviceName = "Test tune driver"
                        }));

                    break;

                case DriverTypeEnum.RTLSDRDriverFM:

                    var cfg = new RTLSDR.DriverSettings()
                    {
                        Port = _configuration.SDRDriverPort,
                        Streamport = _configuration.SDRDriverStreamPort,
                        SDRSampleRate = AudioTools.FMSampleRate
                    };

                    WeakReferenceMessenger.Default.Send(new RTLSDRDriverConnectMessage(cfg));

                    break;


                case DriverTypeEnum.RTLSDRDriverDAB:

                    var DABcfg = new RTLSDR.DriverSettings()
                    {
                        Port = _configuration.SDRDriverPort,
                        Streamport = _configuration.SDRDriverStreamPort,
                        SDRSampleRate = AudioTools.DABSampleRate
                    };

                    WeakReferenceMessenger.Default.Send(new RTLSDRDriverConnectMessage(DABcfg));

                    break;
            }

           // _viewModel.UpdateActiveDriverType();

            WeakReferenceMessenger.Default.Send(new DVBTDriverStateChangedMessages(null));
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
        }

        private void InitializeVLC()
        {
            try
            {
                _loggingService.Info("Initializing LibVLC");

                var options = new string[]
                {
                    "--avcodec-hw=any",
                    "--file-caching=1500",     // local files/streams
                    "--network-caching=2000",  // DVB/UDP/HTTP streams
                    //"--mediacodec",
                    //"--no-mediacodec-dr"
                };

                _LibVLC = new LibVLC(options);

                _mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_LibVLC);
                videoView.MediaPlayer = _mediaPlayer;

                /*  debug video
                var media = new Media(_LibVLC, new Uri("http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4"));
                _mediaPlayer.Media = media;
                _mediaPlayer.Play();
                */
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "Error while initializing VLC");
            }
        }

        private async void TuneButton_Clicked(object sender, EventArgs e)
        {
            if (_tuningPage != null &&
                _tuningPage.IsLoaded)
            {
                // preventing click when the settings page is just (or yet) loaded
                return;
            }

            await Navigation.PushAsync(_tuningPage);
        }

        private void DriverButton_Clicked(object sender, EventArgs e)
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

        private async Task ActionOK(bool longPress)
        {
            _loggingService.Debug($"ActionOK");

            switch (_focusItems.FocusedItemName)
            {
                case "ChannelsListView":

                    if (longPress)
                    {
                        _viewModel.MenuVisible = !_viewModel.MenuVisible;
                        return;
                    }

                    if (_viewModel.PlayingState == PlayingStateEnum.Playing)
                    {
                        VideoStackLayout_Tapped(this, new TappedEventArgs(null));
                        return;
                    }

                    await Task.Run(async () =>
                    {
                        await ActionPlay();
                    });
                    break;
                case "SettingsButton":
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        SettingsButton_Clicked(this, new EventArgs());
                    });
                    break;
                case "TuneButton":
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        TuneButton_Clicked(this, new EventArgs());
                    });
                    break;
                case "DVBTTelevizorButton":
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        DVBTTelevizorButton_Clicked(this, new EventArgs());
                    });
                    break;
                case "MenuButton":
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        MenuButton_Clicked(this, new EventArgs());
                    });
                    break;
                case "DriverStateButton":
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        DriverStateButton_Clicked(this, new EventArgs());
                    });
                    break;
                case "QuickTuneButton":
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        TuneButton_Clicked(this, new EventArgs());
                    });
                    break;
            }
        }

        public async Task ActionBack(bool longPress)
        {
            _loggingService.Debug($"ActionBack");

            switch (PlayingState)
            {
                case PlayingStateEnum.Playing:

                    if (_viewModel.EPGDetailEnabled)
                    {
                        _viewModel.EPGDetailEnabled = false;
                    }
                    else
                    {
                        await ActionStop(false);
                    }
                    break;

                case PlayingStateEnum.PlayingInPreview:
                    await ActionStop(false);
                    break;
                case PlayingStateEnum.Stopped:

                    if ((_lastBackPressedTime == DateTime.MinValue) || ((DateTime.Now - _lastBackPressedTime).TotalSeconds > 3))
                    {
                        if (longPress)
                        {
                            WeakReferenceMessenger.Default.Send(new QuitAppMessage(null));
                            return;
                        }

                        WeakReferenceMessenger.Default.Send(new ToastMessage($"Press once again for exit".Translated()));
                        _lastBackPressedTime = DateTime.Now;
                    }
                    else
                    {
                        WeakReferenceMessenger.Default.Send(new QuitAppMessage(null));
                    }

                    break;
            }
        }


        public async Task ActionRight()
        {
            _loggingService.Debug($"ActionRight");

            if (_viewModel.PlayingState != PlayingStateEnum.Playing)
            {
                if ((_focusItems.FocusedItemName == "ChannelsListView") &&
                    (_viewModel.SelectedChannel != null))
                {
                    _viewModel.SelectedChannel.Focused = false;
                    _viewModel.SelectedChannel.NotifyChanges();
                }
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    _focusItems.FocusNextItem(true);
                });
            } else
            {
                // right when playing:

                if (!_viewModel.EPGDetailEnabled)
                {
                    _viewModel.EPGDetailEnabled = true;
                } else
                {
                    if (_focusItems.FocusedItemName == "EPGDetailGrid")
                    {
                        _focusItems.DeFocusAll();
                    } else
                    {
                        _focusItems.FocusItem("EPGDetailGrid");
                    }
                }
            }
        }

        public async Task ActionLeft()
        {
            _loggingService.Debug($"ActionLeft");

            if (_viewModel.PlayingState != PlayingStateEnum.Playing)
            {
                if ((_focusItems.FocusedItemName == "ChannelsListView") && (_viewModel.SelectedChannel != null))
                {
                    _viewModel.SelectedChannel.Focused = false;
                    _viewModel.SelectedChannel.NotifyChanges();
                }

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    _focusItems.FocusPreviousItem(true);
                });
            } else
            {
                if (_lastPlayedChannels != null &&
                    _lastPlayedChannels[0] != _viewModel.SelectedChannel)
                {
                    _viewModel.SelectedChannel = _lastPlayedChannels[0];
                    await ActionPlay();
                }
            }
        }

        public async Task ActionDown()
        {
            _loggingService.Debug($"ActionDown");

            if (_viewModel.PlayingState != PlayingStateEnum.Playing)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    if ((new List<string>() { null, "DVBTTelevizorButton", "DriverStateButton", "TuneButton", "MenuButton" }).Contains(_focusItems.FocusedItemName) &&
                    _viewModel.ChannelsListViewVisible)
                    {
                        _focusItems.FocusItem("ChannelsListView");
                    }
                    else
                    if (_focusItems.FocusedItemName == "ChannelsListView")
                    {
                        _viewModel.SelectNextChannel();
                        await FocusSelectedChannel();
                    }
                    else
                    if (_focusItems.FocusedItemName == "EPGDetailGrid")
                    {
                        // scroll down
                        await SelectedChannelEPGDescriptionScrollView.ScrollToAsync(
                            SelectedChannelEPGDescriptionScrollView.ScrollX,
                            SelectedChannelEPGDescriptionScrollView.ScrollY + 50, false);
                    }
                    else
                    {
                        _focusItems.FocusNextItem(true);
                    }
                });
            }
            else
            {
                if (_viewModel.EPGDetailEnabled && _focusItems.FocusedItemName == "EPGDetailGrid")
                {
                    // scroll down
                    await SelectedChannelEPGDescriptionScrollView.ScrollToAsync(
                        SelectedChannelEPGDescriptionScrollView.ScrollX,
                        SelectedChannelEPGDescriptionScrollView.ScrollY + 50, false);
                }
                else
                {
                    _viewModel.SelectNextChannel();
                    await ActionPlay();
                }
            }
        }

        public async Task ActionUp()
        {
            _loggingService.Debug($"ActionUp");

            if (_viewModel.PlayingState != PlayingStateEnum.Playing)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    if ((new List<string>() { null, "DVBTTelevizorButton", "DriverStateButton", "TuneButton", "MenuButton" }).Contains(_focusItems.FocusedItemName) &&
                    _viewModel.ChannelsListViewVisible)
                    {
                        _focusItems.FocusItem("ChannelsListView");
                    }
                    else
                if (_focusItems.FocusedItemName == "ChannelsListView")
                    {
                        _viewModel.SelectPreiousChannel();
                        await FocusSelectedChannel();
                    }
                    else
                if (_focusItems.FocusedItemName == "EPGDetailGrid")
                    {
                        // scroll up
                        await SelectedChannelEPGDescriptionScrollView.ScrollToAsync(
                            SelectedChannelEPGDescriptionScrollView.ScrollX,
                            SelectedChannelEPGDescriptionScrollView.ScrollY - 50, false);
                    }
                    else
                    {
                        _focusItems.FocusPreviousItem(true);
                    }
                });
            } else
            {
                if (_viewModel.EPGDetailEnabled && _focusItems.FocusedItemName == "EPGDetailGrid")
                {
                    // scroll up
                    await SelectedChannelEPGDescriptionScrollView.ScrollToAsync(
                        SelectedChannelEPGDescriptionScrollView.ScrollX,
                        SelectedChannelEPGDescriptionScrollView.ScrollY - 50, false);
                }
                else
                {
                    _viewModel.SelectPreiousChannel();
                    await ActionPlay();
                }
            }
        }

        public async Task ActionStop(bool force)
        {
            _loggingService.Debug($"ActionStop (Force: {force}, PlayingState: {PlayingState})");

            if (PlayingState == PlayingStateEnum.Stopped)
                return;

            // do not check _media or videoView.MediaPlayer.IsPlaying: in case of no signal is MediaPlayer stopped
            if (videoView == null || videoView.MediaPlayer == null)
                return;

            if (!force && (PlayingState == PlayingStateEnum.Playing))
            {
                PlayingState = PlayingStateEnum.PlayingInPreview;
                //_viewModel.EPGDetailEnabled = true;
            }
            else
            {

                CallWithTimeout(delegate
                {
                    videoView.MediaPlayer.Stop();

                    if (_viewModel.RecordingChannel == null)
                    {
                        if (_viewModel.PlayingChannel != null &&
                        _viewModel.PlayingChannel.ChannelType != ChannelTypeEnum.SledovaniTV)
                        {
                            _driver.Stop();
                        }
                    }
                });

                PlayingState = PlayingStateEnum.Stopped;

                _lastActionPlayTime = DateTime.MinValue;

                _viewModel.PlayingChannelAspect = new Size(-1, -1);

                if (_viewModel.PlayingChannel != null)
                {
                    _viewModel.PlayingChannel.Subtitles.Clear();
                    _viewModel.PlayingChannel.AudioTracks.Clear();
                }

                _viewModel.PlayingChannel = null;
            }

            //_focusItems.DeFocusAll();

            var doNotAutomaticallyShowEPGDetail = _viewModel.DoNotAutomaticallyShowEPGDetail;
            _viewModel.DoNotAutomaticallyShowEPGDetail = true;
            _focusItems.FocusItem("ChannelsListView");
            await FocusSelectedChannel();
            _viewModel.DoNotAutomaticallyShowEPGDetail = doNotAutomaticallyShowEPGDetail;
       }

        public async Task ActionRecord(Channel channel = null)
        {
            _loggingService.Debug($"ActionRecord");

            try
            {
                if (channel == null)
                    channel = _viewModel.SelectedChannel;

                if (channel == null)
                    return;

                if (channel.ChannelType != ChannelTypeEnum.SledovaniTV && !_driver.Connected)
                {
                    WeakReferenceMessenger.Default.Send(new ToastMessage($"Record failed - device not connected".Translated()));
                    return;
                }

                _loggingService.Debug($"recording channel: {channel.Name} ({channel.Number})");

                if (PlayingState == PlayingStateEnum.Playing)
                {
                    if (_viewModel.PlayingChannel != channel)
                    {
                        // playing different channel
                        await ActionStop(true);
                        await ActionPlay(channel);
                    }
                }
                else
                {
                    await ActionPlay(channel);
                }

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    _viewModel.RecordingChannel = channel;
                });

                if (_viewModel.RecordingChannel.ChannelType != ChannelTypeEnum.SledovaniTV)
                {
                    await _driver.StartRecording(_configuration.OutputDirectory);
                } else
                {
                    _viewModel.SledovaniTVStartRecording();
                }

                var playStreamInfo = new PlayStreamInfo()
                {
                    Channel = channel,
                    CurrentEvent = await _viewModel.GetChannelEPG(channel)
                };

                WeakReferenceMessenger.Default.Send(new ToastMessage("Recording started".Translated()));
                //MessagingCenter.Send<PlayStreamInfo>(playStreamInfo, BaseViewModel.MSG_ShowRecordNotification);

                _viewModel.NotifyChannelChange();
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
        }

        public async Task ActionStopRecord()
        {
            _loggingService.Debug($"ActionStopRecord");

            if (_viewModel.RecordingChannel == null)
                return;

            try
            {
                string? fName = null;

                if (_viewModel.RecordingChannel.ChannelType != ChannelTypeEnum.SledovaniTV)
                {
                    fName = _driver.RecordFileName;

                    if (_driver.Recording)
                    {
                        _driver.StopRecording();
                    }

                    if (PlayingState == PlayingStateEnum.Stopped)
                    {
                        await _driver.Stop();
                    }
                }

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    _viewModel.RecordingChannel = null;
                });

                WeakReferenceMessenger.Default.Send(new ToastMessage("Recording stopped".Translated()));

                _viewModel.NotifyChannelChange();

                if (fName != null)
                {
                    await Share.RequestAsync(new ShareFileRequest
                    {
                        Title = "Share record".Translated(),
                        File = new ShareFile(fName)
                    });
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
        }

        private bool CanPlay(Channel? channel = null)
        {
            if (_viewModel.RecordingChannel != null && _viewModel.RecordingChannel != channel)
            {
                WeakReferenceMessenger.Default.Send(new ToastMessage("Playing {0} failed (recording in progress)".Translated(channel.Name)));
                return false;
            }

            if (channel.NonFree)
            {
                WeakReferenceMessenger.Default.Send(new ToastMessage("Playing {0} failed (non free channel)".Translated(channel.Name)));
                return false;
            }

            if (channel.ChannelType == ChannelTypeEnum.SledovaniTV)
            {
                // no driver check needed
                return true;
            }

            // DVBT/RTLSDDR driver check

            if (!_driver.DriverInstalled || !_driver.Connected)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    _appMenu.ShowRetryPlayMenu(_driver, channel.UniqueIdentifier);
                });
                return false;
            }
            /*
           if (!_viewModel.FMDriverActive &&
               (channel.ChannelType == ChannelTypeEnum.FM))
           {
               MainThread.BeginInvokeOnMainThread(async () =>
               {
                   _appMenu.ShowConfirmChangeDriverMenu(_driver, _configuration.DVBTDriverType, DriverTypeEnum.RTLSDRDriverFM);
               });
               return false;
           }

           if (!_viewModel.DABDriverActive &&
                (channel.ChannelType == ChannelTypeEnum.DAB))
           {
               MainThread.BeginInvokeOnMainThread(async () =>
               {
                   _appMenu.ShowConfirmChangeDriverMenu(_driver, _configuration.DVBTDriverType, DriverTypeEnum.RTLSDRDriverDAB);
               });
               return false;
           }

           if (
               !_viewModel.DVBTDriverActive &&
               ((channel.ChannelType == ChannelTypeEnum.DVBT) || (channel.ChannelType == ChannelTypeEnum.DVBT2))
               )
           {
               MainThread.BeginInvokeOnMainThread(async () =>
               {
                   _appMenu.ShowConfirmChangeDriverMenu(_driver, _configuration.DVBTDriverType, DriverTypeEnum.AndroidDVBTDriver);
               });
               return false;
           }
           */

            return true;
        }

        public async Task ActionPlay(Channel? channel = null)
        {
            _loggingService.Debug($"ActionPlay");

            try
            {
                if (channel == null)
                    channel = _viewModel.SelectedChannel;

                if (channel == null)
                    return;

                _loggingService.Debug($"playing: {channel.Name} ({channel.Number})");

                if (!CanPlay(channel))
                {
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

                /*
                if (
                    (_configuration.DVBTDriverType == DVBTDriverTypeEnum.RTLSDRFMDriver) ||
                    (_configuration.DVBTDriverType == DVBTDriverTypeEnum.RTLSDRTCPIPFMDriver)
                    )
                {
                    shouldMediaStop = false;
                    shouldMediaPlay = false;
                }
                */

                _viewModel.EPGDetailEnabled = false;

                //VideoStackLayout.IsVisible = false;
                //NoVideoStackLayout.IsVisible = true;

                PlayingState = PlayingStateEnum.Playing;

                _refreshGUIEnabled = false;
                _checkStreamEnabled = false;

                if (shouldMediaStop && videoView.MediaPlayer.IsPlaying)
                {
                    //await _driver.Stop(); // setting no PID

                    CallWithTimeout(delegate
                    {
                        _loggingService.Debug("Stopping Media player");
                        videoView.MediaPlayer.Stop();
                    });
                }

                if (channel.ChannelType == ChannelTypeEnum.SledovaniTV)
                {
                    // no driver tuning needed
                    shouldDriverPlay = false;
                }

                if (shouldDriverPlay)
                {
                    // tuning only when changing frequency, bandwidth or DVBTType

                    var tuneNeeded = true;

                    if (_viewModel.PlayingChannel != null &&
                        _viewModel.PlayingChannel.Frequency == channel.Frequency &&
                        _viewModel.PlayingChannel.Bandwdith == channel.Bandwdith &&
                        _viewModel.PlayingChannel.ChannelType == channel.ChannelType)
                    {
                        tuneNeeded = false;
                        //WeakReferenceMessenger.Default.Send(new LongToastMessage("Tuning ....".Translated()));
                    } else
                    {
                        WeakReferenceMessenger.Default.Send(new ToastMessage("Tuning {0} ....".Translated(channel.FrequencyShortLabel)));

                        var tunedRes = await _driver.TuneEnhanced(channel.Frequency, channel.Bandwdith, (int)channel.ChannelType, false);
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
                    if (channel.ChannelType == ChannelTypeEnum.SledovaniTV)
                    {
                        _media = new Media(_LibVLC, channel.Url, FromType.FromLocation);
                    }
                    else
                    {
                        switch (_driver.DVBTDriverStreamType)
                        {
                            case DVBTDriverStreamTypeEnum.UDP:
                                _media = new Media(_LibVLC, _driver.StreamUrl, FromType.FromLocation);
                                break;
                            case DVBTDriverStreamTypeEnum.Stream:
                                _media = new Media(_LibVLC, new StreamMediaInput(_driver.VideoStream), new string[] { });
                                break;
                            case DVBTDriverStreamTypeEnum.None:
                                break;
                        }
                    }

                    _media.AddOption(":fullscreen");
                    _media.AddOption(":avcodec-hw=any");

                    _media.AddOption(new MediaConfiguration()
                    {
                        EnableHardwareDecoding = true
                    });

                    CallWithTimeout(delegate
                    {
                        videoView.MediaPlayer.Play(_media);

                        /* Video is fixed in RefreshGUI
                        Task.Run(async () =>
                        {
                            // When user visits some page and return back, video is only black screen
                            // calls fix video will re-attach the video and set correct video position
                            await FixVideo(false);
                        });
                        */

                    }, 350);

                    if (!System.String.IsNullOrWhiteSpace(channel.SelectedAudioTrack))
                    {
                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(3500); // wait for CheckStream

                            int trackId;
                            if (int.TryParse(channel.SelectedAudioTrack, out trackId))
                            {
                                SetAudio($"setAudio:{trackId}");
                            }
                        });
                    }

                    if (!System.String.IsNullOrWhiteSpace(channel.SelectedSubtitle))
                    {
                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(3500); // wait for CheckStream

                            int spuId;
                            if (int.TryParse(channel.SelectedSubtitle, out spuId))
                            {
                                SetSubtitles($"setSubtitles:{spuId}");
                            }
                        });
                    }

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
                _viewModel.PlayingChannel.Subtitles.Clear();
                _viewModel.PlayingChannel.AudioTracks.Clear();
                _viewModel.PlayingChannelAspect = new Size(-1, -1);

                _lastActionPlayTime = DateTime.Now;

                _mediaPlayer.Teletext = 100;

                if (_lastPlayedChannels[1] != channel)
                {
                    _lastPlayedChannels[0] = _lastPlayedChannels[1];
                    _lastPlayedChannels[1] = channel;
                }

                _viewModel.NotifyChannelChange();

                await Task.Run(async () =>
                {
                    playInfo.CurrentEvent = await _viewModel.GetChannelEPG(channel);

                    if (playInfo.CurrentEvent == null || playInfo.CurrentEvent.CurrentEventItem == null)
                    {
                        await _viewModel.ScanEPG(channel, true, true, 2000, 3000);
                    }

                    await _viewModel.ShowActualPlayingMessage(playInfo);
                });

                //if (_config.PlayOnBackground)
                //{
                //    MessagingCenter.Send<MainPage, PlayStreamInfo>(this, BaseViewModel.MSG_PlayInBackgroundNotification, playInfo);
                //}

                PlayingState = PlayingStateEnum.Playing;
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
            finally
            {
                //_fixVideoNeeded = true;
                _checkStreamEnabled = true;
                _refreshGUIEnabled = true;

                //RefreshGUI();
            }
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

        /// <summary>
        /// Fix video position
        /// - workaround for black screen video/bad position/ when play/resume
        /// </summary>
        /// <param name="force">force re-attach videoview (will stop if playing)</param>
        /// <returns></returns>
        public async Task FixVideo(bool force)
        {
            _loggingService.Info($"FixVideo: {force}");

            if (_mediaPlayer == null)
                return;


            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    if (force)
                    {
                        if (_viewModel.PlayingState != PlayingStateEnum.Stopped)
                        {
                            if (_mediaPlayer.VideoTrack != -1)
                            {
                                videoView.MediaPlayer.Stop();

                                VideoStackLayout.Children.Remove(videoView);
                                VideoStackLayout.Children.Add(videoView);

                                videoView.MediaPlayer.Play();
                            }
                        }
                        else
                        {
                            VideoStackLayout.Children.Remove(videoView);
                            VideoStackLayout.Children.Add(videoView);
                        }
                    }

                    videoView.MediaPlayer = null;
                    videoView.MediaPlayer = _mediaPlayer;
                } catch (Exception ex)
                {
                    _loggingService.Error(ex);
                }
            });

            await Task.Delay(100);

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                AbsoluteLayout.SetLayoutFlags(VideoStackLayout, AbsoluteLayoutFlags.All);
                AbsoluteLayout.SetLayoutBounds(VideoStackLayout, NoVideoStackLayoutPosition);
            });

            if (LastVideoStackLayoutPosition == null)
            {
                LastVideoStackLayoutPosition = FullScreenVideoPosition;
            }

            for (int i=8;i>=0;i--)
            {
                await Task.Delay(250);

                _loggingService.Info(LastVideoStackLayoutPosition.ToString());


                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    AbsoluteLayout.SetLayoutBounds(VideoStackLayout, LastVideoStackLayoutPosition.Value);

                    /*
                    AbsoluteLayout.SetLayoutFlags(VideoStackLayout, AbsoluteLayoutFlags.All);
                    AbsoluteLayout.SetLayoutBounds(VideoStackLayout, new Rect(
                        LastVideoStackLayoutPosition.Value.Left,
                        LastVideoStackLayoutPosition.Value.Top,
                        LastVideoStackLayoutPosition.Value.Bottom - i / 10.0,
                        LastVideoStackLayoutPosition.Value.Right - i / 10.0));
                    */
                });
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

        private void ShowOrHideMenu()
        {
            if (MainMenu.MenuVisible)
            {
                HideMenu();
            }
            else
            {
                ShowMenu();
            }
        }

        private void ShowMenu()
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                MainMenu.MenuVisible = true;
                _viewModel.MenuVisible = true;
            });
        }

        private void HideMenu()
        {
            MainMenu.MenuVisible = false;
            _viewModel.MenuVisible = false;
        }

        private async void MenuButton_Clicked(object sender, EventArgs e)
        {
            if (!_menuShowEnabled)
            {
                return;
            }

            //bool animating = false;

            _menuShowEnabled = false;
            try
            {
                /*var angle = 0;

                // start rotation
                var task = Task.Run(async () =>
                {
                    animating = true;
                    while (animating)
                    {
                        angle += 36;
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            await MenuButton.RotateYTo(angle);
                        });
                    }
                });
                */
                ShowOrHideMenu();

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                   if (MainMenu.IsVisible)
                   {
                        BuildMenu();
                    }
                });

            } finally
            {
                /*
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    animating = false;
                    MenuButton.CancelAnimations();
                    await Task.Delay(500);
                    MenuButton.RotationY = 0; // reset to default angle
                });
                */
                _menuShowEnabled = true;
            }

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

        public Page GetPageFromStack(IReadOnlyList<Page> stack)
        {
            Page result = null;

            if (stack != null && stack.Count > 0)
            {
                if (stack[stack.Count - 1].GetType() != typeof(MainPage))
                {
                    // different page on navigation top

                    var pageOnTop = stack[stack.Count - 1];
                    if (pageOnTop is NavigationPage np)
                    {
                        result = np.CurrentPage;
                    } else
                    if (pageOnTop is Page p)
                    {
                        result = p;
                    }
                }
            }

            return result;
        }

        private void OnVideo_LongPress(object sender, EventArgs e)
        {
            _loggingService.Debug("OnVideo_LongPress");

            MenuButton_Clicked(this, new EventArgs());
        }

        public async void OnKeyDown(string key, bool longPress)
        {
            _loggingService.Debug($"Main Page OnKeyDown {key}");

            var pageOnTop = GetPageFromStack(Navigation.NavigationStack);
            if ((pageOnTop != null) && (pageOnTop is IOnKeyDown okd))
            {
                okd.OnKeyDown(key, longPress);
                return;
            }

            var keyAction = KeyboardDeterminer.GetKeyAction(key);

            if (MainMenu.MenuVisible)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    OnMenuKeyDown(keyAction);
                });
                return;
            }

            switch (keyAction)
            {
                case KeyboardNavigationActionEnum.Left:

                    await ActionLeft();

                    return;

                case KeyboardNavigationActionEnum.Right:

                    await ActionRight();

                    return;

                case KeyboardNavigationActionEnum.Down:

                    await ActionDown();

                    return;


                case KeyboardNavigationActionEnum.Up:

                    await ActionUp();

                    return;

                case KeyboardNavigationActionEnum.Back:
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await ActionBack(longPress);
                    });
                    return;

                case KeyboardNavigationActionEnum.OK:

                    await ActionOK(longPress);

                    return;
            }

            Task.Run(async () =>
            {
                switch (key.ToLower())
                {
                    //case "end":
                    //case "moveend":
                    //    await ActionFirstOrLast(false);
                    //    break;
                    //case "home":
                    //case "movehome":
                    //    await ActionFirstOrLast(true);
                    //    break;
                    //case "mediafastforward":
                    //case "mediaforward":
                    //case "pagedown":
                    //    await ActionKeyDown(10);
                    //    break;
                    //case "mediarewind":
                    //case "mediafastrewind":
                    //case "pageup":
                    //    await ActionKeyUp(10);
                    //    break;
                    case "mediaplaypause":
                    case "mediaplaystop":
                        if (PlayingState == PlayingStateEnum.Stopped)
                        {
                            await ActionPlay();
                        }
                        else
                        {
                            await ActionStop(true);
                        }
                        break;
                    case "mediastop":
                    case "mediaclose":
                        await ActionStop(true);
                        break;
                    case "f7":
                    case "mediapause":
                    case "forwarddel": // delete
                    case "delete":
                    case "altleft":
                    case "minus":
                    case "period":
                    case "apostrophe":
                    case "buttonselect":
                    case "break": // pause
                        await ActionStop(false);
                        break;
                    case "buttonl2":
                    case "info":
                    case "guide":
                    case "i":
                    case "g":
                    case "numpadadd":
                    case "buttonthumbl":
                    case "f1":
                    case "f8":
                    case "menu":
                    case "tab":
                    case "equals":
                    case "slash":
                    case "backslash":
                    case "insert":
                    case "tvcontentsmenu":
                        MenuButton_Clicked(this, new EventArgs());
                        break;
                    case "0":
                    case "num0":
                    case "number0":
                        HandleNumKey(0);
                        break;
                    case "1":
                    case "num1":
                    case "number1":
                        HandleNumKey(1);
                        break;
                    case "2":
                    case "num2":
                    case "number2":
                        HandleNumKey(2);
                        break;
                    case "3":
                    case "num3":
                    case "number3":
                        HandleNumKey(3);
                        break;
                    case "4":
                    case "num4":
                    case "number4":
                        HandleNumKey(4);
                        break;
                    case "5":
                    case "num5":
                    case "number5":
                        HandleNumKey(5);
                        break;
                    case "6":
                    case "num6":
                    case "number6":
                        HandleNumKey(6);
                        break;
                    case "7":
                    case "num7":
                    case "number7":
                        HandleNumKey(7);
                        break;
                    case "8":
                    case "num8":
                    case "number8":
                        HandleNumKey(8);
                        break;
                    case "9":
                    case "num9":
                    case "number9":
                        HandleNumKey(9);
                        break;
                    //case "f5":
                    //case "numpad0":
                    //case "green":
                    //case "proggreen":
                    //case "f10":
                    //    Reset();
                    //    Refresh();
                    //    break;
                    //case "record":
                    //case "mediarecord":
                    //case "red":
                    //case "progred":
                    //case "f9":
                    //case "r":
                    //    Device.BeginInvokeOnMainThread(async () => await _viewModel.RecordChannel(!_viewModel.IsRecording, true));
                    //    break;
                    //case "yellow":
                    //case "progyellow":
                    //case "f11":
                    //case "l":
                    //    _viewModel.ToggleFav();
                    //    break;
                    //case "blue":
                    //case "progblue":
                    //case "f12":
                    //case "k":
                    //case "leftshift":
                    //case "shiftleft":
                    //    ToggleAudioStream(null);
                    //    break;
                }
            });
        }

        private void HandleNumKey(int number)
        {
            _loggingService.Info($"HandleNumKey {number}");

            if ((DateTime.Now - _lastNumPressedTime).TotalSeconds > 2)
            {
                _lastNumPressedTime = DateTime.MinValue;
                _numberPressed = System.String.Empty;
            }

            _lastNumPressedTime = DateTime.Now;
            _numberPressed += number;


            WeakReferenceMessenger.Default.Send(new ToastMessage(_numberPressed));

            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                var numberPressedBefore = _numberPressed;

                Thread.Sleep(2000);

                if (numberPressedBefore == _numberPressed)
                {
                    Task.Run(async () =>
                    {
                        if (_numberPressed.StartsWith("0") && _numberPressed.Length>1)
                        {
                            // teletext number by 0XXX

                            if (_viewModel.PlayingState == PlayingStateEnum.Stopped)
                            {
                                return;
                            }

                            var teletextNumberAsString = _numberPressed.Substring(1);
                            int teletextNumber;
                            if (int.TryParse(teletextNumberAsString, out teletextNumber) && (teletextNumber>=100))
                            {
                                _mediaPlayer.Teletext = teletextNumber;
                                //_mediaPlayer.SetMarqueeInt(VideoMarqueeOption.Color, 0);
                                //var track = GetTeletextTrack();

                                WeakReferenceMessenger.Default.Send(new ToastMessage("Setting teletext page number: {0}".Translated(teletextNumberAsString)));
                            } else
                            {
                                WeakReferenceMessenger.Default.Send(new ToastMessage("Invalid teletext page number: {0}".Translated(teletextNumberAsString)));
                            }
                        }

                        if (_numberPressed == "0")
                        {
                            switch (_viewModel.PlayingState)
                            {
                                case PlayingStateEnum.Playing:
                                    await ActionLeft();
                                    break;
                                case PlayingStateEnum.PlayingInPreview:

                                    await MainThread.InvokeOnMainThreadAsync(async () =>
                                    {
                                        _viewModel.SelectedChannel = _viewModel.PlayingChannel;
                                    });
                                    await ActionPlay(_viewModel.PlayingChannel);

                                    break;

                                case PlayingStateEnum.Stopped:

                                    if (_focusItems.FocusedItemName == "ChannelsListView")
                                    {
                                        if (_viewModel.StandingOnEnd)
                                        {
                                            await _viewModel.SelectFirstChannel();
                                            _lastTimeHome = true;
                                        }
                                        else
                                        if (_viewModel.StandingOnStart)
                                        {
                                            await _viewModel.SelectLastChannel();
                                            _lastTimeHome = false;
                                        }
                                        else
                                        {
                                            if (_lastTimeHome)
                                            {
                                                await _viewModel.SelectLastChannel();
                                            }
                                            else
                                            {
                                                await _viewModel.SelectFirstChannel();
                                            }
                                            _lastTimeHome = !_lastTimeHome;
                                        }
                                    }
                                    break;
                            }
                            ;

                            return;
                        }

                        var selectedChannel = await _viewModel.SelectChannelByNumber(_numberPressed);
                        if ((selectedChannel != null) && (_numberPressed == selectedChannel.Number))
                        {
                            await ActionPlay(selectedChannel);
                            await FocusSelectedChannel();
                        }
                    });
                }

            }).Start();
        }


        public void OnTextSent(string text)
        {
            _loggingService.Debug($"Main Page OnTextSent: {text}");

            var pageOnTop = GetPageFromStack(Navigation.NavigationStack);
            if ((pageOnTop != null) && (pageOnTop is IOnKeyDown okd))
            {
                okd.OnTextSent(text);
                return;
            }
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

        private MenuItem? GetSelectedMenuItem()
        {
            var menuItems = _appMenu.IsVisible ? _appMenu.MenuItems : _activeMenuItems;

            if (menuItems == null)
                return null;

            foreach (var item in menuItems)
            {
                if (item.Selected)
                {
                    return item;
                }
            }

            return null;
        }

        private void OnMenuKeyDown(KeyboardNavigationActionEnum keyAction)
        {
            var menuItems = _appMenu.IsVisible ? _appMenu.MenuItems : _activeMenuItems;

            switch (keyAction)
            {
                case KeyboardNavigationActionEnum.Right:
                case KeyboardNavigationActionEnum.Down:
                    Task.Run(async () =>
                    {
                        await  MainMenu.SelectNextMenuItem(menuItems, false);
                    });
                    break;

                case KeyboardNavigationActionEnum.Left:
                case KeyboardNavigationActionEnum.Up:
                    Task.Run(async () =>
                    {
                        await MainMenu.SelectNextMenuItem(menuItems, true);
                    });
                    break;

                case KeyboardNavigationActionEnum.Back:
                    HideMenu();
                    break;

                case KeyboardNavigationActionEnum.OK:
                    var item = GetSelectedMenuItem();
                    if (item != null)
                    {
                        Menu_Tapped(item);
                    }
                    break;
            }
        }

        private async void EditChannel(Channel? channel)
        {
            if (_viewModel.SelectedChannel == null || _channelPage.IsLoaded)
            {
                return;
            }

            _channelPage.Channel = channel;
            _channelPage.Channels = _viewModel.Channels;

            await Navigation.PushAsync(_channelPage);
        }

        /*
        private void FitMenuSize()
        {
            var h = 300; // menulabel, margin, .....

            // FontSizeForLabel ~ 12, Margin 10+10
            var labelHeight = _viewModel.GetScaledSize(12) + 10 + 10;

            h += labelHeight * _menuItems.Count;
            h += 30; // delimiter

            var relativeHeight = h / MainAbsoluteLayout.Height;

            var width = IsPortrait ? 0.75 : 0.35;

            AbsoluteLayout.SetLayoutBounds(MenuFrame, new Rect(0.5, 0.5, width, relativeHeight));
            AbsoluteLayout.SetLayoutFlags(MenuFrame, AbsoluteLayoutFlags.All);
        }
        */

        private void Menu_Tapped(object sender, EventArgs e)
        {
            if (e != null &&
                e is TappedEventArgs tea &&
                tea.Parameter is MenuItem item)
            {
                Menu_Tapped(item);
            }
        }

        private async Task BuildTeletextMenu()
        {
            try
            {
                ShowMenu();

                _teletextMenuItems.Clear();

                var title = "Teletext menu".Translated();

                if (_currentTeletextNum == null)
                {
                    _teletextMenuItems.Add(MainMenu.CreateMenuItem("menuTeletextOn", "On".Translated(), "on.png"));
                    _teletextMenuItems.Add(MainMenu.CreateMenuItem("menuTeletextOff", "Off".Translated(), "off.png"));
                    _teletextMenuItems.Add(MainMenu.CreateMenuItem("menuTeletextNum", "Set Number ...".Translated(), "teletextnum.png")); ;


                    _teletextMenuItems.Add(MainMenu.CreateMenuItem("menuBack", "Back".Translated(), "back.png"));
                } else
                {
                    string dots = "";
                    switch (_currentTeletextNum.Length)
                    {
                        case 0:
                            title = "Teletext menu - set page number (first digit)".Translated();
                            dots = "..";
                            break;
                        case 1:
                            title = "Teletext menu - set page number  (second digit)".Translated();
                            dots = ".";
                            break;
                        case 2:
                            title = "Teletext menu - set page number".Translated();
                            break;
                    }

                    for (var i=0;i<=9;i++)
                    {
                        if ((_currentTeletextNum.Length==0) && (i==0))
                        {
                            continue; // no begin with 0
                        }

                        _teletextMenuItems.Add(MainMenu.CreateMenuItem("menuTeletextNum" + i.ToString(), _currentTeletextNum + i.ToString() + dots, "")); ;

                    }
                }

                _teletextMenuItems.Add(MainMenu.CreateMenuItem("menuClose", "Close".Translated(), "close.png"));

                _activeMenuItems = _teletextMenuItems;
                MainMenu.UpdateMenu((int)_configuration.AppFontSize, title, _teletextMenuItems);

            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
        }
        private async Task AudioMenu_Tapped()
        {
            try
            {
                if (_viewModel.SelectedChannel == null ||
                    _viewModel.SelectedChannel.AudioTracks == null)
                {
                    return;
                }

                ShowMenu();

                _audioMenuItems.Clear();

                var actualId = -1;
                if (videoView != null && videoView.MediaPlayer != null)
                {
                    actualId = videoView.MediaPlayer.AudioTrack;
                }

                int index = 0;
                foreach (var track in _viewModel.SelectedChannel.AudioTracks)
                {
                    index++;
                    var title = track.Value;
                    if (track.Key == actualId)
                    {
                        title += " *";
                    }
                    _audioMenuItems.Add(MainMenu.CreateMenuItem($"setAudio:{track.Key}", title, "audio.png", index > _viewModel.SelectedChannel.AudioTracks.Count - 1));
                }

                _audioMenuItems.Add(MainMenu.CreateMenuItem("menuBack", "Back".Translated(), "back.png"));
                _audioMenuItems.Add(MainMenu.CreateMenuItem("menuClose", "Close".Translated(), "close.png"));

                _activeMenuItems = _audioMenuItems;
                MainMenu.UpdateMenu((int)_configuration.AppFontSize, "Audio menu".Translated(), _audioMenuItems);

            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
        }

        private async Task SubtitlesMenu_Tapped()
        {
            try
            {
                if (_viewModel.SelectedChannel == null ||
                    _viewModel.SelectedChannel.AudioTracks == null)
                {
                    return;
                }

                ShowMenu();

                _subtitleMenuItems.Clear();

                var actualId = -1;
                if (videoView != null && videoView.MediaPlayer != null)
                {
                    actualId = videoView.MediaPlayer.Spu;
                }

                int index = 0;
                foreach (var sub in _viewModel.SelectedChannel.Subtitles)
                {
                    index++;
                    var title = sub.Value;
                    if (sub.Key == actualId)
                    {
                        title += " *";
                    }
                    _subtitleMenuItems.Add(MainMenu.CreateMenuItem($"setSubtitles:{sub.Key}", title, "subtitles.png", index > _viewModel.SelectedChannel.Subtitles.Count - 1));
                }

                _subtitleMenuItems.Add(MainMenu.CreateMenuItem("menuBack", "Back".Translated(), "back.png"));
                _subtitleMenuItems.Add(MainMenu.CreateMenuItem("menuClose", "Close".Translated(), "close.png"));

                _activeMenuItems = _subtitleMenuItems;
                MainMenu.UpdateMenu((int)_configuration.AppFontSize, "Subtitles menu".Translated(), _subtitleMenuItems);

            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
        }

        private async Task AspectMenu_Tapped()
        {
            try
            {
                ShowMenu();

                _aspectMenuItems.Clear();

                /*
                        actions.Add("16:9");
                        actions.Add("4:3");
                        actions.Add("Original");
                        actions.Add("Fill");
                */

                _aspectMenuItems.Add(MainMenu.CreateMenuItem("setAspect:16:9", "16:9", "aspect.png", false));
                _aspectMenuItems.Add(MainMenu.CreateMenuItem("setAspect:4:3", "4:3", "aspect.png", false));
                _aspectMenuItems.Add(MainMenu.CreateMenuItem("setAspect:Original", "Original".Translated(), "aspect.png", false));
                _aspectMenuItems.Add(MainMenu.CreateMenuItem("setAspect:Fill", "Fill".Translated(), "aspect.png", true));

                _aspectMenuItems.Add(MainMenu.CreateMenuItem("menuBack", "Back".Translated(), "back.png"));
                _aspectMenuItems.Add(MainMenu.CreateMenuItem("menuClose", "Close".Translated(), "close.png"));

                _activeMenuItems = _aspectMenuItems;
                MainMenu.UpdateMenu((int)_configuration.AppFontSize, "Aspect menu".Translated(), _aspectMenuItems);

            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
        }

        private void SetAspect(string command)
        {
            _loggingService.Info($"Setting Aspect: {command}");

            try
            {
                var valueAsString = command.Substring(10);

                if (videoView == null || videoView.MediaPlayer == null || PlayingState == PlayingStateEnum.Stopped || _viewModel.PlayingChannelAspect.Width == -1)
                {
                    return;
                }

                int width = Convert.ToInt32(_viewModel.PlayingChannelAspect.Width);
                int height = Convert.ToInt32(_viewModel.PlayingChannelAspect.Height);

                switch (valueAsString)
                {
                    case "16:9":
                        width = Convert.ToInt32(16.0 / 9.0 * _viewModel.PlayingChannelAspect.Height);
                        break;
                    case "4:3":
                        width = Convert.ToInt32(4.0 / 3.0 * _viewModel.PlayingChannelAspect.Height);
                        break;
                    case "Fill":
                        width = Convert.ToInt32(_lastAllocatedSize.Width);
                        height = Convert.ToInt32(_lastAllocatedSize.Height);
                        break;
                }

                CallWithTimeout(delegate
                {
                    videoView.MediaPlayer.AspectRatio = $"{width}:{height}";
                });

            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
        }

        private void SetSubtitles(string command)
        {
            _loggingService.Info($"Setting SetSubtitles: {command}");

            try
            {
                var idAsString = command.Substring(13);
                var id = Convert.ToInt32(idAsString);

                if (videoView == null || videoView.MediaPlayer == null)
                    return;

                foreach (var desc in videoView.MediaPlayer.SpuDescription)
                {
                    if (
                        (desc.Id == id) &&
                        (videoView.MediaPlayer.Spu != id)
                        )
                    {
                        _loggingService.Info($"Changing subtitles to: {desc.Id} ({desc.Name})");
                        videoView.MediaPlayer.SetSpu(desc.Id);
                        WeakReferenceMessenger.Default.Send(new ToastMessage("Changing subtitle: ".Translated() + desc.Name));

                        if (_viewModel.SelectedChannel != null)
                        {
                            _viewModel.SelectedChannel.SelectedSubtitle = desc.Id.ToString();
                            _viewModel.Config.SaveChannels(_viewModel.Channels);
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
        }

        private void SetAudio(string command)
        {
            _loggingService.Info($"Setting audio: {command}");

            try
            {
                var idAsString = command.Substring(9);
                var id = Convert.ToInt32(idAsString);

                if (videoView == null || videoView.MediaPlayer == null)
                    return;

                foreach (var desc in videoView.MediaPlayer.AudioTrackDescription)
                {
                    if (
                        (desc.Id == id) &&
                        (videoView.MediaPlayer.AudioTrack != id)
                        )
                    {
                        _loggingService.Info($"Changing audio track to: {desc.Id} ({desc.Name})");
                        videoView.MediaPlayer.SetAudioTrack(desc.Id);
                        WeakReferenceMessenger.Default.Send(new ToastMessage("Changing audio track: ".Translated() + desc.Name));

                        if (_viewModel.SelectedChannel != null)
                        {
                            _viewModel.SelectedChannel.SelectedAudioTrack = desc.Id.ToString();
                            _viewModel.Config.SaveChannels(_viewModel.Channels);
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
        }

        private async void Menu_Tapped(MenuItem menuItem)
        {
            var menuId = menuItem.Id;
            HideMenu();

            if (menuId.StartsWith("setAudio"))
            {
                SetAudio(menuId);
                return;
            }

            if (menuId.StartsWith("setAspect"))
            {
                SetAspect(menuId);
                return;
            }

            if (menuId.StartsWith("setSubtitles"))
            {
                SetSubtitles(menuId);
                return;
            }

            switch (menuId)
            {
                case "menuSettings":
                    SettingsButton_Clicked(this, null);
                    break;
                case "menuClose":
                    break;
                case "menuQuit":
                    WeakReferenceMessenger.Default.Send(new QuitAppMessage(null));
                    break;
                case "menuSubtitles":
                    await SubtitlesMenu_Tapped();
                    break;
                case "menuAudio":
                    await AudioMenu_Tapped();
                    break;
                case "menuAspect":
                    await AspectMenu_Tapped();
                    break;
                case "menuScanEPG":
                    await _viewModel.ScanEPG(_viewModel.SelectedChannel, false, false);
                    break;
                case "menuShowEPG":
                    _viewModel.DoNotAutomaticallyShowEPGDetail = false;
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        if (_viewModel.SelectedChannel == null ||
                        _viewModel.SelectedChannel.CurrentEventItem == null)
                        {
                            WeakReferenceMessenger.Default.Send(new ToastMessage("No program info found".Translated()));
                        } else
                        {
                            _viewModel.EPGDetailEnabled = true;
                        }
                    });
                    RefreshGUI();
                    break;
                case "menuHideEPG":
                    _viewModel.EPGDetailEnabled = false;
                    _viewModel.DoNotAutomaticallyShowEPGDetail = true;
                    RefreshGUI();
                    break;
                case "menuChannel":
                    EditChannel(_viewModel.SelectedChannel);
                    break;
                case "menuRefresh":
                    await _viewModel.RefreshChannels();
                    break;
                case "menuBack":
                    _activeMenuItems = _menuItems;
                    ShowMenu();
                    MainMenu.UpdateMenu((int)_configuration.AppFontSize, "Menu".Translated(), _menuItems);
                    break;
                case "menuPlay":
                    await ActionPlay();
                    break;
                case "menuStop":
                    await ActionStop(true);
                    break;
                case "menuRecord":
                    await ActionRecord();
                    break;
                case "menuStopRecord":
                    await ActionStopRecord();
                    break;
                case "menuTeletext":
                    await BuildTeletextMenu();
                    break;
                case "menuTeletextNum":
                    _currentTeletextNum = System.String.Empty;
                    await BuildTeletextMenu();
                    break;
                case "menuTeletextOn":
                    TurnOnTeletext();
                    break;
                case "menuTeletextOff":
                    if (_mediaPlayer != null)
                    {
                        _mediaPlayer.SetSpu(-1);
                    }
                    break;
                    case "menuFilter":
                        await Navigation.PushAsync(_filterPage);
                    break;
                case "menuCancelPlay":
                    await ActionStop(true);
                    break;

                case "menuDriver":
                    DriverStateButton_Clicked(this, null);
                    break;

                case "menuChangeDriver":
                    await ActionStop(true);
                    _vlcRawAudioInput = null;
                    await _viewModel.ChangeDriver(menuItem.DriverType);
                    break;

                case "menuConnectDriver":
                    WeakReferenceMessenger.Default.Send(new SendConnectDriverRequestMessage(System.String.Empty));
                    break;

                case "menuInstallDriver":
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        //if (_viewModel.DVBTDriverActive)
                        //{
                        //    await Browser.OpenAsync("https://play.google.com/store/apps/details?id=info.martinmarinov.dvbdriver", BrowserLaunchMode.External);
                        //}
                        //else
                        //{
                        //    await Browser.OpenAsync("https://play.google.com/store/apps/details?id=marto.rtl_tcp_andro", BrowserLaunchMode.External);
                        //}
                    });
                    break;

                case "menuRetryPlay":

                    await ActionPlay(menuItem.ChannelId == null ?
                            null :
                            _viewModel.GetChannelByUniqueidentifier(menuItem.ChannelId));
                    break;
            }

            for (var i=0;i<=9;i++)
            {
                if (menuId == "menuTeletextNum" + i.ToString())
                {
                    _currentTeletextNum += i.ToString();

                    if (_currentTeletextNum.Length == 3)
                    {
                        if (_mediaPlayer != null)
                        {
                            _mediaPlayer.Teletext = Convert.ToInt32(_currentTeletextNum);
                            _currentTeletextNum = null;
                        }
                    }
                    else
                    {
                        await BuildTeletextMenu();
                    }
                }
            }
        }

        private void TurnOnTeletext()
        {
            if (_mediaPlayer == null)
                return;

            foreach  (var track in _mediaPlayer.SpuDescription)
            {
                if (track.Name.ToLower().Contains("teletext"))
                {
                    _mediaPlayer.SetSpu(track.Id);
                    return;
                }
            }

            WeakReferenceMessenger.Default.Send(new ToastMessage("No teletext found".Translated()));
        }

        private void BuildMenu()
        {
            _menuItems.Clear();

            if ((_viewModel.Channels.Count > 0) && (_viewModel.SelectedChannel != null))
            {
                if ((_viewModel.PlayingState == PlayingStateEnum.Playing) ||
                    (_viewModel.PlayingState == PlayingStateEnum.PlayingInPreview))
                {
                    _menuItems.Add(MainMenu.CreateMenuItem("menuStop", "Stop".Translated(), "stop.png"));

                    if (_viewModel.SelectedChannel.AudioTracks.Count > 0)
                    {
                        _menuItems.Add(MainMenu.CreateMenuItem("menuAudio", "Audio".Translated(), "audio.png"));
                    }
                    if (_viewModel.SelectedChannel.Subtitles.Count > 0)
                    {
                        _menuItems.Add(MainMenu.CreateMenuItem("menuSubtitles", "Subtitles".Translated(), "subtitles.png"));
                    }

                    _menuItems.Add(MainMenu.CreateMenuItem("menuAspect", "Aspect ratio".Translated(), "aspect.png"));
                    _menuItems.Add(MainMenu.CreateMenuItem("menuTeletext", "Teletext".Translated(), "teletext.png"));
                }
                else
                {
                    _menuItems.Add(MainMenu.CreateMenuItem("menuPlay", "Play".Translated(), "play.png"));
                }
                _menuItems.Add(MainMenu.CreateMenuItem("menuChannel", "Channel detail".Translated()/* + "(" + _viewModel.SelectedChannel.Name + ")"*/, "edit.png"));

                if (_viewModel.EPGDetailVisible)
                {
                    _menuItems.Add(MainMenu.CreateMenuItem("menuHideEPG", "Hide program info".Translated(), "epg.png"));
                }
                else
                {
                    _menuItems.Add(MainMenu.CreateMenuItem("menuShowEPG", "Show program info".Translated(), "epg.png"));
                }

                _menuItems.Add(MainMenu.CreateMenuItem("menuScanEPG", "Scan EPG".Translated(), "epgscan.png"));

                if (_viewModel.RecordingChannel == null)
                {
                    _menuItems.Add(MainMenu.CreateMenuItem("menuRecord", "Record".Translated(), "recording.png"));
                }
                else
                {
                    _menuItems.Add(MainMenu.CreateMenuItem("menuStopRecord", "Stop record".Translated(), "stoprecord.png"));
                }
            }

            if (
                    (_viewModel.PlayingState != PlayingStateEnum.Playing) &&
                    (_viewModel.Channels.Count > 0)
                )
            {
                _menuItems.Add(MainMenu.CreateMenuItem("menuFilter", "Filter".Translated(), "filter.png"));
            }

            _menuItems.Add(MainMenu.CreateMenuItem("menuSettings", "Settings".Translated(), "settings.png"));
            _menuItems.Add(MainMenu.CreateMenuItem("menuRefresh", "Refresh channels".Translated(), "refresh.png"));
            _menuItems.Add(MainMenu.CreateMenuItem("menuQuit", "Quit application".Translated(), "quit.png", true));

            _menuItems.Add(MainMenu.CreateMenuItem("menuClose", "Close".Translated(), "close.png"));

            // _menuItems.First().Selected = true;
            _activeMenuItems = _menuItems;
            MainMenu.UpdateMenu((int)_configuration.AppFontSize,"Menu".Translated(), _menuItems);
            //FitMenuSize();
        }

        private void EPGDetailGrid_SwipeRight(object sender, SwipedEventArgs e)
        {
            _viewModel.EPGDetailEnabled = false;
            RefreshGUI();
        }

        private void VideoStackLayout_DoubleTapped(object sender, TappedEventArgs e)
        {
            if (_viewModel.PlayingState == PlayingStateEnum.Playing)
            {
                _viewModel.PlayingState = PlayingStateEnum.PlayingInPreview;
            }
            else
            if (_viewModel.PlayingState == PlayingStateEnum.PlayingInPreview)
            {
                _viewModel.PlayingState = PlayingStateEnum.Playing;
            }

            RefreshGUI();
        }

        private void VideoStackLayout_Tapped(object sender, TappedEventArgs e)
        {
            /*
            if (_viewModel.PlayingState == PlayingStateEnum.Playing ||
                _viewModel.PlayingState == PlayingStateEnum.PlayingInPreview)
            {
                _viewModel.EPGDetailEnabled = !_viewModel.EPGDetailEnabled;
                RefreshGUI();
            }
            */

            if (_viewModel.PlayingState == PlayingStateEnum.Playing)
            {
                MenuButton_Clicked(this, new EventArgs());
            }
        }

        private void DriverImageTapped(object sender, TappedEventArgs e)
        {
            WeakReferenceMessenger.Default.Send(new InstallDriverMessage(System.String.Empty));
        }

        private void TuneImageTapped(object sender, TappedEventArgs e)
        {
            TuneButton_Clicked(this, new EventArgs());
        }

        private void QuickDriverButton_Clicked(object sender, EventArgs e)
        {
            WeakReferenceMessenger.Default.Send(new InstallDriverMessage(System.String.Empty));
        }

        private void OnChannel_Tapped(object sender, TappedEventArgs e)
        {
            _loggingService.Info("OnChannel_Tapped");

            // this event is fired immedietly after LongTappedEvent!
            if ((DateTime.Now - _lastLongTappedTime).TotalMilliseconds<1000)
            {
                return;
            }

            if (sender is Grid grid && grid.BindingContext is Channel channel)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    _viewModel.SelectedChannel = channel;
                    await ActionPlay(channel);
                });
            }
        }

        private void OnChannel_LongTapped(object sender, EventArgs e)
        {
            _loggingService.Info("OnChannel_LongTapped");

            if (sender is Grid grid && grid.BindingContext is Channel channel)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    _viewModel.SelectedChannel = channel;
                });

                _lastLongTappedTime = DateTime.Now;
            }
        }

        private async void VideoSwiped_Up(object sender, SwipedEventArgs e)
        {
            await ActionUp();
        }

        private async void VideoSwiped_Down(object sender, SwipedEventArgs e)
        {
            await ActionDown();
        }

        private void OnChannel_LongTapped(object sender, CommunityToolkit.Maui.Core.LongPressCompletedEventArgs e)
        {

        }

        private void OnVideo_LongPress(object sender, CommunityToolkit.Maui.Core.LongPressCompletedEventArgs e)
        {

        }
    }

}
