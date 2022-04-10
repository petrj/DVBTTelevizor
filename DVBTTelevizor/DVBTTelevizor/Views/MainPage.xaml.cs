using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using System.IO;
using System.Threading;
using LoggerService;
using LibVLCSharp.Shared;

namespace DVBTTelevizor
{
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        private MainPageViewModel _viewModel;

        private IDVBTDriverManager _driver;
        private DialogService _dlgService;
        private ILoggingService _loggingService;
        private DVBTTelevizorConfiguration _config;
        private PlayerPage _playerPage;
        private ServicePage _servicePage;
        private ChannelPage _editChannelPage;
        private TunePage _tunePage;
        private SettingsPage _settingsPage;
        private ChannelService _channelService;

        private DateTime _lastNumPressedTime = DateTime.MinValue;
        private string _numberPressed = String.Empty;
        private bool _firstStartup = true;
        private Size _lastAllocatedSize = new Size(-1, -1);

        public bool IsPortrait { get; private set; } = false;

        private LibVLC _libVLC = null;
        private MediaPlayer _mediaPlayer;
        private Media _media = null;

        public MainPage(ILoggingService loggingService, DVBTTelevizorConfiguration config, IDVBTDriverManager driverManager)
        {
            InitializeComponent();

            _dlgService = new DialogService(this);

            _loggingService = loggingService;

            _config = config;

            _driver = driverManager;

            try
            {
                _playerPage = new PlayerPage(_driver, _config);
            } catch (Exception ex)
            {
                _loggingService.Error(ex, "Error while initializing player page");
            }

            _channelService = new ConfigChannelService(_loggingService, _config);

            _tunePage = new TunePage(_loggingService, _dlgService, _driver, _config, _channelService);
            _servicePage = new ServicePage(_loggingService, _dlgService, _driver, _config, _playerPage);
            _settingsPage = new SettingsPage(_loggingService, _dlgService, _config, _channelService);
            _editChannelPage = new ChannelPage(_loggingService,_dlgService, _driver, _config);

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

            _servicePage.Disappearing += anyPage_Disappearing;
            _servicePage.Disappearing += anyPage_Disappearing;
            _tunePage.Disappearing += anyPage_Disappearing;
            _settingsPage.Disappearing += anyPage_Disappearing;
            _editChannelPage.Disappearing += _editChannelPage_Disappearing;
            ChannelsListView.ItemSelected += ChannelsListView_ItemSelected;

            Appearing += MainPage_Appearing;

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_KeyDown, (key) =>
            {
                OnKeyDown(key);
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_EditChannel, (message) =>
            {
                Xamarin.Forms.Device.BeginInvokeOnMainThread(
                    delegate
                    {
                        EditSelectedChannel();
                    });
            });

            MessagingCenter.Subscribe<PlayStreamInfo> (this, BaseViewModel.MSG_PlayStream, (playStreamInfo) =>
            {
                Device.BeginInvokeOnMainThread(
                 new Action(() =>
                 {
                     if (_playerPage != null)
                     {
                         _playerPage.PlayStreamInfo = playStreamInfo;

                         if (_playerPage.Playing)
                         {
                             //_playerPage.StopPlay();
                             _playerPage.StartPlay();
                         }
                         else
                         {
                             Navigation.PushModalAsync(_playerPage);
                         }

                         ShowActualPlayingMessage(playStreamInfo);

                         if (_config.PlayOnBackground)
                         {
                             MessagingCenter.Send<MainPage, PlayStreamInfo>(this, BaseViewModel.MSG_PlayInBackgroundNotification, playStreamInfo);
                         }
                     }
                     else
                     {
                         Task.Run(async () =>
                         {
                             await _dlgService.Error("Player not initialized");
                         });
                     }
                 }));
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
                OnKeyUp();
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_PlayNextChannel, (msg) =>
            {
                OnKeyDown();
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_StopStream, (msg) =>
            {
                StopPlayback();
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_ImportChannelsList, (message) =>
            {
                _viewModel.ImportCommand.Execute(message);
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
                _viewModel.PlayingState = value;

                RefreshGUI();
            }
        }

        private void MainPage_Appearing(object sender, EventArgs e)
        {
            if (!_config.ShowServiceMenu && ToolbarItems.Contains(ToolServicePage))
            {
                ToolbarItems.Remove(ToolServicePage);
            }

            if (_config.ShowServiceMenu && !ToolbarItems.Contains(ToolServicePage))
            {
                // adding before settings
                ToolbarItems.Remove(ToolSettingsPage);

                ToolbarItems.Add(ToolServicePage);
                ToolbarItems.Add(ToolSettingsPage);
            }
        }

        public void ResumePlayback()
        {
            if (_playerPage != null && _playerPage.Playing)
            {
                _playerPage.Resume();
            }
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
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // workaround for non selected channel at startup
            if (_firstStartup)
            {
                _firstStartup = false;

                _viewModel.RefreshCommand.Execute(null);
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

                _servicePage.Done();
            });
        }

        public void StopPlayback()
        {
            if (_playerPage != null && _playerPage.Playing)
            {
                MessagingCenter.Send("", BaseViewModel.MSG_StopPlayInBackgroundNotification);
                _playerPage.StopPlay();
                Navigation.PopModalAsync();
            }
        }

        public async void OnKeyDown(string key)
        {
            _loggingService.Debug($"OnKeyDown {key}");

            // key events can be consumed only on this MainPage

            var stack = Navigation.NavigationStack;
            if (stack[stack.Count - 1].GetType() != typeof(MainPage))
            {
                // different page on navigation top
                return;
            }

            switch (key.ToLower())
            {
                case "dpaddown":
                case "buttonr1":
                case "down":
                case "s":
                case "numpad2":
                    await OnKeyDown();
                    break;
                case "dpadup":
                case "buttonl1":
                case "up":
                case "w":
                case "numpad8":
                    await OnKeyUp();
                    break;
                case "dpadleft":
                case "pageup":
                case "left":
                case "a":
                case "b":
                case "f2":
                case "mediaplayprevious":
                case "mediaprevious":
                case "numpad4":
                    await OnKeyLeft();
                    break;
                case "pagedown":
                case "dpadright":
                case "right":
                case "d":
                case "f":
                case "f3":
                case "mediaplaynext":
                case "medianext":
                case "numpad6":
                    await OnKeyRight();
                    break;
                case "dpadcenter":
                case "space":
                case "buttonr2":
                case "mediaplaypause":
                case "mediaplay":
                case "enter":
                case "numpad5":
                case "buttona":
                case "buttonstart":
                    //await _viewModel.PlayChannel();
                    await ActionPlay();
                    break;
                case "f4":
                case "escape":
                case "mediaplaystop":
                case "mediastop":
                case "mediaclose":
                case "numpadsubtract":
                case "del":
                case "buttonx":
                    //StopPlayback();
                    await ActionStop(false);
                    break;
                case "num0":
                case "number0":
                    HandleNumKey(0);
                    break;
                case "num1":
                case "number1":
                    HandleNumKey(1);
                    break;
                case "num2":
                case "number2":
                    HandleNumKey(2);
                    break;
                case "num3":
                case "number3":
                    HandleNumKey(3);
                    break;
                case "num4":
                case "number4":
                    HandleNumKey(4);
                    break;
                case "num5":
                case "number5":
                    HandleNumKey(5);
                    break;
                case "num6":
                case "number6":
                    HandleNumKey(6);
                    break;
                case "num7":
                case "number7":
                    HandleNumKey(7);
                    break;
                case "num8":
                case "number8":
                    HandleNumKey(8);
                    break;
                case "num9":
                case "number9":
                    HandleNumKey(9);
                    break;
                case "f5":
                case "numpad0":
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
                    await Detail_Clicked(this,null);
                    break;
                default:
                    {
                        _loggingService.Debug($"Unbound key: {key}");
#if DEBUG
                        MessagingCenter.Send($"Unbound key: {key}", BaseViewModel.MSG_ToastMessage);
#endif
                    }
                    break;
            }
        }

        private void HandleNumKey(int number)
        {
            _loggingService.Debug($"HandleNumKey {number}");

            if ((DateTime.Now - _lastNumPressedTime).TotalSeconds > 1)
            {
                _lastNumPressedTime = DateTime.MinValue;
                _numberPressed = String.Empty;
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
                        await _viewModel.SelectChannelByNumber(_numberPressed);

                        if (
                                (_viewModel.SelectedChannel != null) &&
                                (_numberPressed == _viewModel.SelectedChannel.Number)
                           )
                        {
                            await _viewModel.PlayChannel();
                        }
                    });
                }

            }).Start();
        }

        private async Task OnKeyLeft()
        {
            await _viewModel.SelectPreviousChannel(10);
        }

        private async Task OnKeyRight()
        {
            await _viewModel.SelectNextChannel(10);
        }

        private async Task OnKeyDown()
        {
            var currentChannel = _viewModel.SelectedChannel;

            await _viewModel.SelectNextChannel();

            if (currentChannel == _viewModel.SelectedChannel)
                return; // no channel chaned

            if (_playerPage != null && _playerPage.Playing)
            {
                await _viewModel.PlayChannel(_viewModel.SelectedChannel);
            }
        }

        private async Task OnKeyUp()
        {
            var currentChannel = _viewModel.SelectedChannel;

            await _viewModel.SelectPreviousChannel();

            if (currentChannel == _viewModel.SelectedChannel)
                return; // no channel chaned

            if (_playerPage != null && _playerPage.Playing)
            {
                await _viewModel.PlayChannel(_viewModel.SelectedChannel);
            }
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

        private void ShowActualPlayingMessage(PlayStreamInfo playStreamInfo)
        {
            if (playStreamInfo == null ||
                playStreamInfo.Channel == null)
                return;

            var msg = "\u25B6 " + playStreamInfo.Channel.Name;
            if (playStreamInfo.CurrentEvent != null)
                msg += $" - {playStreamInfo.CurrentEvent.EventName}";

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

            if (_playerPage != null && _playerPage.Playing)
            {
                ShowActualPlayingMessage(_playerPage.PlayStreamInfo);
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

            } else
            {

                if (await _dlgService.Confirm($"Disconnected.", $"Device status", "Connect", "Back"))
                {
                    MessagingCenter.Send("", BaseViewModel.MSG_Init);
                }
            }
        }

        private void ToolServicePage_Clicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(_servicePage);
        }

        private void ToolSettingsPage_Clicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(_settingsPage);
        }

        private void ToolTune_Clicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(_tunePage);
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
            if (!_viewModel. DoNotScrollToChannel)
            {
                ChannelsListView.ScrollTo(_viewModel.SelectedChannel, ScrollToPosition.MakeVisible, false);
            }

            _viewModel.DoNotScrollToChannel = false;
        }

        private void OnVideoSingleTapped(object sender, EventArgs e)
        {

        }

        public void OnVideoDoubleTapped(object sender, EventArgs e)
        {

        }

        private void SwipeGestureRecognizer_Swiped(object sender, SwipedEventArgs e)
        {
            if (e.Direction == SwipeDirection.Left)
            {
                Task.Run(async () => await ActionStop(true));
            }
            else
            {
                Task.Run(async () => await ActionStop(false));
            }
        }

        private void SwipeGestureRecognizer_Up(object sender, SwipedEventArgs e)
        {

        }

        private void SwipeGestureRecognizer_Down(object sender, SwipedEventArgs e)
        {

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
            Device.BeginInvokeOnMainThread(() =>
            {
                switch (PlayingState)
                {
                    case PlayingStateEnum.Playing:

                        // turn off tool bar
                        NavigationPage.SetHasNavigationBar(this, false);

                        MessagingCenter.Send(String.Empty, BaseViewModel.MSG_EnableFullScreen);

                        // VideoStackLayout must be visible before changing Layout
                        VideoStackLayout.IsVisible = true;
                        AbsoluteLayout.SetLayoutFlags(VideoStackLayout, AbsoluteLayoutFlags.All);
                        AbsoluteLayout.SetLayoutBounds(VideoStackLayout, new Rectangle(0, 0, 1, 1));

                        break;
                    case PlayingStateEnum.PlayingInPreview:

                        NavigationPage.SetHasNavigationBar(this, true);

                        if (!_config.Fullscreen)
                        {
                            MessagingCenter.Send(String.Empty, BaseViewModel.MSG_DisableFullScreen);
                        }

                        AbsoluteLayout.SetLayoutFlags(VideoStackLayout, AbsoluteLayoutFlags.All);
                        AbsoluteLayout.SetLayoutBounds(VideoStackLayout, new Rectangle(1, 1, 0.5, 0.35));

                        //CheckStreamCommand.Execute(null);

                        break;
                    case PlayingStateEnum.Stopped:
                    case PlayingStateEnum.Recording:

                        NavigationPage.SetHasNavigationBar(this, true);

                        if (!_config.Fullscreen)
                        {
                            MessagingCenter.Send(String.Empty, BaseViewModel.MSG_DisableFullScreen);
                        }

                        VideoStackLayout.IsVisible = false;

                        break;
                }
            });
        }

        public async Task ActionPlay(DVBTChannel channel = null)
        {
            if (channel == null)
                channel = _viewModel.SelectedChannel;

            if (channel == null)
                return;

            if (PlayingState == PlayingStateEnum.Recording)
            {
                MessagingCenter.Send($"Playing {channel.Name} failed (recording in progress)", BaseViewModel.MSG_ToastMessage);
                return;
            }

            if (PlayingState == PlayingStateEnum.PlayingInPreview && _viewModel.PlayingChannel == channel)
            {
                PlayingState = PlayingStateEnum.Playing;
                return;
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                if (PlayingState == PlayingStateEnum.Playing || PlayingState == PlayingStateEnum.PlayingInPreview)
                {
                    videoView.MediaPlayer.Stop();
                }
            });

            if (!_driver.Started)
            {
                MessagingCenter.Send($"Playing {channel.Name} failed (device connection error)", BaseViewModel.MSG_ToastMessage);
                return;
            }

            var playRes = await _driver.Play(channel.Frequency, channel.Bandwdith, channel.DVBTType, channel.PIDsArary);
            if (!playRes.OK)
            {
                MessagingCenter.Send($"Playing {channel.Name} failed (device connection error)", BaseViewModel.MSG_ToastMessage);
                return;
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                if (_driver.VideoStream != null)
                {
                    _media = new Media(_libVLC, _driver.VideoStream, new string[] { });
                    videoView.MediaPlayer.Play(_media);
                }
            });

            var playInfo = new PlayStreamInfo
            {
                Channel = channel,
                SignalStrengthPercentage = playRes.SignalStrengthPercentage
            };

            var eitManager = _driver.GetEITManager(channel.Frequency);
            if (eitManager != null)
            {
                playInfo.CurrentEvent = eitManager.GetEvent(DateTime.Now, Convert.ToInt32(channel.ProgramMapPID));
            }

            ShowActualPlayingMessage(playInfo);

            if (_config.PlayOnBackground)
            {
                MessagingCenter.Send<MainPage, PlayStreamInfo>(this, BaseViewModel.MSG_PlayInBackgroundNotification, playInfo);
            }

            _viewModel.PlayingChannel = channel;
            PlayingState = PlayingStateEnum.Playing;

        }

        public async Task ActionStop(bool force)
        {
            if (_media == null || videoView == null || videoView.MediaPlayer == null)
                return;

            if (!force && (PlayingState == PlayingStateEnum.Playing))
            {
                PlayingState = PlayingStateEnum.PlayingInPreview;
            }
            else
            if (force || (PlayingState == PlayingStateEnum.PlayingInPreview))
            {
                Device.BeginInvokeOnMainThread(async () =>
                {
                    videoView.MediaPlayer.Stop();
                });
                PlayingState = PlayingStateEnum.Stopped;
                _viewModel.PlayingChannel = null;

                if (!_driver.Recording)
                {
                    await _driver.Stop();
                }
            }
        }

    }
}
