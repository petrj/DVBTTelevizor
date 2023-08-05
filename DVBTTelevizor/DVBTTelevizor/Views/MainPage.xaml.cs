using LibVLCSharp.Shared;
using LoggerService;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;
using static Android.Resource;
using static DVBTTelevizor.MainPageViewModel;


namespace DVBTTelevizor
{
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage, IOnKeyDown
    {
        private MainPageViewModel _viewModel;

        private static SemaphoreSlim _semaphoreSlimForRefreshGUI = new SemaphoreSlim(1, 1);
        private bool _refreshGUIEnabled = true;
        private bool _checkStreamEnabled = true;

        private IDVBTDriverManager _driver;
        private DialogService _dlgService;
        private ILoggingService _loggingService;
        private DVBTTelevizorConfiguration _config;
        private ChannelPage _editChannelPage;
        private TuneOptionsPage _tuneOptionsPage;
        private SettingsPage _settingsPage;
        private ChannelService _channelService;
        private KeyboardFocusableItem _tuneFocusItem = null;
        private RemoteAccessService.RemoteAccessService _remoteAccessService;
        private DateTime _lastToggledAudioStreamTime = DateTime.MinValue;
        private DateTime _lastToggledSubtitlesTime = DateTime.MinValue;

        private DateTime _lastNumPressedTime = DateTime.MinValue;
        private string _numberPressed = System.String.Empty;
        private bool _firstStartup = true;
        private Size _lastAllocatedSize = new Size(-1, -1);
        private DateTime _lastBackPressedTime = DateTime.MinValue;
        private bool _lastTimeHome = false;

        public bool IsPortrait { get; private set; } = false;

        private LibVLC _libVLC = null;
        private MediaPlayer _mediaPlayer;
        private Media _media = null;

        private bool _firstAppearing = true;
        private DVBTChannel[] _lastPlayedChannels = new DVBTChannel[2];
        private List<string> _remoteDevicesConnected = new List<string>();

        public Command CheckStreamCommand { get; set; }

        // EPGDetailGrid
        private Rectangle LandscapeEPGDetailGridPosition { get; set; } = new Rectangle(1.0, 1.0, 0.3, 1.0);
        private Rectangle LandscapePreviewEPGDetailGridPosition { get; set; } = new Rectangle(1.0, 1.0, 0.3, 0.7);
        private Rectangle LandscapePlayingEPGDetailGridPosition { get; set; } = new Rectangle(1.0, 1.0, 0.3, 1.0);
        private Rectangle PortraitPlayingEPGDetailGridPosition { get; set; } = new Rectangle(1.0, 1.0, 1.0, 0.3);
        private Rectangle PortraitEPGDetailGridPosition { get; set; } = new Rectangle(1.0, 1.0, 1.0, 0.3);
        private Rectangle PortraitPreviewEPGDetailGridPosition { get; set; } = new Rectangle(1.0, 1.0, 1.0, 0.3);

        // VideoStackLayout
        private Rectangle LandscapePreviewVideoStackLayoutPosition { get; set; } = new Rectangle(1.0, 0.0, 0.3, 0.3);
        private Rectangle LandscapeVideoStackLayoutPositionWhenEPGDetailVisible { get; set; } = new Rectangle(0.0, 0.0, 0.7, 1.0);
        private Rectangle PortraitVideoStackLayoutPositionWhenEPGDetailVisible { get; set; } = new Rectangle(0.0, 0.0, 1.0, 0.7);
        private Rectangle PortraitPreviewVideoStackLayoutPosition { get; set; } = new Rectangle(1.0, 0.0, 0.5, 0.3);

        // VideoStackLayout must be visible when initializing VLC window!
        private Rectangle NoVideoStackLayoutPosition { get; set; } = new Rectangle(0, 0, 0.001, 0.001);

        // RecordingLabel
        private Rectangle LandscapeRecordingLabelPosition { get; set; } = new Rectangle(1.0, 1.0, 0.1, 0.1);
        private Rectangle LandscapePreviewRecordingLabelPosition { get; set; } = new Rectangle(1.0, 0.25, 0.1, 0.1);
        private Rectangle LandscapeRecordingLabelPositionWhenEPGDetailVisible { get; set; } = new Rectangle(0.65, 1.0, 0.1, 0.1);
        private Rectangle PotraitRecordingLabelPosition { get; set; } = new Rectangle(1.0, 1.0, 0.1, 0.1);
        private Rectangle PortraitRecordingLabelPositionWhenEPGDetailVisible { get; set; } = new Rectangle(1.0, 0.65, 0.1, 0.1);
        private Rectangle PortraitPreviewRecordingLabelPosition { get; set; } = new Rectangle(1.0, 0.25, 0.1, 0.1);

        // ChannelsListView
        private Rectangle LandscapeChannelsListViewPositionWhenEPGDetailVisible { get; set; } = new Rectangle(0.0, 1.0, 0.7, 1.0);
        private Rectangle ChannelsListViewPositionWhenEPGDetailNOTVisible { get; set; } = new Rectangle(0, 0, 1, 1);
        private Rectangle PortraitChannelsListViewPositionWhenEPGDetailVisible { get; set; } = new Rectangle(0.0, 0.0, 1.0, 0.7);

        public MainPage(ILoggingService loggingService, DVBTTelevizorConfiguration config, IDVBTDriverManager driverManager)
        {
            InitializeComponent();

            _dlgService = new DialogService(this);

            _loggingService = loggingService;

            _config = config;

            _driver = driverManager;

            _channelService = new ConfigChannelService(_loggingService, _config);

            _tuneOptionsPage = new TuneOptionsPage(_loggingService, _dlgService, _driver, _config, _channelService);
            _settingsPage = new SettingsPage(_loggingService, _dlgService, _config, _channelService);
            _editChannelPage = new ChannelPage(_loggingService, _dlgService, _driver, _config, _channelService);

            Core.Initialize();

            _libVLC = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVLC) { EnableHardwareDecoding = true };
            videoView.MediaPlayer = _mediaPlayer;

            BindingContext = _viewModel = new MainPageViewModel(_loggingService, _dlgService, _driver, _config, _channelService);

            if (_config.AutoInitAfterStart)
            {
                Task.Run(() =>
                {
                    Xamarin.Forms.Device.BeginInvokeOnMainThread(
                    new Action(
                    delegate
                    {
                        MessagingCenter.Send("", BaseViewModel.MSG_Init);
                    }));
                });
            }

            CheckStreamCommand = new Command(async () => await CheckStream());

            BackgroundCommandWorker.RunInBackground(CheckStreamCommand, 3, 5);

            _tuneOptionsPage.Disappearing += anyPage_Disappearing;
            _settingsPage.Disappearing += anyPage_Disappearing;
            _editChannelPage.Disappearing += _editChannelPage_Disappearing;

            ChannelsListView.ItemSelected += ChannelsListView_ItemSelected;

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_KeyDown, (key) =>
            {
                var longPress = false;
                if (key.StartsWith(BaseViewModel.LongPressPrefix))
                {
                    longPress = true;
                    key = key.Substring(BaseViewModel.LongPressPrefix.Length);
                }

                OnKeyDown(key, longPress);
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_EditChannel, (message) =>
            {
                Xamarin.Forms.Device.BeginInvokeOnMainThread(
                    delegate
                    {
                        EditSelectedChannel();
                    });
            });

            MessagingCenter.Subscribe<PlayStreamInfo>(this, BaseViewModel.MSG_PlayStream, (playStreamInfo) =>
            {
                Task.Run(async () =>
                {
                    await ActionPlay(false, playStreamInfo.Channel);
                });
            });

            MessagingCenter.Subscribe<PlayStreamInfo>(this, BaseViewModel.MSG_PlayAndRecordStream, (playStreamInfo) =>
            {
                Task.Run(async () =>
                {
                    await ActionPlay(true, playStreamInfo.Channel);
                });
            });

            MessagingCenter.Subscribe<PlayStreamInfo>(this, BaseViewModel.MSG_RecordStreamToFile, (playStreamInfo) =>
            {
                Task.Run(async () =>
                {
                    await _driver.Tune(playStreamInfo.Channel.Frequency, playStreamInfo.Channel.Bandwdith, playStreamInfo.Channel.DVBTType);
                    await _driver.SetPIDs(playStreamInfo.Channel.PIDsArary);
                    await _driver.StartRecording();
                    _viewModel.RecordingChannel = playStreamInfo.Channel;
                    _viewModel.RecordingChannel.Recording = true;
                    _viewModel.NotifyMediaChange();
                });
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_DVBTDriverConfiguration, (message) =>
            {
                _loggingService.Debug($"Received DVBTDriverConfiguration message: {message}");

                if (!_driver.Started)
                {
                    _viewModel.ConnectDriver(message);
                }
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_UpdateDriverState, (message) =>
            {
                _viewModel.UpdateDriverState();
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_DVBTDriverConfigurationFailed, (message) =>
            {
                Device.BeginInvokeOnMainThread(delegate
                {
                    _viewModel.UpdateDriverState();

                    MessagingCenter.Send($"Device connection error ({message})", BaseViewModel.MSG_ToastMessage);
                });
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_PlayPreviousChannel, (msg) =>
            {
                ActionUp();
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_PlayNextChannel, (msg) =>
            {
                ActionDown();
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_StopStream, (msg) =>
            {
                Task.Run(async () => { await ActionStop(true); });
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_StopRecord, (msg) =>
            {
                Task.Run(async () => { await _viewModel.StopRecord(); });
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_ImportChannelsList, (message) =>
            {
                _viewModel.ImportCommand.Execute(message);
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_EPGDetailVisibilityChange, (message) =>
            {
                RefreshGUI();
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_ChangeSubtitleId, (spuId) =>
            {
                int id;
                if (!int.TryParse(spuId, out id) )
                {
                    return;
                }

                SetSubtitles(id);
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_ChangeAudioTrackId, (trackId) =>
            {
                int id;
                if (!int.TryParse(trackId, out id))
                {
                    return;
                }

                SetAudioTrack(id);
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_ChangeAspect, (aspect) =>
            {
                SetAspect(aspect);
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_TeletextPageNumber, (pageNum) =>
            {
                CallWithTimeout(delegate
                {
                    videoView.MediaPlayer.Teletext = Convert.ToInt32(pageNum);
                });
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_ClearEPG, (msg) =>
            {
                _viewModel.EIT.ClearAll();
            });

            _tuneFocusItem = KeyboardFocusableItem.CreateFrom("TuneButton", new List<View> { TuneButton });
            _tuneFocusItem.Focus();

            Task.Run(async () => { await _settingsPage.AcknowledgePurchases(); });

            _remoteAccessService = new RemoteAccessService.RemoteAccessService(_loggingService);
            RestartRemoteAccessService();
        }

        private void SetSubtitles(int id)
        {
            if ((!_viewModel.PlayingChannelSubtitles.ContainsKey(id)) || (PlayingState == PlayingStateEnum.Stopped))
            {
                return;
            }

            _viewModel.Subtitles = id;

            CallWithTimeout( delegate
            {
                videoView.MediaPlayer.SetSpu(id);
            });
        }

        /// <summary>
        /// -100 .. auto
        /// -1 .. no audio
        ///  0 .. n  .. track id
        /// </summary>
        /// <param name="id"></param>
        private void SetAudioTrack(int id)
        {
            if (id == -100)
            {
                _viewModel.AudioTrack = -100;
                return;
            }

            if ((!_viewModel.PlayingChannelAudioTracks.ContainsKey(id)) || (PlayingState == PlayingStateEnum.Stopped))
            {
                return;
            }

            _viewModel.AudioTrack = id;

            CallWithTimeout(delegate
            {
                if (_viewModel.RecordingChannel != null)
                {
                    videoView.MediaPlayer.SetAudioTrack(-1);
                }

                videoView.MediaPlayer.SetAudioTrack(id);
            });
        }

        private void RestartRemoteAccessService()
        {
            if (_config.AllowRemoteAccessService)
            {
                if (_remoteAccessService.IsBusy)
                {
                    if (_remoteAccessService.ParamsChanged(_config.RemoteAccessServiceIP, _config.RemoteAccessServicePort, _config.RemoteAccessServiceSecurityKey))
                    {
                        _remoteAccessService.StopListening();
                        _remoteAccessService.SetConnection(_config.RemoteAccessServiceIP, _config.RemoteAccessServicePort, _config.RemoteAccessServiceSecurityKey);
                        _remoteAccessService.StartListening(OnMessageReceived, BaseViewModel.DeviceFriendlyName);
                    }
                }
                else
                {
                    _remoteAccessService.SetConnection(_config.RemoteAccessServiceIP, _config.RemoteAccessServicePort, _config.RemoteAccessServiceSecurityKey);
                    _remoteAccessService.StartListening(OnMessageReceived, BaseViewModel.DeviceFriendlyName);
                }
            }
            else
            {
                _remoteAccessService.StopListening();
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

        public void SetAspect(string aspect)
        {
            if (PlayingState == PlayingStateEnum.Stopped || _viewModel.PlayingChannelAspect.Width == -1)
            {
                return;
            }

            int width = Convert.ToInt32(_viewModel.PlayingChannelAspect.Width);
            int height = Convert.ToInt32(_viewModel.PlayingChannelAspect.Height);

            switch (aspect)
            {
                case "16:9":
                    width = Convert.ToInt32(16.0 /9.0* _viewModel.PlayingChannelAspect.Height);
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

        public void Resume()
        {
            _loggingService.Info("ResumePlayback");

            bool playing = false;
            if ((PlayingState == PlayingStateEnum.Playing) || (PlayingState == PlayingStateEnum.PlayingInPreview))
            {
                playing = true;
            }

            // workaround for black screen after resume (only audio is playing)
            // TODO: resume video without reinitializing

            Device.BeginInvokeOnMainThread(() =>
            {
                if (playing)
                {
                    if (_mediaPlayer.VideoTrack != -1)
                    {
                        videoView.MediaPlayer.Stop();

                        VideoStackLayout.Children.Remove(videoView);
                        VideoStackLayout.Children.Add(videoView);

                        videoView.MediaPlayer.Play();
                    }
                } else
                {
                    VideoStackLayout.Children.Remove(videoView);
                    VideoStackLayout.Children.Add(videoView);
                }
            });

        }

        private void OnMessageReceived(RemoteAccessService.RemoteAccessMessage message)
        {
            if (message == null)
                return;

            var senderFriendlyName = message.GetSenderFriendlyName();
            if (!_remoteDevicesConnected.Contains(senderFriendlyName))
            {
                _remoteDevicesConnected.Add(senderFriendlyName);
                var msg = "Remote device connected";
                if (!string.IsNullOrEmpty(senderFriendlyName))
                {
                    msg += $" ({senderFriendlyName})";
                }

                MessagingCenter.Send(msg, BaseViewModel.MSG_ToastMessage);
            }

            if (message.command == "keyDown")
            {
                MessagingCenter.Send(message.commandArg1, BaseViewModel.MSG_RemoteKeyAction);
            }
            if (message.command == "sendText")
            {
                OnTextSent(message.commandArg1);
            }
        }

        public void OnTextSent(string text)
        {
            Device.BeginInvokeOnMainThread(delegate
            {
                var stack = Navigation.NavigationStack;
                if (stack[stack.Count - 1].GetType() != typeof(MainPage))
                {
                    // different page on navigation top

                    var pageOnTop = stack[stack.Count - 1];

                    if (pageOnTop is IOnKeyDown)
                    {
                        (pageOnTop as IOnKeyDown).OnTextSent(text);
                    }

                    return;
                }
            });
        }

        private void _editChannelPage_Disappearing(object sender, EventArgs e)
        {
            Task.Run(async () =>
            {
                await _channelService.SaveChannels(_viewModel.Channels);

                Device.BeginInvokeOnMainThread(
                    delegate
                    {
                        _viewModel.RefreshCommand.Execute(null);
                    });
            });
        }

        private void anyPage_Disappearing(object sender, EventArgs e)
        {
            _viewModel.NotifyFontSizeChange();
            _viewModel.RefreshCommand.Execute(null);

            if (sender is SettingsPage)
            {
                RestartRemoteAccessService();
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            _viewModel.SelectedToolbarItemName = null;
            _viewModel.SelectedPart = SelectedPartEnum.ChannelsListOrVideo;
            if (_viewModel.TunningButtonVisible)
            {
                _tuneFocusItem.Focus();
            }

            _viewModel.EPGDetailEnabled = false;

            if (_firstAppearing)
            {
                _firstAppearing = false;

                // workaround for not selecting channel after startup:
                _viewModel.SelectchannelAfterStartup(3);
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
        }

        public void Done()
        {
            Task.Run(async () =>
            {
                await _viewModel.DisconnectDriver();

                MessagingCenter.Unsubscribe<string>(this, BaseViewModel.MSG_KeyDown);
                MessagingCenter.Unsubscribe<string>(this, BaseViewModel.MSG_EditChannel);
                MessagingCenter.Unsubscribe<PlayStreamInfo>(this, BaseViewModel.MSG_PlayStream);
                MessagingCenter.Unsubscribe<string>(this, BaseViewModel.MSG_DVBTDriverConfiguration);
                MessagingCenter.Unsubscribe<string>(this, BaseViewModel.MSG_UpdateDriverState);
                MessagingCenter.Unsubscribe<string>(this, BaseViewModel.MSG_DVBTDriverConfigurationFailed);
            });
        }

        private void OnCloseEPGDetailTapped(object sender, EventArgs e)
        {
            _viewModel.EPGDetailEnabled = false;
        }

        private void OnCloseVideoTapped(object sender, EventArgs e)
        {
            Task.Run(async () =>
            {
                await ActionStop(true);
            });
        }

        private void OnMaximizeVideoTapped(object sender, EventArgs e)
        {
            PlayingState = PlayingStateEnum.Playing;
        }

        private void OnMinimizeVideoTapped(object sender, EventArgs e)
        {
            Task.Run(async () =>
            {
                await ActionStop(false);
            });
        }

        public async void OnKeyDown(string key, bool longPress)
        {
            _loggingService.Debug($"OnKeyDown {key}");

            var keyAction = KeyboardDeterminer.GetKeyAction(key);

            var stack = Navigation.NavigationStack;
            if (stack[stack.Count - 1].GetType() != typeof(MainPage))
            {
                // different page on navigation top

                var pageOnTop = stack[stack.Count - 1];

                if (pageOnTop is IOnKeyDown)
                {
                    (pageOnTop as IOnKeyDown).OnKeyDown(key, longPress);
                }

                return;
            }

            switch (keyAction)
            {
                case KeyboardNavigationActionEnum.Down:
                    await ActionDown();
                    return;

                case KeyboardNavigationActionEnum.Up:
                    await ActionUp();
                    return;

                case KeyboardNavigationActionEnum.Right:
                    await ActionRight();
                    return;

                case KeyboardNavigationActionEnum.Left:
                    await ActionLeft();
                    return;

                case KeyboardNavigationActionEnum.Back:
                    await ActionBack(longPress);
                    return;

                case KeyboardNavigationActionEnum.OK:
                    await ActionOK(longPress);
                    return;
            }

            switch (key.ToLower())
            {
                case "end":
                case "moveend":
                    await ActionFirstOrLast(false);
                    break;
                case "home":
                case "movehome":
                    await ActionFirstOrLast(true);
                    break;

                case "mediafastforward":
                case "mediaforward":
                case "pagedown":
                    await ActionDown(10);
                    break;
                case "mediarewind":
                case "mediafastrewind":
                case "pageup":
                    await ActionUp(10);
                    break;

                case "0":
                case "num0":
                case "number0":
                case "numpad0":
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
                case "f5":
                case "ctrlleft":
                    _viewModel.RefreshCommand.Execute(null);
                    break;
                case "buttonl2":
                case "info":
                case "guide":
                case "i":
                case "g":
                case "numpadadd":
                case "buttonthumbl":
                case "tab":
                case "f1":
                case "f6":
                case "f7":
                case "f8":
                case "focus":
                case "camera":
                case "menu":
                    await Detail_Clicked(this, null);
                    break;
                case "red":
                case "progred":
                case "F9":
                    await ToggleRecord();
                    break;
                case "green":
                case "proggreen":
                case "F10":
                    await _viewModel.ShowChannelMenu();
                    break;
                case "yellow":
                case "progyellow":
                case "F11":
                    ToggleSubtitles();
                    break;
                case "blue":
                case "progblue":
                case "F12":
                    ToggleAudioStream();
                    break;
                case "mediaplaypause":
                    await ActionPlay(false);
                    break;
                case "mediastop":
                    await ActionStop(true);
                    break;
            }
        }

        private async Task ToggleRecord()
        {
            if (_viewModel.SelectedChannel == null)
            {
                return;
            }

            if (_viewModel.RecordingChannel == null)
            {
                MessagingCenter.Send(new PlayStreamInfo { Channel = _viewModel.SelectedChannel }, BaseViewModel.MSG_PlayAndRecordStream);
            } else
            {
                await _viewModel.StopRecord();
            }
        }

        private void ToggleAudioStream()
        {
            _loggingService.Info($"ToggleAudioStream");

            if ((_lastToggledAudioStreamTime != DateTime.MinValue) && (DateTime.Now - _lastToggledAudioStreamTime).TotalSeconds < 3)
            {
                return;
            }

            _lastToggledAudioStreamTime = DateTime.Now;

            if (_mediaPlayer == null)
                return;

            if (_viewModel.PlayingChannelAudioTracks.Count <= 1)
                return;

            var currentAudioTrack = _mediaPlayer.AudioTrack;
            if (currentAudioTrack == -1)
                return;

            var firstAudioTrackId = -1;
            string firstAudioTrackName = null;

            var select = false;
            var selected = false;

            string selectedName = null;
            int selectedId = -1;

            foreach (var track in _viewModel.PlayingChannelAudioTracks)
            {
                if (firstAudioTrackId == -1)
                {
                    firstAudioTrackId = track.Key;
                    firstAudioTrackName = track.Value;
                }

                // toggle next track
                if (track.Key == currentAudioTrack)
                {
                    select = true;
                }
                else
                {
                    if (select)
                    {
                        selectedName = track.Value;
                        selectedId = track.Key;
                        selected = true;
                        break;
                    }
                }
            }

            if (!selected)
            {
                selectedName = firstAudioTrackName;
                selectedId = firstAudioTrackId;
            }

            SetAudioTrack(selectedId);

            if (string.IsNullOrEmpty(selectedName)) selectedName = $"# {selectedId}";

            MessagingCenter.Send($"Setting audio track {selectedName}", BaseViewModel.MSG_ToastMessage);

            _loggingService.Info($"Selected audio track: {selectedName}");
        }

        private void ToggleSubtitles()
        {
            _loggingService.Info($"ToggleSubtitles");

            if ((_lastToggledSubtitlesTime != DateTime.MinValue) && (DateTime.Now - _lastToggledSubtitlesTime).TotalSeconds < 3)
            {
                return;
            }

            _lastToggledSubtitlesTime = DateTime.Now;

            if (_mediaPlayer == null)
                return;

            if (_viewModel.PlayingChannelSubtitles.Count == 0)
                return;

            var currentSubtitlesTrack = _mediaPlayer.Spu;

            var firstSubtitlesId = -1;
            string firstSubtitlesName = null;

            var select = false;
            var selected = false;

            string selectedName = null;
            int selectedId = -1;

            foreach (var subt in _viewModel.PlayingChannelSubtitles)
            {
                if (firstSubtitlesId == -1)
                {
                    firstSubtitlesId = subt.Key;
                    firstSubtitlesName = subt.Value;
                }

                // toggle next track
                if (subt.Key == currentSubtitlesTrack)
                {
                    select = true;
                }
                else
                {
                    if (select)
                    {
                        selectedName = subt.Value;
                        selectedId = subt.Key;
                        selected = true;
                        break;
                    }
                }
            }

            if (!selected)
            {
                if (currentSubtitlesTrack == -1)
                {
                    selectedName = firstSubtitlesName;
                    selectedId = firstSubtitlesId;
                } else
                {
                    selectedName = "disabled";
                    selectedId = -1;
                }
            }

            SetSubtitles(selectedId);

            if (string.IsNullOrEmpty(selectedName)) selectedName = $"# {selectedId}";

            if (selectedName == "disabled")
            {
                MessagingCenter.Send($"Subtitles disabled", BaseViewModel.MSG_ToastMessage);
            } else
            {
                MessagingCenter.Send($"Setting subtitles {selectedName}", BaseViewModel.MSG_ToastMessage);
            }

            _loggingService.Info($"Selected subtitles: {selectedName}");
        }

        private void HandleNumKey(int number)
        {
            _loggingService.Debug($"HandleNumKey {number}");

            if ((DateTime.Now - _lastNumPressedTime).TotalSeconds > 1)
            {
                _lastNumPressedTime = DateTime.MinValue;
                _numberPressed = System.String.Empty;
            }

            _lastNumPressedTime = DateTime.Now;
            _numberPressed += number;

            MessagingCenter.Send(_numberPressed, BaseViewModel.MSG_ToastMessage);

            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                var numberPressedBefore = _numberPressed;

                Thread.Sleep(2000);

                if (numberPressedBefore == _numberPressed)
                {
                    Task.Run(async () =>
                    {
                        if (_numberPressed == "0")
                        {
                            switch (_viewModel.PlayingState)
                            {
                                case PlayingStateEnum.Playing:
                                    await ActionLeft();
                                    break;
                                case PlayingStateEnum.PlayingInPreview:
                                    _viewModel.SelectedChannel = _viewModel.PlayingChannel;
                                    await ActionPlay(_viewModel.RecordingChannel != null, _viewModel.PlayingChannel);
                                    break;
                                case PlayingStateEnum.Stopped:
                                    if (_viewModel.StandingOnEnd)
                                    {
                                        await ActionFirstOrLast(true);
                                        _lastTimeHome = true;
                                    }
                                    else
                                        if (_viewModel.StandingOnStart)
                                    {
                                        await ActionFirstOrLast(false);
                                        _lastTimeHome = false;
                                    }
                                    else
                                    {
                                        await ActionFirstOrLast(_lastTimeHome);
                                        _lastTimeHome = !_lastTimeHome;
                                    }
                                    break;
                            };

                            return;
                        }

                        await _viewModel.SelectChannelByNumber(_numberPressed);

                        if (
                                (_viewModel.SelectedChannel != null) &&
                                (_numberPressed == _viewModel.SelectedChannel.Number)
                            )
                        {
                            await ActionPlay(false);
                        }
                    });
                }

            }).Start();
        }

        private async Task ActionOK(bool longPress)
        {
            _loggingService.Debug($"ActionOK");

            try
            {
                if (_viewModel.TunningButtonVisible && (_viewModel.SelectedPart  != SelectedPartEnum.ToolBar))
                {
                    ToolTune_Clicked(this, null);
                }
                else
                if (PlayingState == PlayingStateEnum.Playing)
                {
                    if (longPress)
                    {
                        ToolMenu_Clicked(this, null);
                    }
                    else
                    {
                            if (_viewModel.EPGDetailEnabled)
                            {
                                _viewModel.EPGDetailEnabled = false;
                            }
                            else
                            {
                                if (_viewModel.PlayingChannel != null &&
                                    _viewModel.SelectedChannel.CurrentEventItem != null)
                                {
                                    _viewModel.EPGDetailEnabled = true;
                                    _viewModel.SelectedPart = SelectedPartEnum.ChannelsListOrVideo;
                                }
                                else
                                {
                                    await ShowActualPlayingMessage();
                                }
                            }
                    }
                }
                else
                {
                    switch (_viewModel.SelectedPart)
                    {
                        case SelectedPartEnum.ChannelsListOrVideo:
                            if (longPress)
                            {
                                ToolMenu_Clicked(this, null);
                            }
                            else
                            {
                                await ActionPlay(false, _viewModel.SelectedChannel);
                            }
                            break;
                        case SelectedPartEnum.EPGDetail:
                            await ActionPlay(false, _viewModel.SelectedChannel);
                            return;

                        case SelectedPartEnum.ToolBar:
                            ActionPressToolBar(_viewModel.SelectedToolbarItemName);
                            return;
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "ActionOK general error");
            }
        }

        private async Task ActionRight()
        {
            _loggingService.Debug($"ActionRight");

            try
            {
                if (PlayingState == PlayingStateEnum.Playing)
                {
                    if (_viewModel.EPGDetailVisible)
                    {
                        if (_viewModel.SelectedPart != SelectedPartEnum.EPGDetail)
                        {
                            _viewModel.SelectedPart = SelectedPartEnum.EPGDetail;
                        }
                        else
                        {
                            _viewModel.SelectedPart = SelectedPartEnum.ChannelsListOrVideo;
                        }
                    }
                }
                else
                {
                    if (_viewModel.SelectedPart == SelectedPartEnum.ChannelsListOrVideo)
                    {
                        if (_viewModel.EPGDetailVisible)
                        {
                            _viewModel.SelectedPart = SelectedPartEnum.EPGDetail;
                        }
                        else
                        {
                            _viewModel.SelectedToolbarItemName = "ToolbarItemDriver";
                            _viewModel.SelectedPart = SelectedPartEnum.ToolBar;
                            _tuneFocusItem.DeFocus();
                        }
                    }
                    else if (_viewModel.SelectedPart == SelectedPartEnum.ToolBar)
                    {
                        if (_viewModel.SelectedToolbarItemName == "ToolbarItemSettings")
                        {
                            _viewModel.SelectedToolbarItemName = null;
                            _viewModel.SelectedPart = SelectedPartEnum.ChannelsListOrVideo;
                            if (_viewModel.TunningButtonVisible)
                            {
                                _tuneFocusItem.Focus();
                            }
                        }
                        else
                        {
                            SelectNextToolBarItem();
                        }
                    }
                    else if (_viewModel.SelectedPart == SelectedPartEnum.EPGDetail)
                    {
                        _viewModel.SelectedToolbarItemName = "ToolbarItemDriver";
                        _viewModel.SelectedPart = SelectedPartEnum.ToolBar;
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "ActionRight general error");
            }
        }

        private async Task ActionLeft()
        {
            _loggingService.Debug($"ActionLeft");

            try
            {
                if (PlayingState == PlayingStateEnum.Playing)
                {
                    // play previous channel
                    if (_lastPlayedChannels != null &&
                        _lastPlayedChannels[0] != _viewModel.SelectedChannel)
                    {
                        _viewModel.SelectedChannel = _lastPlayedChannels[0];
                    }
                    else
                    if (!_viewModel.StandingOnStart)
                    {
                        await _viewModel.SelectPreviousChannel();
                    }

                    await ActionPlay(false);
                }
                else
                {
                    if (_viewModel.SelectedPart == SelectedPartEnum.ChannelsListOrVideo)
                    {
                        _viewModel.SelectedToolbarItemName = "ToolbarItemSettings";
                        _viewModel.SelectedPart = SelectedPartEnum.ToolBar;
                        _tuneFocusItem.DeFocus();

                    }
                    else if (_viewModel.SelectedPart == SelectedPartEnum.EPGDetail)
                    {
                        await ScrollViewChannelEPGDescription.ScrollToAsync(0, 0, false);
                        _viewModel.SelectedPart = SelectedPartEnum.ChannelsListOrVideo;
                    }
                    else if (_viewModel.SelectedPart == SelectedPartEnum.ToolBar)
                    {
                        if (_viewModel.SelectedToolbarItemName == "ToolbarItemDriver")
                        {
                            _viewModel.SelectedToolbarItemName = null;

                            if (_viewModel.EPGDetailVisible)
                            {
                                _viewModel.SelectedPart = SelectedPartEnum.EPGDetail;
                            }
                            else
                            {
                                await ScrollViewChannelEPGDescription.ScrollToAsync(0, 0, false);
                                _viewModel.SelectedPart = SelectedPartEnum.ChannelsListOrVideo;
                                if (_viewModel.TunningButtonVisible)
                                {
                                    _tuneFocusItem.Focus();
                                }
                            }
                        }
                        else
                        {
                            SelecPreviousToolBarItem();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "ActionLeft general error");
            }
        }

        private async Task ActionDown(int step = 1)
        {
            _loggingService.Info($"ActionDown");

            try
            {
                if (PlayingState == PlayingStateEnum.Playing)
                {
                    if (_viewModel.EPGDetailVisible)
                    {
                        if (_viewModel.SelectedPart != SelectedPartEnum.EPGDetail)
                        {
                            _viewModel.SelectedPart = SelectedPartEnum.EPGDetail;
                        }
                        else
                        {
                            await ScrollViewChannelEPGDescription.ScrollToAsync(ScrollViewChannelEPGDescription.ScrollX, ScrollViewChannelEPGDescription.ScrollY + 10 + (int)_config.AppFontSize, false);
                        }
                    }
                    else
                    {
                        if (!_viewModel.StandingOnEnd)
                        {
                            await _viewModel.SelectNextChannel(step);
                            await ActionPlay(false);
                        }
                    }
                }
                else
                {
                    if (_viewModel.SelectedPart == SelectedPartEnum.ChannelsListOrVideo)
                    {
                        await _viewModel.SelectNextChannel(step);
                    }
                    else if (_viewModel.SelectedPart == SelectedPartEnum.EPGDetail)
                    {
                        await ScrollViewChannelEPGDescription.ScrollToAsync(ScrollViewChannelEPGDescription.ScrollX, ScrollViewChannelEPGDescription.ScrollY + 10 + (int)_config.AppFontSize, false);
                    }
                    else if (_viewModel.SelectedPart == SelectedPartEnum.ToolBar)
                    {
                        _viewModel.SelectedToolbarItemName = null;
                        _viewModel.SelectedPart = SelectedPartEnum.ChannelsListOrVideo;
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "ActionDown general error");
            }
        }

        private async Task ActionFirstOrLast(bool first)
        {
            _loggingService.Info($"ActionFirstOrLast");

            try
            {
                if ((PlayingState != PlayingStateEnum.Playing) && (_viewModel.SelectedPart == SelectedPartEnum.ChannelsListOrVideo))
                {
                    await _viewModel.SelectFirstOrLastChannel(first);
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "ActionFirstOrLast general error");
            }
        }

        private async Task ActionBack(bool longPress)
        {
            _loggingService.Info($"ActionBack");

            if (PlayingState == PlayingStateEnum.Playing || PlayingState == PlayingStateEnum.PlayingInPreview)
            {
                if (_viewModel.EPGDetailEnabled)
                {
                    _viewModel.EPGDetailEnabled = false;
                }
                else
                {

                    await ActionStop(longPress);
                    _lastBackPressedTime = DateTime.MinValue;
                }

                return;
            }

            if (longPress)
            {
                MessagingCenter.Send<string>(string.Empty, BaseViewModel.MSG_QuitApp);
                return;
            }

            if ((_lastBackPressedTime == DateTime.MinValue) || ((DateTime.Now - _lastBackPressedTime).TotalSeconds > 3))
            {
                MessagingCenter.Send($"Press back again to exit", BaseViewModel.MSG_ToastMessage);
                _lastBackPressedTime = DateTime.Now;
            }
            else
            {
                MessagingCenter.Send<string>(string.Empty, BaseViewModel.MSG_QuitApp);
            }
        }

        private async Task ActionUp(int step = 1)
        {
            _loggingService.Info($"ActionUp");

            try
            {
                if (PlayingState == PlayingStateEnum.Playing)
                {
                    if (_viewModel.EPGDetailVisible)
                    {
                        if (_viewModel.SelectedPart != SelectedPartEnum.EPGDetail)
                        {
                            _viewModel.SelectedPart = SelectedPartEnum.EPGDetail;
                        }
                        else
                        {
                            await ScrollViewChannelEPGDescription.ScrollToAsync(ScrollViewChannelEPGDescription.ScrollX, ScrollViewChannelEPGDescription.ScrollY - (10 + (int)_config.AppFontSize), false);
                        }
                    }
                    else
                    {
                        if (!_viewModel.StandingOnStart)
                        {
                            await _viewModel.SelectPreviousChannel(step);
                            await ActionPlay(false);
                        }
                    }
                }
                else
                {
                    if (_viewModel.SelectedPart == SelectedPartEnum.ChannelsListOrVideo)
                    {
                        await _viewModel.SelectPreviousChannel(step);
                    }
                    else if (_viewModel.SelectedPart == SelectedPartEnum.EPGDetail)
                    {
                        await ScrollViewChannelEPGDescription.ScrollToAsync(ScrollViewChannelEPGDescription.ScrollX, ScrollViewChannelEPGDescription.ScrollY - (10 + (int)_config.AppFontSize), false);
                    }
                    else if (_viewModel.SelectedPart == SelectedPartEnum.ToolBar)
                    {
                        _viewModel.SelectedToolbarItemName = null;
                        _viewModel.SelectedPart = SelectedPartEnum.ChannelsListOrVideo;
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "ActionUp general error");
            }
        }

        private void ActionPressToolBar(string itemName)
        {
            switch (itemName)
            {
                case "ToolbarItemDriver":
                    ToolConnect_Clicked(this, null);
                    return;
                case "ToolbarItemTune":
                    ToolTune_Clicked(this, null);
                    return;
                case "ToolbarItemSettings":
                    ToolSettingsPage_Clicked(this, null);
                    return;
                case "ToolbarItemMenu":
                    ToolMenu_Clicked(this, null);
                    return;
            }
        }

        private void SelecPreviousToolBarItem()
        {
            _loggingService.Info($"SelecPreviousToolBarItem");

            if (_viewModel.SelectedToolbarItemName == null)
            {
                _viewModel.SelectedToolbarItemName = "ToolbarItemSettings";
            }
            else
            if (_viewModel.SelectedToolbarItemName == "ToolbarItemSettings")
            {
                _viewModel.SelectedToolbarItemName = "ToolbarItemMenu";
            }
            else
            if (_viewModel.SelectedToolbarItemName == "ToolbarItemMenu")
            {
                _viewModel.SelectedToolbarItemName = "ToolbarItemTune";
            }
            else
            if (_viewModel.SelectedToolbarItemName == "ToolbarItemTune")
            {
                _viewModel.SelectedToolbarItemName = "ToolbarItemDriver";
            }
            else
            if (_viewModel.SelectedToolbarItemName == "ToolbarItemDriver")
            {
                _viewModel.SelectedPart = SelectedPartEnum.ChannelsListOrVideo;
                _viewModel.SelectedToolbarItemName = null;
            }

            _viewModel.NotifyToolBarChange();
        }

        private void SelectNextToolBarItem()
        {
            _loggingService.Info($"SelecNextToolBarItem");

            if (_viewModel.SelectedToolbarItemName == null)
            {
                _viewModel.SelectedToolbarItemName = "ToolbarItemDriver";
            }
            else
            if (_viewModel.SelectedToolbarItemName == "ToolbarItemDriver")
            {
                _viewModel.SelectedToolbarItemName = "ToolbarItemTune";
            }
            else
            if (_viewModel.SelectedToolbarItemName == "ToolbarItemTune")
            {
                _viewModel.SelectedToolbarItemName = "ToolbarItemMenu";
            }
            else
            if (_viewModel.SelectedToolbarItemName == "ToolbarItemMenu")
            {
                _viewModel.SelectedToolbarItemName = "ToolbarItemSettings";
            }
            else
            if (_viewModel.SelectedToolbarItemName == "ToolbarItemSettings")
            {
                _viewModel.SelectedPart = SelectedPartEnum.ChannelsListOrVideo;
                _viewModel.SelectedToolbarItemName = null;
            }

            _viewModel.NotifyToolBarChange();
        }

        private void EditSelectedChannel()
        {
            var ch = _viewModel.SelectedChannel;
            if (ch != null)
            {
                _editChannelPage.Channel = ch;
                Navigation.PushAsync(_editChannelPage);
            }
        }

        private async Task ShowActualPlayingMessage(PlayStreamInfo playStreamInfo = null)
        {
            if (playStreamInfo == null ||
                playStreamInfo.Channel == null)
            {
                if (_viewModel.SelectedChannel == null)
                    return;

                playStreamInfo = new PlayStreamInfo
                {
                    Channel = _viewModel.SelectedChannel
                };

                playStreamInfo.CurrentEvent = await _viewModel.GetChannelEPG(_viewModel.SelectedChannel);
            }

            var msg = "\u25B6 " + playStreamInfo.Channel.Name;
            if (playStreamInfo.CurrentEvent != null && playStreamInfo.CurrentEvent.CurrentEventItem != null)
                msg += $" - {playStreamInfo.CurrentEvent.CurrentEventItem.EventName}";

            // showing signal percents only for the first time
            if (playStreamInfo.SignalStrengthPercentage > 0)
            {
                msg += $" (signal {playStreamInfo.SignalStrengthPercentage}%)";
                playStreamInfo.SignalStrengthPercentage = 0;
            }

            MessagingCenter.Send(msg, BaseViewModel.MSG_ToastMessage);
        }

        private async Task Detail_Clicked(object sender, EventArgs e)
        {
            _loggingService.Info($"Detail_Clicked");

            if ((PlayingState == PlayingStateEnum.Playing) || (PlayingState == PlayingStateEnum.PlayingInPreview))
            {
                await ShowActualPlayingMessage();
            }
            else
            {
                await _viewModel.ShowChannelMenu();
            }
        }

        private async void ToolConnect_Clicked(object sender, EventArgs e)
        {
            if (_driver.Started)
            {
                if (!(await _dlgService.Confirm($"Connected device: {_driver.Configuration.DeviceName}.", $"Device status", "Back", "Disconnect")))
                {
                    await _viewModel.DisconnectDriver();
                }

            }
            else
            {

                if (await _dlgService.Confirm($"Disconnected.", $"Device status", "Connect", "Back"))
                {
                    MessagingCenter.Send("", BaseViewModel.MSG_Init);
                }
            }
        }

        private void ToolSettingsPage_Clicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(_settingsPage);
        }

        private void ToolTune_Clicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(_tuneOptionsPage);
        }

        private void ToolRefresh_Clicked(object sender, EventArgs e)
        {
            if (_firstStartup)
            {
                _viewModel.RefreshCommand.Execute(null);
            }
        }

        private async void ToolMenu_Clicked(object sender, EventArgs e)
        {
            await _viewModel.ShowChannelMenu();
        }

        private void ChannelsListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (!_viewModel.DoNotScrollToChannel)
            {
                ChannelsListView.ScrollTo(_viewModel.SelectedChannel, ScrollToPosition.MakeVisible, false);
            }

            if (PlayingState != PlayingStateEnum.Playing)
            { // EPG detail should not be enabled when playing on fullscreen (avoiding not necessary RefreshGUI calling)
                _viewModel.EPGDetailEnabled = true;
            }
        }

        private void OnVideoSingleTapped(object sender, EventArgs e)
        {
            Task.Run(async () =>
            {
                await ActionTap(1);
            });
        }

        public void OnVideoDoubleTapped(object sender, EventArgs e)
        {
            Task.Run(async () =>
            {
                await ActionTap(2);
            });
        }

        public async Task ActionTap(int count)
        {
            _loggingService.Info($"ActionTap: {count}");

            try
            {
                if (count == 1)
                {
                    if (PlayingState == PlayingStateEnum.PlayingInPreview || PlayingState == PlayingStateEnum.Playing)
                    {
                        if (_viewModel.EPGDetailEnabled)
                        {
                            _viewModel.EPGDetailEnabled = false;
                        }
                        else
                        {
                            if (_viewModel.PlayingChannel != null &&
                                _viewModel.SelectedChannel.CurrentEventItem != null)
                            {
                                _viewModel.EPGDetailEnabled = true;
                                _viewModel.SelectedPart = SelectedPartEnum.ChannelsListOrVideo;
                            }
                            else
                            {
                                ShowActualPlayingMessage();
                            }
                        }
                    }
                }

                if (count == 2)
                {
                    if (PlayingState == PlayingStateEnum.PlayingInPreview)
                    {
                        PlayingState = PlayingStateEnum.Playing;
                    }
                    else
                    if (PlayingState == PlayingStateEnum.Playing)
                    {
                        PlayingState = PlayingStateEnum.PlayingInPreview;
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "ActionTap general error");
                //MessagingCenter.Send($"Chyba: {ex.Message}", BaseViewModel.MSG_ToastMessage);
            }
        }

        private void EPDDetail_SwipedRight(object sender, SwipedEventArgs e)
        {
            _viewModel.EPGDetailEnabled = false;
        }

        private void EPDDetail_SwipedDown(object sender, SwipedEventArgs e)
        {
            _viewModel.EPGDetailEnabled = false;
        }

        private void SwipeGestureRecognizer_Swiped(object sender, SwipedEventArgs e)
        {
            if (e.Direction == SwipeDirection.Left)
            {
                Task.Run(async () => await ActionStop(true));
            }
            else
            {
                // right

                Task.Run(async () => await ActionStop(false));
            }
        }

        private void SwipeGestureRecognizer_Up(object sender, SwipedEventArgs e)
        {
            Task.Run(async () =>
            {
                if (PlayingState == PlayingStateEnum.Playing)
                {
                    if (_viewModel.EPGDetailVisible)
                    {
                        _viewModel.EPGDetailEnabled = false;
                    }
                    else
                    {
                        PlayingState = PlayingStateEnum.PlayingInPreview;
                    }
                }
                else if (PlayingState == PlayingStateEnum.PlayingInPreview)
                {
                    PlayingState = PlayingStateEnum.Playing;
                }
            });
        }

        private void SwipeGestureRecognizer_Down(object sender, SwipedEventArgs e)
        {
            Task.Run(async () =>
            {
                if (PlayingState == PlayingStateEnum.Playing)
                {
                    if (_viewModel.EPGDetailVisible)
                    {
                        _viewModel.EPGDetailEnabled = false;
                    }
                    else
                    {
                        PlayingState = PlayingStateEnum.PlayingInPreview;
                    }
                }
                else if (PlayingState == PlayingStateEnum.PlayingInPreview)
                {
                    PlayingState = PlayingStateEnum.Playing;
                }
            });
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            System.Diagnostics.Debug.WriteLine($"OnSizeAllocated: {width}/{height}");

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

            //_loggingService.Info("RefreshGUI");

                Device.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        _semaphoreSlimForRefreshGUI.WaitAsync();

                        AbsoluteLayout.SetLayoutFlags(VideoStackLayout, AbsoluteLayoutFlags.All);
                        AbsoluteLayout.SetLayoutFlags(NoVideoStackLayout, AbsoluteLayoutFlags.All);

                        //_loggingService.Debug($"PlayingState: {PlayingState}");

                        switch (PlayingState)
                        {
                            case PlayingStateEnum.Playing:

                                // turn off tool bar
                                if (NavigationPage.GetHasNavigationBar(this))
                                {
                                    NavigationPage.SetHasNavigationBar(this, false);
                                }

                                MessagingCenter.Send(System.String.Empty, BaseViewModel.MSG_EnableFullScreen);

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
                                        AbsoluteLayout.SetLayoutBounds(VideoStackLayout, new Rectangle(0, 0, 1, 1));
                                        AbsoluteLayout.SetLayoutBounds(NoVideoStackLayout, new Rectangle(0, 0, 1, 1));
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
                                        AbsoluteLayout.SetLayoutBounds(VideoStackLayout, new Rectangle(0, 0, 1, 1));
                                        AbsoluteLayout.SetLayoutBounds(NoVideoStackLayout, new Rectangle(0, 0, 1, 1));
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

                                if (!_config.Fullscreen)
                                {
                                    MessagingCenter.Send(System.String.Empty, BaseViewModel.MSG_DisableFullScreen);
                                }

                                if (IsPortrait)
                                {
                                    if (_viewModel.EPGDetailVisible)
                                    {
                                        AbsoluteLayout.SetLayoutBounds(EPGDetailGrid, PortraitPreviewEPGDetailGridPosition);
                                        AbsoluteLayout.SetLayoutBounds(ChannelsListView, PortraitChannelsListViewPositionWhenEPGDetailVisible);
                                    }
                                    else
                                    {
                                        AbsoluteLayout.SetLayoutBounds(ChannelsListView, new Rectangle(0, 0, 1, 1));
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
                                        AbsoluteLayout.SetLayoutBounds(ChannelsListView, new Rectangle(0, 0, 1, 1));
                                    }

                                    AbsoluteLayout.SetLayoutBounds(VideoStackLayout, LandscapePreviewVideoStackLayoutPosition);
                                    AbsoluteLayout.SetLayoutBounds(NoVideoStackLayout, LandscapePreviewVideoStackLayoutPosition);
                                    AbsoluteLayout.SetLayoutBounds(RecordingLabel, LandscapePreviewRecordingLabelPosition);
                                }

                                //CheckStreamCommand.Execute(null);

                                break;
                            case PlayingStateEnum.Stopped:

                                if (!NavigationPage.GetHasNavigationBar(this))
                                {
                                    NavigationPage.SetHasNavigationBar(this, true);
                                }

                                //ChannelsListView.IsVisible = true;

                                if (!_config.Fullscreen)
                                {
                                    MessagingCenter.Send(System.String.Empty, BaseViewModel.MSG_DisableFullScreen);
                                }

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

        public async Task ActionPlay(bool recording, DVBTChannel channel = null)
        {
            _loggingService.Debug($"Calling ActionPlay (recording: {recording}");

            try
            {
                _refreshGUIEnabled = false;
                _checkStreamEnabled = false;

                if (channel == null)
                    channel = _viewModel.SelectedChannel;

                if (channel == null)
                    return;

                _loggingService.Debug($"playing: {channel.Name} ({channel.Number})");

                if (!_driver.Started)
                {
                    MessagingCenter.Send($"Playing {channel.Name} failed (device connection error)", BaseViewModel.MSG_ToastMessage);
                    return;
                }

                if (_driver.Recording)
                {
                    MessagingCenter.Send($"Playing {channel.Name} failed (raw stream recording in progress)", BaseViewModel.MSG_ToastMessage);
                    return;
                }

                long? signalStrengthPercentage = null;

                if ((_viewModel.RecordingChannel != null) && (_viewModel.RecordingChannel != channel))
                {
                    MessagingCenter.Send($"Playing {channel.Name} failed (recording in progress)", BaseViewModel.MSG_ToastMessage);
                    return;
                }

                var shouldMediaPlay = true;
                var shouldDriverPlay = true;
                var shouldMediaRecord = false;

                // just playing  ?
                if (PlayingState != PlayingStateEnum.Stopped)
                {
                    if (_viewModel.PlayingChannel != channel)
                    {
                        // playing different channel
                        shouldMediaPlay = true;
                        shouldDriverPlay = true;
                    }
                    else
                    {
                        // playing the same channel
                        shouldDriverPlay = false;
                        shouldMediaPlay = false;
                    }

                    if (recording && _viewModel.RecordingChannel == null)
                    {
                        // start recording
                        shouldMediaRecord = true;
                        shouldMediaPlay = true;
                        shouldDriverPlay = false;
                    }
                }
                else
                {
                    if (recording && _viewModel.RecordingChannel == channel)
                    {
                        shouldMediaPlay = true;
                        shouldDriverPlay = false;
                        shouldMediaRecord = false;
                    }
                    else
                    {
                        shouldMediaPlay = true;
                        shouldDriverPlay = true;
                        shouldMediaRecord = recording;
                    }
                }

                //_loggingService.Debug($"shouldMediaPlay: {shouldMediaPlay}");
                //_loggingService.Debug($"shouldDriverPlay: {shouldDriverPlay}");
                //_loggingService.Debug($"shouldMediaRecord: {shouldMediaRecord}");

                if (shouldMediaPlay)
                {
                    if (videoView.MediaPlayer.IsPlaying)
                    {
                        _loggingService.Debug("Stopping Media player");

                        CallWithTimeout(delegate
                        {
                            videoView.MediaPlayer.Stop();
                        });
                    }
                }

                if (shouldDriverPlay)
                {
                    var tunedRes = await _driver.TuneEnhanced(channel.Frequency, channel.Bandwdith, channel.DVBTType, channel.PIDsArary, false);
                    if (tunedRes.Result != SearchProgramResultEnum.OK)
                    {
                        switch (tunedRes.Result)
                        {
                            case SearchProgramResultEnum.NoSignal:
                                MessagingCenter.Send($"No signal", BaseViewModel.MSG_ToastMessage);
                                break;
                            default:
                                MessagingCenter.Send($"Playing failed", BaseViewModel.MSG_ToastMessage);
                                break;
                        }

                        return;
                    }

                    /*
                    if (_config.ScanEPG)
                    {
                        var ev = await _viewModel.GetChannelEPG(channel);
                        if (ev == null)
                        {
                            MessagingCenter.Send($"Scanning EPG...", BaseViewModel.MSG_ToastMessage);
                            await _viewModel.EIT.Scan(1500);
                        }
                    }
                    */
                    //_driver.StopReadStream();
                    _driver.SendStream();

                    signalStrengthPercentage = tunedRes.SignalPercentStrength;
                }

                if (shouldMediaPlay)
                {
                    _media = new Media(_libVLC, _driver.VideoStream, new string[] { });

                    if (shouldMediaRecord)
                    {
                        _viewModel.RecordingChannel = channel;

                        channel.Recording = true;

                        _media.AddOption(":sout-all");  //  does not work? only first audio and no subtitles recorded!
                        _media.AddOption(":sout-keep");
                        _media.AddOption(":sout=#duplicate{dst=display,dst=file{dst=\"" + _viewModel.RecordingFileName + "\"}}");

                        MessagingCenter.Send($"Recording started", BaseViewModel.MSG_ToastMessage);
                    }

                    CallWithTimeout(delegate
                    {
                        videoView.MediaPlayer.Play(_media);
                    });

                    SetSubtitles(-1);
                    SetAudioTrack(-100);
                }

                var playInfo = new PlayStreamInfo
                {
                    Channel = channel
                };

                if (signalStrengthPercentage.HasValue)
                {
                    playInfo.SignalStrengthPercentage = Convert.ToInt32(signalStrengthPercentage.Value);
                }

                playInfo.CurrentEvent = await _viewModel.GetChannelEPG(channel);

                await ShowActualPlayingMessage(playInfo);

                if (_config.PlayOnBackground)
                {
                    if (recording)
                    {
                        MessagingCenter.Send<PlayStreamInfo>(playInfo, BaseViewModel.MSG_ShowRecordNotification);
                    }
                    MessagingCenter.Send<MainPage, PlayStreamInfo>(this, BaseViewModel.MSG_PlayInBackgroundNotification, playInfo);
                }

                _viewModel.PlayingChannel = channel;
                _viewModel.PlayingChannelSubtitles.Clear();
                _viewModel.PlayingChannelAudioTracks.Clear();
                _viewModel.PlayingChannelAspect = new Size(-1, -1);
                PlayingState = PlayingStateEnum.Playing;
                _viewModel.EPGDetailEnabled = false;

                if (_lastPlayedChannels[1] != channel)
                {
                    _lastPlayedChannels[0] = _lastPlayedChannels[1];
                    _lastPlayedChannels[1] = channel;
                }

                _viewModel.NotifyMediaChange();
            } finally
            {
                _refreshGUIEnabled = true;
                _checkStreamEnabled = true;
                RefreshGUI();
            }
        }

        public async Task ActionStop(bool force)
        {
            _loggingService.Debug($"Calling ActionStop (Force: {force}, PlayingState: {PlayingState})");

            if (_media == null || videoView == null || videoView.MediaPlayer == null)
                return;

            _viewModel.SelectedPart = SelectedPartEnum.ChannelsListOrVideo;
            _viewModel.EPGDetailEnabled = false;

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
                Device.BeginInvokeOnMainThread(async () =>
                {
                    if (_viewModel.RecordingChannel == null)
                    {
                        CallWithTimeout(delegate
                        {
                            videoView.MediaPlayer.Stop();
                        });
                        await _driver.Stop();
                    }
                    else
                    {
                        // Mute does not work
                        SetAudioTrack(-1);
                    }
                });

                PlayingState = PlayingStateEnum.Stopped;
                _viewModel.PlayingChannelSubtitles.Clear();
                _viewModel.PlayingChannelAudioTracks.Clear();
                _viewModel.PlayingChannelAspect = new Size(-1, -1);
                _viewModel.PlayingChannel = null;

                MessagingCenter.Send("", BaseViewModel.MSG_StopPlayInBackgroundNotification);
            }

            _viewModel.SelectedToolbarItemName = null;
            _viewModel.SelectedPart = SelectedPartEnum.ChannelsListOrVideo;
            _viewModel.NotifyMediaChange();
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

        private async Task CheckStream()
        {
            //_loggingService.Debug("CheckStream");

            if (!_checkStreamEnabled)
                return;

            if (PlayingState == PlayingStateEnum.Stopped)
            {
                return;
            }

            CallWithTimeout(delegate
            {
                try
                {
                    // checking stopped stream
                    if (!videoView.MediaPlayer.IsPlaying)
                    {
                        videoView.MediaPlayer.Play();
                    }

                    // checking no video
                    var videoTreackCount = videoView.MediaPlayer.VideoTrackCount;

                    if (videoTreackCount <= 0)
                    {
                        Device.BeginInvokeOnMainThread(() =>
                        {
                            NoVideoStackLayout.IsVisible = true;
                            //VideoStackLayout.IsVisible = false;
                            AbsoluteLayout.SetLayoutBounds(VideoStackLayout, NoVideoStackLayoutPosition);
                        });
                    }
                    else
                    {
                        //PreviewVideoBordersFix();

                        Device.BeginInvokeOnMainThread(() =>
                        {
                            NoVideoStackLayout.IsVisible = false;
                            VideoStackLayout.IsVisible = true;

                            if (AbsoluteLayout.GetLayoutBounds(VideoStackLayout) == NoVideoStackLayoutPosition)
                            {
                                _loggingService.Debug("CheckStream - VideoStackLayout has invalid bounds");
                                RefreshGUI();
                            }
                        });
                    }

                    var actualSubtitleTrack = videoView.MediaPlayer.Spu;
                    var actualAudioTrack = videoView.MediaPlayer.AudioTrack;

                    //_loggingService.Debug($"CheckStream - ActualSubtitleTrack: {actualSubtitleTrack}");
                    //_loggingService.Debug($"CheckStream - ActualAudioTrack: {actualAudioTrack}");

                    // setting subtitles
                    foreach (var desc in videoView.MediaPlayer.SpuDescription)
                    {
                        if (!_viewModel.PlayingChannelSubtitles.ContainsKey(desc.Id))
                        {
                            _loggingService.Debug($"CheckStream - Adding subtitle {desc.Name}");
                            _viewModel.PlayingChannelSubtitles.Add(desc.Id, desc.Name);
                        }
                    }

                    // setting audio tracks
                    foreach (var desc in videoView.MediaPlayer.AudioTrackDescription)
                    {
                        if (!_viewModel.PlayingChannelAudioTracks.ContainsKey(desc.Id))
                        {
                            _loggingService.Debug($"CheckStream - Adding audio track {desc.Name}");
                            _viewModel.PlayingChannelAudioTracks.Add(desc.Id, desc.Name);
                        }
                    }

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

                    // check subtitles
                    if (actualSubtitleTrack != _viewModel.Subtitles)
                    {
                        _loggingService.Debug($"CheckStream - invalid subtitles {actualSubtitleTrack}, setting {_viewModel.Subtitles}");
                        videoView.MediaPlayer.SetSpu(_viewModel.Subtitles);
                    }

                    // check audio track
                    if (actualAudioTrack != _viewModel.AudioTrack)
                    {
                        if ((_viewModel.AudioTrack == -100) && (actualAudioTrack != -1))
                        {
                            _loggingService.Debug($"CheckStream - Setting automatic audio track {actualAudioTrack}");
                            _viewModel.AudioTrack = actualAudioTrack;

                            if (_viewModel.RecordingChannel != null)
                            {
                                _loggingService.Debug($"CheckStream - Record audio fix");

                                // workaround for ducplicate sound when sout duplicate
                                Device.BeginInvokeOnMainThread(() =>
                                {
                                    videoView.MediaPlayer.SetAudioTrack(-1);
                                    videoView.MediaPlayer.SetAudioTrack(actualAudioTrack);
                                });
                            }
                        }
                        else
                        {
                            _loggingService.Debug($"CheckStream - invalid audio track {actualAudioTrack}, setting {_viewModel.AudioTrack}");
                            videoView.MediaPlayer.SetAudioTrack(_viewModel.AudioTrack);
                        }
                    }

                }
                catch (Exception ex)
                {
                    _loggingService.Error(ex, "CheckStream general error");
                }
            });
        }

        private MediaTrack? GetVideoTrack()
        {
            if (_media.Tracks != null &&
                _media.Tracks.Length > 0 &&
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

        private void PreviewVideoBordersFix()
        {
            try
            {
                if (PlayingState == PlayingStateEnum.PlayingInPreview)
                {
                    var videoTrack = GetVideoTrack();

                    if (!videoTrack.HasValue)
                        return;

                    var originalVideoWidth = videoTrack.Value.Data.Video.Width;
                    var originalVideoHeight = videoTrack.Value.Data.Video.Height;

                    if (IsPortrait)
                    {
                        var aspect = (double)originalVideoWidth / (double)originalVideoHeight;

                        // TODO: get REAL video aspect, MPEGTS returns width 720 despite the real width is 1024!

                        var newVideoHeight = VideoStackLayout.Width / aspect;

                        var borderTopHeight = (VideoStackLayout.Height - newVideoHeight);

                        var rect = new Rectangle()
                        {
                            Left = VideoStackLayout.X,
                            Top = VideoStackLayout.Y + borderTopHeight,
                            Width = VideoStackLayout.Width,
                            Height = newVideoHeight
                        };

                        if (rect.X != VideoStackLayout.X ||
                            rect.Y != VideoStackLayout.Y ||
                            rect.Width != VideoStackLayout.Width ||
                            rect.Height != newVideoHeight)
                        {
                            AbsoluteLayout.SetLayoutFlags(VideoStackLayout, AbsoluteLayoutFlags.None);
                            AbsoluteLayout.SetLayoutBounds(VideoStackLayout, rect);
                        }
                    }
                    else
                    {
                        var aspect = (double)originalVideoHeight / (double)originalVideoWidth;
                        var newVideoWidth = VideoStackLayout.Height / aspect;

                        var borderLeftWidth = (VideoStackLayout.Width - newVideoWidth);

                        var rect = new Rectangle()
                        {
                            Left = VideoStackLayout.X + borderLeftWidth,
                            Top = VideoStackLayout.Y,
                            Width = newVideoWidth,
                            Height = VideoStackLayout.Height
                        };

                        if (rect.X != VideoStackLayout.X ||
                            rect.Y != VideoStackLayout.Y ||
                            rect.Width != newVideoWidth ||
                            rect.Height != VideoStackLayout.Height)
                        {
                            AbsoluteLayout.SetLayoutFlags(VideoStackLayout, AbsoluteLayoutFlags.None);
                            AbsoluteLayout.SetLayoutBounds(VideoStackLayout, rect);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
        }
    }
}