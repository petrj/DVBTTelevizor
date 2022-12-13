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
using static DVBTTelevizor.MainPageViewModel;

namespace DVBTTelevizor
{
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage, IOnKeyDown
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

        public Command CheckStreamCommand { get; set; }

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
            _editChannelPage = new ChannelPage(_loggingService, _dlgService, _driver, _config);

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

            _servicePage.Disappearing += anyPage_Disappearing;
            _servicePage.Disappearing += anyPage_Disappearing;
            _tunePage.Disappearing += anyPage_Disappearing;
            _settingsPage.Disappearing += anyPage_Disappearing;
            _editChannelPage.Disappearing += _editChannelPage_Disappearing;
            ChannelsListView.ItemSelected += ChannelsListView_ItemSelected;

            Appearing += MainPage_Appearing;

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
                    await ActionPlay();
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

        public async void OnKeyDown(string key, bool longPress)
        {
            _loggingService.Debug($"OnKeyDown {key}");

            // key events can be consumed only on this MainPage

#if DEBUG
            if (longPress)
            {
                MessagingCenter.Send($"Long key: {key}", BaseViewModel.MSG_ToastMessage);
            }
            else
            {
                MessagingCenter.Send($"key: {key}", BaseViewModel.MSG_ToastMessage);
            }
#endif

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

            switch (key.ToLower())
            {
                case "dpaddown":
                case "buttonr1":
                case "down":
                case "s":
                case "numpad2":
                    await ActionDown();
                    break;
                case "dpadup":
                case "buttonl1":
                case "up":
                case "w":
                case "numpad8":
                    await ActionUp();
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
                    await ActionLeft();
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
                    await ActionRight();
                    break;

                case "dpadcenter":
                case "space":
                case "buttonr2":
                case "mediaplay":
                case "enter":
                case "numpad5":
                case "numpadenter":
                case "buttona":
                case "buttonstart":
                case "capslock":
                case "comma":
                case "semicolon":
                case "grave":
                    await ActionOK(longPress);
                    break;


                //case "mediaplaypause":
                //await ActionPlay();

                //break;
                case "f4":
                case "escape":
                case "mediaplaystop":
                case "mediastop":
                case "mediaclose":
                case "numpadsubtract":
                case "del":
                case "buttonx":
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
                    await Detail_Clicked(this, null);
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

        private async Task ActionOK(bool longPress)
        {
            _loggingService.Debug($"ActionOK");

            try
            {
                if (PlayingState == PlayingStateEnum.Playing)
                {
                    if (longPress)
                    {
                        ToolMenu_Clicked(this, null);
                    } else
                    {
                        ShowActualPlayingMessage();
                    }
                }
                else
                {
                    switch (_viewModel.SelectedPart)
                    {
                        case SelectedPartEnum.ChannelsList:
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
                    if (!_viewModel.StandingOnEnd)
                    {
                        await _viewModel.SelectNextChannel();
                        await _viewModel.PlayChannel();
                    }
                }
                else
                {
                    if (_viewModel.SelectedPart == SelectedPartEnum.ChannelsList)
                    {
                        if (_viewModel.EPGDetailVisible)
                        {
                            _viewModel.SelectedPart = SelectedPartEnum.EPGDetail;
                        } else
                        {
                            _viewModel.SelectedToolbarItemName = "ToolbarItemDriver";
                            _viewModel.SelectedPart = SelectedPartEnum.ToolBar;
                        }
                    }
                    else if (_viewModel.SelectedPart == SelectedPartEnum.ToolBar)
                    {
                        if (_viewModel.SelectedToolbarItemName == "ToolbarItemSettings")
                        {
                            _viewModel.SelectedToolbarItemName = null;
                            _viewModel.SelectedPart = SelectedPartEnum.ChannelsList;
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
                    // TODO: play last channel

                    if (!_viewModel.StandingOnStart)
                    {
                        await _viewModel.SelectPreviousChannel();
                        await _viewModel.PlayChannel();
                    }
                }
                else
                {
                    if (_viewModel.SelectedPart == SelectedPartEnum.ChannelsList)
                    {
                        _viewModel.SelectedToolbarItemName = "ToolbarItemSettings";
                        _viewModel.SelectedPart = SelectedPartEnum.ToolBar;

                    }
                    else if (_viewModel.SelectedPart == SelectedPartEnum.EPGDetail)
                    {
                        await ScrollViewChannelEPGDescription.ScrollToAsync(0, 0, false);
                        _viewModel.SelectedPart = SelectedPartEnum.ChannelsList;
                    }
                    else if (_viewModel.SelectedPart == SelectedPartEnum.ToolBar)
                    {
                        if (_viewModel.SelectedToolbarItemName == "ToolbarItemDriver")
                        {
                            if (_viewModel.EPGDetailVisible)
                            {
                                _viewModel.SelectedPart = SelectedPartEnum.EPGDetail;
                            }
                            else
                            {
                                await ScrollViewChannelEPGDescription.ScrollToAsync(0, 0, false);
                                _viewModel.SelectedPart = SelectedPartEnum.ChannelsList;
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

        private async Task ActionDown()
        {
            _loggingService.Info($"ActionDown");

            try
            {
                if (PlayingState == PlayingStateEnum.Playing)
                {
                    if (!_viewModel.StandingOnEnd)
                    {
                        await _viewModel.SelectNextChannel();
                        await _viewModel.PlayChannel();
                    }
                }
                else
                {
                    if (_viewModel.SelectedPart == SelectedPartEnum.ChannelsList)
                    {
                        await _viewModel.SelectNextChannel();
                    }
                    else if (_viewModel.SelectedPart == SelectedPartEnum.EPGDetail)
                    {
                        await ScrollViewChannelEPGDescription.ScrollToAsync(ScrollViewChannelEPGDescription.ScrollX, ScrollViewChannelEPGDescription.ScrollY + 10 + (int)_config.AppFontSize, false);
                    }
                    else if (_viewModel.SelectedPart == SelectedPartEnum.ToolBar)
                    {
                        _viewModel.SelectedToolbarItemName = null;
                        _viewModel.SelectedPart = SelectedPartEnum.ChannelsList;
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "ActionDown general error");
                //MessagingCenter.Send($"Chyba: {ex.Message}", BaseViewModel.MSG_ToastMessage);
            }
        }

        private async Task ActionUp()
        {
            _loggingService.Info($"ActionUp");

            try
            {
                if (PlayingState == PlayingStateEnum.Playing)
                {
                    if (!_viewModel.StandingOnStart)
                    {
                        await _viewModel.SelectPreviousChannel();
                        await _viewModel.PlayChannel();
                    }
                }
                else
                {
                    if (_viewModel.SelectedPart == SelectedPartEnum.ChannelsList)
                    {
                        await _viewModel.SelectPreviousChannel();
                    }
                    else if (_viewModel.SelectedPart == SelectedPartEnum.EPGDetail)
                    {
                        await ScrollViewChannelEPGDescription.ScrollToAsync(ScrollViewChannelEPGDescription.ScrollX, ScrollViewChannelEPGDescription.ScrollY - (10 + (int)_config.AppFontSize), false);
                    }
                    else if (_viewModel.SelectedPart == SelectedPartEnum.ToolBar)
                    {
                        _viewModel.SelectedToolbarItemName = null;
                        _viewModel.SelectedPart = SelectedPartEnum.ChannelsList;
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "ActionUp general error");
                MessagingCenter.Send($"Chyba: {ex.Message}", BaseViewModel.MSG_ToastMessage);
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
                case "ToolbarItemTools":
                    ToolServicePage_Clicked(this, null);
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
                _viewModel.SelectedToolbarItemName = "ToolbarItemTools";
            }
            else
            if (_viewModel.SelectedToolbarItemName == "ToolbarItemTools")
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
                _viewModel.SelectedPart = SelectedPartEnum.ChannelsList;
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
                _viewModel.SelectedToolbarItemName = "ToolbarItemTools";
            }
            else
            if (_viewModel.SelectedToolbarItemName == "ToolbarItemTools")
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
                _viewModel.SelectedPart = SelectedPartEnum.ChannelsList;
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

        private void ShowActualPlayingMessage(PlayStreamInfo playStreamInfo = null)
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

                var eitManager = _driver.GetEITManager(_viewModel.SelectedChannel.Frequency);
                if (eitManager != null)
                {
                    playStreamInfo.CurrentEvent = eitManager.GetEvent(DateTime.Now, Convert.ToInt32(_viewModel.SelectedChannel.ProgramMapPID));
                }
            }

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
            ShowActualPlayingMessage();
        }

        public void OnVideoDoubleTapped(object sender, EventArgs e)
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
                        NoVideoStackLayout.IsVisible = false;
                        //ChannelsListView.IsVisible = false;

                        AbsoluteLayout.SetLayoutFlags(VideoStackLayout, AbsoluteLayoutFlags.All);
                        AbsoluteLayout.SetLayoutBounds(VideoStackLayout, new Rectangle(0, 0, 1, 1));

                        AbsoluteLayout.SetLayoutFlags(NoVideoStackLayout, AbsoluteLayoutFlags.All);
                        AbsoluteLayout.SetLayoutBounds(NoVideoStackLayout, new Rectangle(0, 0, 1, 1));

                        CheckStreamCommand.Execute(null);

                        break;
                    case PlayingStateEnum.PlayingInPreview:

                        NavigationPage.SetHasNavigationBar(this, true);

                        //ChannelsListView.IsVisible = true;

                        if (!_config.Fullscreen)
                        {
                            MessagingCenter.Send(String.Empty, BaseViewModel.MSG_DisableFullScreen);
                        }

                        AbsoluteLayout.SetLayoutFlags(VideoStackLayout, AbsoluteLayoutFlags.All);
                        AbsoluteLayout.SetLayoutBounds(VideoStackLayout, new Rectangle(1, 1, 0.5, 0.35));

                        AbsoluteLayout.SetLayoutFlags(NoVideoStackLayout, AbsoluteLayoutFlags.All);
                        AbsoluteLayout.SetLayoutBounds(NoVideoStackLayout, new Rectangle(1, 1, 0.5, 0.35));

                        CheckStreamCommand.Execute(null);

                        break;
                    case PlayingStateEnum.Stopped:
                    case PlayingStateEnum.Recording:

                        NavigationPage.SetHasNavigationBar(this, true);

                        //ChannelsListView.IsVisible = true;

                        if (!_config.Fullscreen)
                        {
                            MessagingCenter.Send(String.Empty, BaseViewModel.MSG_DisableFullScreen);
                        }

                        VideoStackLayout.IsVisible = false;
                        NoVideoStackLayout.IsVisible = false;

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
            _loggingService.Debug($"Calling ActionStop (Force: {force}, PlayingState: {PlayingState})");

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

        private async Task CheckStream()
        {
            if (PlayingState == PlayingStateEnum.Stopped)
            {
                return;
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                if (!videoView.MediaPlayer.IsPlaying)
                {
                    videoView.MediaPlayer.Play(_media);
                }

                if (videoView.MediaPlayer.VideoTrackCount <= 0)
                {
                    NoVideoStackLayout.IsVisible = true;
                    VideoStackLayout.IsVisible = false;
                }
                else
                {
                    NoVideoStackLayout.IsVisible = false;
                    VideoStackLayout.IsVisible = true;

                    PreviewVideoBordersFix();
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

            } else
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
                        var newVideoHeight = VideoStackLayout.Width / aspect;

                        var borderHeight = (VideoStackLayout.Height - newVideoHeight) / 2.0;

                        var rect = new Rectangle()
                        {
                            Left = VideoStackLayout.X,
                            Top = VideoStackLayout.Y + borderHeight,
                            Width = VideoStackLayout.Width,
                            Height = newVideoHeight
                        };

                        if (rect.X != VideoStackLayout.X ||
                            rect.Y != VideoStackLayout.Y ||
                            rect.Width != VideoStackLayout.Width ||
                            rect.Height != VideoStackLayout.Height)
                        {
                            AbsoluteLayout.SetLayoutFlags(VideoStackLayout, AbsoluteLayoutFlags.None);
                            AbsoluteLayout.SetLayoutBounds(VideoStackLayout, rect);
                        }
                    } else
                    {
                        var aspect = (double)originalVideoHeight / (double)originalVideoWidth;
                        var newVideoWidth = VideoStackLayout.Height / aspect;

                        var borderWidth = (VideoStackLayout.Width - newVideoWidth) / 2.0;

                        var rect = new Rectangle()
                        {
                            Left = VideoStackLayout.X + borderWidth,
                            Top = VideoStackLayout.Y,
                            Width = newVideoWidth,
                            Height = VideoStackLayout.Height
                        };

                        if (rect.X != VideoStackLayout.X ||
                            rect.Y != VideoStackLayout.Y ||
                            rect.Width != VideoStackLayout.Width ||
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
