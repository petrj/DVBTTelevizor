using Android.Hardware.Camera2;
using Android.Media;
using Java.Net;
using LibVLCSharp.Shared;
using LoggerService;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
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
        private KeyboardFocusableItem _installDriverFocusItem = null;
        private RemoteAccessService.RemoteAccessService _remoteAccessService;
        private DateTime _lastToggledAudioStreamTime = DateTime.MinValue;
        private DateTime _lastToggledSubtitlesTime = DateTime.MinValue;
        private DateTime _lastActionPlayTime = DateTime.MinValue;

        private DateTime _lastNumPressedTime = DateTime.MinValue;
        private string _numberPressed = System.String.Empty;
        private Size _lastAllocatedSize = new Size(-1, -1);
        private DateTime _lastBackPressedTime = DateTime.MinValue;
        private bool _lastTimeHome = false;

        public bool IsPortrait { get; private set; } = false;

        private LibVLC _libVLC = null;
        private LibVLCSharp.Shared.MediaPlayer _mediaPlayer;
        private Media _media = null;

        private bool _firstAppearing = true;
        private DVBTChannel[] _lastPlayedChannels = new DVBTChannel[2];
        private List<string> _remoteDevicesConnected = new List<string>();

        public Command CheckStreamCommand { get; set; }
        public Command CheckPIDsCommand { get; set; }

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
        private Rectangle NoVideoStackLayoutPosition { get; set; } = new Rectangle(-10, -10, -5, -5);

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

            BindingContext = _viewModel = new MainPageViewModel(_loggingService, _dlgService, _driver, _config, _channelService);

            _tuneOptionsPage = new TuneOptionsPage(_loggingService, _dlgService, _driver, _config, _channelService);
            _settingsPage = new SettingsPage(_loggingService, _dlgService, _config, _channelService, _driver);
            _editChannelPage = new ChannelPage(_loggingService, _dlgService, _driver, _config, _channelService);

            _editChannelPage.Disappearing += _editChannelPage_Disappearing;

            Core.Initialize();

            _libVLC = new LibVLC();
            _mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libVLC) { EnableHardwareDecoding = true };
            videoView.MediaPlayer = _mediaPlayer;



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
            CheckPIDsCommand = new Command(async () => await _driver.CheckPIDs());

            BackgroundCommandWorker.RunInBackground(CheckStreamCommand, 3, 5);

#if CHECKPIDS
            BackgroundCommandWorker.RunInBackground(CheckPIDsCommand, 60, 10);
