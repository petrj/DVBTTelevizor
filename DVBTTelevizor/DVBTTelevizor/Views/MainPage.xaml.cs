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

namespace DVBTTelevizor
{
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        private MainPageViewModel _viewModel;

        private DVBTDriverManager _driver;
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

        public MainPage(ILoggingService loggingService, DVBTTelevizorConfiguration config, DVBTDriverManager driverManager)
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
                    await _viewModel.PlayChannel();
                    break;
                case "f4":
                case "escape":
                case "mediaplaystop":
                case "mediastop":
                case "mediaclose":
                case "numpadsubtract":
                case "del":
                case "buttonx":
                    StopPlayback();
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

        private void ShowActualPlayingMessage(PlayStreamInfo playStreamInfo)
        {
            if (playStreamInfo == null ||
                playStreamInfo.Channel == null)
                return;

            var msg = "\u25B6 " + playStreamInfo.Channel.Name;
            if (playStreamInfo.CurrentEvent != null)
                msg += $" - {playStreamInfo.CurrentEvent.EventName}";

            MessagingCenter.Send(msg, BaseViewModel.MSG_ToastMessage);
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
    }
}