#endif

            _tuneOptionsPage.Disappearing += AnyPage_Disappearing;
            _settingsPage.Disappearing += AnyPage_Disappearing;

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

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_ChangeAudioTrackRequest, (message) =>
            {
                Xamarin.Forms.Device.BeginInvokeOnMainThread(async () =>
                {
                    if (_viewModel.PlayingChannel != null)
                    {
                        await _viewModel.ShowAudioTrackMenu(_viewModel.PlayingChannel);
                    }
                });
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_ChangeSubtitlesRequest, (message) =>
            {
                Xamarin.Forms.Device.BeginInvokeOnMainThread(async () =>
                {
                    if (_viewModel.PlayingChannel != null)
                    {
                        await _viewModel.ShowSubtitlesMenu(_viewModel.PlayingChannel);

                    }
                });
            });

            MessagingCenter.Subscribe<PlayStreamInfo>(this, BaseViewModel.MSG_PlayStream, (playStreamInfo) =>
            {
                Task.Run(async () =>
                {
                    await ActionPlay(playStreamInfo.Channel);
                });
            });

            MessagingCenter.Subscribe<PlayStreamInfo>(this, BaseViewModel.MSG_RecordStream, (playStreamInfo) =>
            {
                Task.Run(async () =>
                {
                    await ActionRecord(playStreamInfo.Channel);
                });
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_DVBTDriverConfiguration, (message) =>
            {
                _loggingService.Debug($"Received DVBTDriverConfiguration message: {message}");

                if (!_driver.Connected)
                {
                    _viewModel.ConnectDriver(message);
                }
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_UpdateDriverState, (message) =>
            {
                _viewModel.UpdateDriverState();
                Task.Run(async () => await _viewModel.AutoPlay());
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_DVBTDriverConfigurationFailed, (message) =>
            {
                Device.BeginInvokeOnMainThread(delegate
                {
                    _viewModel.UpdateDriverState();
                    _viewModel.NotifyDriverOrChannelsChange();

                    MessagingCenter.Send($"Device connection error ({message})", BaseViewModel.MSG_ToastMessage);
                });
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_DVBTDriverConfigurationChanged, (message) =>
            {
                Device.BeginInvokeOnMainThread(delegate
                {
                    _viewModel.UpdateDriverState();
                    _viewModel.NotifyDriverOrChannelsChange();
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
                Task.Run(async () => { await ActionStopRecord(); });
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

                if (_editChannelPage != null)
                {
                    _editChannelPage.SetSubtitles(_viewModel.PlayingChannel == _viewModel.SelectedChannel, _viewModel.PlayingChannelSubtitles, id);
                }
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_ChangeAudioTrackId, (trackId) =>
            {
                int id;
                if (!int.TryParse(trackId, out id))
                {
                    return;
                }

                SetAudioTrack(id);

                if (_editChannelPage != null)
                {
                    _editChannelPage.SetAudioTracks(_viewModel.PlayingChannel == _viewModel.SelectedChannel, _viewModel.PlayingChannelAudioTracks, id);
                }
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
                    MessagingCenter.Send($"Teletext: {pageNum}", BaseViewModel.MSG_ToastMessage);
                });
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_TeletextOn, (pageNum) =>
            {
                CallWithTimeout(delegate
                {
                    SetSubtitles(-1);
                    videoView.MediaPlayer.ToggleTeletext();
                    videoView.MediaPlayer.Teletext = 100;
                    MessagingCenter.Send("Teletext activated, page: 100", BaseViewModel.MSG_ToastMessage);
                });
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_TeletextOff, (pageNum) =>
            {
                CallWithTimeout(delegate
                {
                    SetSubtitles(-1);
                    MessagingCenter.Send("Teletext deactivated", BaseViewModel.MSG_ToastMessage);
                });
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_ClearCache, (msg) =>
            {
                _viewModel.EIT.Clear();
                _viewModel.PID.Clear();
            });

            _tuneFocusItem = KeyboardFocusableItem.CreateFrom("TuneButton", new List<View> { TuneButton });
            _tuneFocusItem.Focus();

            _installDriverFocusItem = KeyboardFocusableItem.CreateFrom("DriverButton", new List<View> { DriverButton });
            _installDriverFocusItem.Focus();

            Task.Run(async () => { await _settingsPage.AcknowledgePurchases(); });

            _remoteAccessService = new RemoteAccessService.RemoteAccessService(_loggingService);
            RestartRemoteAccessService();
        }

        private async void _editChannelPage_Disappearing(object sender, EventArgs e)
        {
            if (_editChannelPage.Changed)
            {
                _viewModel.RefreshCommand.Execute(null);
            }

            if (_editChannelPage.ChannelToDelete != null)
            {
                await _viewModel.DeleteChannel(_editChannelPage.ChannelToDelete);
                _editChannelPage.ChannelToDelete = null;
            }
        }

        private void SetSubtitles(int id)
        {
            _loggingService.Info($"Setting subtitles: {id}");

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
            _loggingService.Info($"Setting audio track: {id}");

            if (id == -100)
            {
                _viewModel.AudioTrack = -100;
                return;
            }

            if ((!_viewModel.PlayingChannelAudioTracks.ContainsKey(id)) || (PlayingState == PlayingStateEnum.Stopped))
            {
                return;
            }

            CallWithTimeout(delegate
            {
                _viewModel.AudioTrack = id;
                videoView.MediaPlayer.SetAudioTrack(id);
            });
        }

        private void RestartRemoteAccessService()
        {
            _loggingService.Info("RestartRemoteAccessService");

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
            _loggingService.Info("Resume");

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

        private void AnyPage_Disappearing(object sender, EventArgs e)
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
            _loggingService.Info($"OnAppearing");

            base.OnAppearing();

            _viewModel.SelectedToolbarItemName = null;
            _viewModel.SelectedPart = SelectedPartEnum.ChannelsListOrVideo;
            if (_viewModel.TunningButtonVisible)
            {
                _tuneFocusItem.Focus();
            }
            if (_viewModel.InstallDriverButtonVisible)
            {
                _installDriverFocusItem.Focus();
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
            _loggingService.Info($"OnDisappearing");

            base.OnDisappearing();
        }

        public void Done()
        {
            _loggingService.Info($"Done");

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
                    await ActionPlayStop();
                    break;
                case "mediastop":
                    await ActionStop(true);
                    break;
            }
        }

        private async Task ToggleRecord()
        {
            _loggingService.Info($"ToggleRecord");

            if (_viewModel.SelectedChannel == null)
            {
                return;
            }

            if (_viewModel.RecordingChannel == null)
            {
                await ActionRecord();
            } else
            {
                await ActionStopRecord();
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

            if ((DateTime.Now - _lastNumPressedTime).TotalSeconds > 2)
            {
                _lastNumPressedTime = DateTime.MinValue;
                _numberPressed = System.String.Empty;
            }

            _lastNumPressedTime = DateTime.Now;
            _numberPressed += number;

            if (_viewModel.TeletextEnabled)
            {
                MessagingCenter.Send($"Teletext: {_numberPressed}", BaseViewModel.MSG_ToastMessage);
            }
            else
            {
                MessagingCenter.Send(_numberPressed, BaseViewModel.MSG_ToastMessage);
            }

            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                var numberPressedBefore = _numberPressed;

                Thread.Sleep(2000);

                if (numberPressedBefore == _numberPressed)
                {
                    Task.Run(async () =>
                    {
                        if (!_viewModel.TeletextEnabled)
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
                                        await ActionPlay(_viewModel.PlayingChannel);
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
                                await ActionPlay();
                            }
                        } else
                        {
                            MessagingCenter.Send(_numberPressed, BaseViewModel.MSG_TeletextPageNumber);
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
                if (_viewModel.InstallDriverButtonVisible && (_viewModel.SelectedPart != SelectedPartEnum.ToolBar))
                {
                    InstallDriver_Clicked(this, null);
                }
                else
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
                                    await _viewModel.ShowActualPlayingMessage();
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
                                await ActionPlay(_viewModel.SelectedChannel);
                            }
                            break;
                        case SelectedPartEnum.EPGDetail:
                            await ActionPlay(_viewModel.SelectedChannel);
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
                            _installDriverFocusItem.DeFocus();
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
                            if (_viewModel.InstallDriverButtonVisible)
                            {
                                _installDriverFocusItem.Focus();
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

                    await ActionPlay();
                }
                else
                {
                    if (_viewModel.SelectedPart == SelectedPartEnum.ChannelsListOrVideo)
                    {
                        _viewModel.SelectedToolbarItemName = "ToolbarItemSettings";
                        _viewModel.SelectedPart = SelectedPartEnum.ToolBar;
                        _tuneFocusItem.DeFocus();
                        _installDriverFocusItem.DeFocus();

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
                                if (_viewModel.InstallDriverButtonVisible)
                                {
                                    _installDriverFocusItem.Focus();
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
                        if (_viewModel.TeletextEnabled)
                        {
                            videoView.MediaPlayer.Teletext += 1;
                            MessagingCenter.Send(videoView.MediaPlayer.Teletext.ToString(), BaseViewModel.MSG_TeletextPageNumber);
                        }
                        else
                        {
                            if (!_viewModel.StandingOnEnd)
                            {
                                await _viewModel.SelectNextChannel(step);
                                await ActionPlay();
                            }
                        }
                    }
                }
                else
                {
                    if (_viewModel.SelectedPart == SelectedPartEnum.ChannelsListOrVideo)
                    {
                        if (_viewModel.TunningButtonVisible || _viewModel.InstallDriverButtonVisible)
                        {
                            _viewModel.SelectedToolbarItemName = "ToolbarItemDriver";
                            _viewModel.SelectedPart = SelectedPartEnum.ToolBar;
                            _tuneFocusItem.DeFocus();
                            _installDriverFocusItem.DeFocus();
                        }
                        else
                        {
                            await _viewModel.SelectNextChannel(step);
                        }
                    }
                    else if (_viewModel.SelectedPart == SelectedPartEnum.EPGDetail)
                    {
                        await ScrollViewChannelEPGDescription.ScrollToAsync(ScrollViewChannelEPGDescription.ScrollX, ScrollViewChannelEPGDescription.ScrollY + 10 + (int)_config.AppFontSize, false);
                    }
                    else if (_viewModel.SelectedPart == SelectedPartEnum.ToolBar)
                    {
                        _viewModel.SelectedToolbarItemName = null;
                        _viewModel.SelectedPart = SelectedPartEnum.ChannelsListOrVideo;
                        if (_viewModel.TunningButtonVisible)
                        {
                            _tuneFocusItem.Focus();
                        }
                        if (_viewModel.InstallDriverButtonVisible)
                        {
                            _installDriverFocusItem.Focus();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "ActionDown general error");
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
                        if (_viewModel.TeletextEnabled)
                        {
                            videoView.MediaPlayer.Teletext -= 1;
                            MessagingCenter.Send(videoView.MediaPlayer.Teletext.ToString(), BaseViewModel.MSG_TeletextPageNumber);
                        }
                        else
                        {
                            if (!_viewModel.StandingOnStart)
                            {
                                await _viewModel.SelectPreviousChannel(step);
                                await ActionPlay();
                            }
                        }
                    }
                }
                else
                {
                    if (_viewModel.SelectedPart == SelectedPartEnum.ChannelsListOrVideo)
                    {
                        if (_viewModel.TunningButtonVisible || _viewModel.InstallDriverButtonVisible)
                        {
                            _viewModel.SelectedToolbarItemName = "ToolbarItemDriver";
                            _viewModel.SelectedPart = SelectedPartEnum.ToolBar;
                            _tuneFocusItem.DeFocus();
                            _installDriverFocusItem.DeFocus();
                        }
                        else
                        {
                            await _viewModel.SelectPreviousChannel(step);
                        }
                    }
                    else if (_viewModel.SelectedPart == SelectedPartEnum.EPGDetail)
                    {
                        await ScrollViewChannelEPGDescription.ScrollToAsync(ScrollViewChannelEPGDescription.ScrollX, ScrollViewChannelEPGDescription.ScrollY - (10 + (int)_config.AppFontSize), false);
                    }
                    else if (_viewModel.SelectedPart == SelectedPartEnum.ToolBar)
                    {
                        _viewModel.SelectedToolbarItemName = null;
                        _viewModel.SelectedPart = SelectedPartEnum.ChannelsListOrVideo;
                        if (_viewModel.TunningButtonVisible)
                        {
                            _tuneFocusItem.Focus();
                        }
                        if (_viewModel.InstallDriverButtonVisible)
                        {
                            _installDriverFocusItem.Focus();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "ActionUp general error");
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

        private async void EditSelectedChannel()
        {
            _loggingService.Info($"EditSelectedChannel");

            var ch = _viewModel.SelectedChannel;
            if (ch != null)
            {
                try
                {
                    _editChannelPage.StreamInfoVisible = false;
                    _editChannelPage.StreamVideoSize = "";

                    if (_viewModel.PlayingChannel != null && (ch.FrequencyAndMapPID == _viewModel.PlayingChannel.FrequencyAndMapPID))
                    {
                        var videoTrack = GetVideoTrack();
                        if (videoTrack.HasValue)
                        {
                            _editChannelPage.StreamInfoVisible = true;
                            _editChannelPage.StreamVideoSize = $"{videoTrack.Value.Data.Video.Width}x{videoTrack.Value.Data.Video.Height}";
                        }

                        _editChannelPage.SetAudioTracks(true,_viewModel.PlayingChannelAudioTracks, _viewModel.AudioTrack);
                        _editChannelPage.SetSubtitles(true, _viewModel.PlayingChannelSubtitles, _viewModel.Subtitles);

                        _editChannelPage.DeleteVisible = false;
                    } else
                    {
                        _editChannelPage.SetAudioTracks(false, _viewModel.PlayingChannelAudioTracks, -1);
                        _editChannelPage.SetSubtitles(false, _viewModel.PlayingChannelSubtitles, -1);

                        _editChannelPage.DeleteVisible = true;
                    }

                    if (_viewModel.RecordingChannel != null && (ch.FrequencyAndMapPID == _viewModel.RecordingChannel.FrequencyAndMapPID))
                    {
                        _editChannelPage.DeleteVisible = false;
                    }

                } catch (Exception ex)
                {
                    _loggingService.Error(ex);
                }

                try
                {
                    if (_driver.Streaming)
                    {
                        if (_driver.Bitrate > 0)
                        {
                            _editChannelPage.StreamBitRateVisible = true;
                            _editChannelPage.Bitrate = BaseViewModel.GetHumanReadableBitRate(_driver.Bitrate);
                        } else
                        {
                            _editChannelPage.StreamBitRateVisible = false;
                        }

                        var status = await _driver.GetStatus();
                        if (status.SuccessFlag)
                        {
                            _editChannelPage.SignalStrength = $"{status.rfStrengthPercentage}%";
                            _editChannelPage.SignalStrengthVisible = true;
                        } else
                        {
                            _editChannelPage.SignalStrengthVisible = false;
                        }
                    } else
                    {
                        _editChannelPage.StreamBitRateVisible = false;
                    }
                } catch (Exception ex)
                {
                    _editChannelPage.SignalStrengthVisible = false;
                    _editChannelPage.StreamBitRateVisible = false;
                    _loggingService.Error(ex);
                }

                await _editChannelPage.Reload(ch.FrequencyAndMapPID);

                await Navigation.PushAsync(_editChannelPage);
            }
        }

        private async Task Detail_Clicked(object sender, EventArgs e)
        {
            _loggingService.Info($"Detail_Clicked");

            if ((PlayingState == PlayingStateEnum.Playing) || (PlayingState == PlayingStateEnum.PlayingInPreview))
            {
                await _viewModel.ShowActualPlayingMessage();
            }
            else
            {
                await _viewModel.ShowChannelMenu();
            }
        }

        private async void ToolConnect_Clicked(object sender, EventArgs e)
        {
            if (_driver.Connected)
            {
                if (!(await _dlgService.Confirm($"Connected device: {_driver.Configuration.DeviceName}.", $"Device status", "Back", "Disconnect")))
                {
                    await _viewModel.DisconnectDriver();
                }

            }
            else
            {
                if (_driver.Installed)
                {
                    if (await _dlgService.Confirm($"Disconnected.", $"Device status", "Connect", "Back"))
                    {
                        MessagingCenter.Send("", BaseViewModel.MSG_Init);
                    }
                } else
                {
                    if (await _dlgService.Confirm($"DVB-T Driver not installed.", $"Device status", "Install DVB-T Driver", "Back"))
                    {
                        InstallDriver_Clicked(this, null);
                    }
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

        private async void InstallDriver_Clicked(object sender, EventArgs e)
        {
            await Browser.OpenAsync("https://play.google.com/store/apps/details?id=info.martinmarinov.dvbdriver", BrowserLaunchMode.External);
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
                                _viewModel.ShowActualPlayingMessage();
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
                    } else
                    if (_viewModel.TeletextEnabled)
                    {
                        videoView.MediaPlayer.Teletext += 1;
                        MessagingCenter.Send(videoView.MediaPlayer.Teletext.ToString(), BaseViewModel.MSG_TeletextPageNumber);
                    }
                    else
                    {
                        await _viewModel.SelectPreviousChannel();
                        await ActionPlay();
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
                    if (_viewModel.TeletextEnabled)
                    {
                        videoView.MediaPlayer.Teletext -= 1;
                        MessagingCenter.Send(videoView.MediaPlayer.Teletext.ToString(), BaseViewModel.MSG_TeletextPageNumber);
                    }
                    else
                    {
                        await _viewModel.SelectNextChannel();
                        await ActionPlay();
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

        public async Task ActionPlayStop(DVBTChannel channel = null)
        {
            if (channel == null)
                channel = _viewModel.SelectedChannel;

            if (channel == null)
                return;

            if (_viewModel.PlayingChannel == channel)
            {
                await ActionStop(true);
            } else
            {
                await ActionPlay(channel);
            }
        }

        public async Task ActionPlay(DVBTChannel channel = null)
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
                    MessagingCenter.Send($"Playing {channel.Name} failed (device not connected)", BaseViewModel.MSG_ToastMessage);
                    return;
                }

                if (_viewModel.RecordingChannel != null && _viewModel.RecordingChannel != channel)
                {
                    MessagingCenter.Send($"Playing {channel.Name} failed (recording in progress)", BaseViewModel.MSG_ToastMessage);
                    return;
                }

                if (channel.NonFree)
                {
                    MessagingCenter.Send($"Playing {channel.Name} failed (non free channel)", BaseViewModel.MSG_ToastMessage);
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
                    } else
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
                        MessagingCenter.Send($"Tuning ....", BaseViewModel.MSG_LongToastMessage);
                    }

                    if (tuneNeeded)
                    {
                        MessagingCenter.Send($"Tuning {channel.FrequencyShortLabel} ....", BaseViewModel.MSG_LongToastMessage);

                        var tunedRes = await _driver.TuneEnhanced(channel.Frequency, channel.Bandwdith, channel.DVBTType, false);
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

                        signalStrengthPercentage = tunedRes.SignalState.rfStrengthPercentage;
                    }

                    var cachedPIDs = _viewModel.PID.GetChannelPIDs(channel.Frequency, channel.ProgramMapPID);

                    if (cachedPIDs != null &&
                        cachedPIDs.Count > 0)
                    {
                        var setPIDres = await _driver.SetPIDs(cachedPIDs);

                        if (!setPIDres.SuccessFlag)
                        {
                            MessagingCenter.Send($"Playing failed", BaseViewModel.MSG_ToastMessage);
                            return;
                        }
                    }
                    else
                    {
                        var setupPIDsRes = await _driver.SetupChannelPIDs(channel.ProgramMapPID, false);

                        if (setupPIDsRes.Result != SearchProgramResultEnum.OK)
                        {
                            MessagingCenter.Send($"Playing failed", BaseViewModel.MSG_ToastMessage);
                            return;
                        }

                        _viewModel.PID.AddChannelPIDs(channel.Frequency, channel.ProgramMapPID, setupPIDsRes.PIDs);
                    }

                    _driver.StartStream();

                    _lastActionPlayTime = DateTime.Now;
                }

                if (shouldMediaPlay)
                {
                    switch (_driver.DVBTDriverStreamType)
                    {
                        case Models.DVBTDriverStreamTypeEnum.UDP:
                            _media = new Media(_libVLC, _driver.StreamUrl, FromType.FromLocation);
                            break;
                        case Models.DVBTDriverStreamTypeEnum.Stream:
                            _media = new Media(_libVLC, new StreamMediaInput(_driver.VideoStream), new string[] { });
                            break;
                    }

                    CallWithTimeout(delegate
                    {
                        videoView.MediaPlayer.Play(_media);
                    });

                    SetSubtitles(-1);
                    SetAudioTrack(-100);
                    _viewModel.TeletextEnabled = false;
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
                PlayingState = PlayingStateEnum.Playing;
                _viewModel.EPGDetailEnabled = false;

                if (_lastPlayedChannels[1] != channel)
                {
                    _lastPlayedChannels[0] = _lastPlayedChannels[1];
                    _lastPlayedChannels[1] = channel;
                }

                _viewModel.NotifyMediaChange();

                playInfo.CurrentEvent = await _viewModel.GetChannelEPG(channel);

                if (playInfo.CurrentEvent == null || playInfo.CurrentEvent.CurrentEventItem == null)
                {
                    await _viewModel.ScanEPG(channel, true, true, 2000, 3000);
                }

                await _viewModel.ShowActualPlayingMessage(playInfo);

                if (_config.PlayOnBackground)
                {
                    MessagingCenter.Send<MainPage, PlayStreamInfo>(this, BaseViewModel.MSG_PlayInBackgroundNotification, playInfo);
                }

            } catch (Exception ex)
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

        public async Task ActionStop(bool force)
        {
            _loggingService.Debug($"ActionStop (Force: {force}, PlayingState: {PlayingState})");

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
                    CallWithTimeout(delegate
                    {
                        videoView.MediaPlayer.Stop();
                    });

                    if (_viewModel.RecordingChannel == null)
                    {
                        await _driver.Stop();
                    }
                });

                PlayingState = PlayingStateEnum.Stopped;

                _lastActionPlayTime = DateTime.MinValue;

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

        public async Task ActionRecord(DVBTChannel channel = null)
        {
            _loggingService.Debug($"ActionRecord");

            try
            {
                if (channel == null)
                    channel = _viewModel.SelectedChannel;

                if (channel == null)
                    return;

                if (!_driver.Connected)
                {
                    MessagingCenter.Send($"Record {channel.Name} failed (device not connected)", BaseViewModel.MSG_ToastMessage);
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
                } else
                {
                    await ActionPlay(channel);
                }

                _viewModel.RecordingChannel = channel;
                _viewModel.RecordingChannel.Recording = true;

                await _driver.StartRecording();

                var playStreamInfo = new PlayStreamInfo()
                {
                   Channel = channel,
                   CurrentEvent = await _viewModel.GetChannelEPG(channel)
                };

                MessagingCenter.Send<PlayStreamInfo>(playStreamInfo, BaseViewModel.MSG_ShowRecordNotification);

                _viewModel.NotifyMediaChange();
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
        }

        public async Task ActionStopRecord()
        {
            _loggingService.Debug($"ActionStopRecord");

            try
            {
                if (_driver.Recording)
                {
                    _driver.StopRecording();
                }

                if (PlayingState == PlayingStateEnum.Stopped)
                {
                    await _driver.Stop();
                }

                Xamarin.Forms.Device.BeginInvokeOnMainThread(delegate
                {
                    if (_viewModel.RecordingChannel != null)
                    {
                        _viewModel.RecordingChannel.Recording = false;
                        _viewModel.RecordingChannel = null;
                    }
                });

                MessagingCenter.Send($"Recording stopped", BaseViewModel.MSG_ToastMessage);

                MessagingCenter.Send<string>(string.Empty, BaseViewModel.MSG_CloseRecordNotification);

                _viewModel.NotifyMediaChange();
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
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

                    // check do data from driver

                    if (_lastActionPlayTime != DateTime.MinValue)
                    {
                        if (!_driver.DriverStreamDataAvailable)
                        {
                            var timeFromPlayMSecs = (DateTime.Now - _lastActionPlayTime).TotalMilliseconds;
                            if (timeFromPlayMSecs > 10000)
                            {
                                _loggingService.Info($"CheckStream - No data for {timeFromPlayMSecs} ms");

                                MessagingCenter.Send("", BaseViewModel.MSG_StopStream);
                                MessagingCenter.Send($"Error - no data from device", BaseViewModel.MSG_ToastMessage);
                            } else if (timeFromPlayMSecs > 5000)
                            {
                                _loggingService.Info($"CheckStream - No data for {timeFromPlayMSecs} ms");
                            }
                        }
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

                    if ((!_viewModel.TeletextEnabled) && (actualSubtitleTrack != _viewModel.Subtitles))
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

        private LibVLCSharp.Shared.MediaTrack? GetVideoTrack()
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