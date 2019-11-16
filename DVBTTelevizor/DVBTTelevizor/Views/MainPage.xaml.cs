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
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
using System.IO;
using System.Threading;
using LoggerService;

namespace DVBTTelevizor
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
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

        public MainPage(ILoggingService loggingService)
        {
            InitializeComponent();

            _dlgService = new DialogService(this);

            _loggingService = loggingService;

            _config = new DVBTTelevizorConfiguration()
            {
                AutoInitAfterStart = true
            };

            _driver = new DVBTDriverManager(_loggingService, _config);

            try
            {
                _playerPage = new PlayerPage(_driver);
            } catch (Exception ex)
            {
                _loggingService.Error(ex, "Error while initializing player page");
            }

            _channelService = new JSONChannelsService(_loggingService, _config);

            _tunePage = new TunePage(_loggingService, _dlgService, _driver, _config, _channelService);
            _servicePage = new ServicePage(_loggingService, _dlgService, _driver, _config, _playerPage);
            _settingsPage = new SettingsPage(_loggingService, _dlgService, _config, _channelService);
            _editChannelPage = new ChannelPage(_loggingService,_dlgService, _driver, _config);

            BindingContext = _viewModel = new MainPageViewModel(_loggingService, _dlgService, _driver, _config, _channelService);

            _servicePage.Disappearing += delegate
             {
                 _viewModel.RefreshCommand.Execute(null);
             };
            _tunePage.Disappearing += delegate
            {
                _viewModel.RefreshCommand.Execute(null);
            };
            _settingsPage.Disappearing += delegate
            {
                _viewModel.RefreshCommand.Execute(null);
            };
            _editChannelPage.Disappearing += delegate
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
            };


            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_KeyDown, (key) =>
            {
                OnKeyDown(key);
            });


            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_EditChannel, (message) =>
            {
                Xamarin.Forms.Device.BeginInvokeOnMainThread(
                    delegate
                    {
                        var ch = _viewModel.SelectedChannel;
                        if (ch != null)
                        {
                            _editChannelPage.Channel = ch;
                            Navigation.PushAsync(_editChannelPage);
                        }
                    });
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_PlayStream, (message) =>
            {
                Device.BeginInvokeOnMainThread(
                 new Action(() =>
                 {
                     if (_playerPage != null)
                     {
                         if (_playerPage.Playing)
                         {
                             _playerPage.StopPlay();
                             _playerPage.StartPlay();
                         }
                         else
                         {
                             Navigation.PushModalAsync(_playerPage);                          
                         }
                     }
                     else
                     {
                         Task.Run(async () =>
                         {
                             await _dlgService.Error("Player not initialized");
                         }
                            );
                     }
                 }));
            });


            if (_config.AutoInitAfterStart)
            {
                Task.Run( () =>
               {
                   Xamarin.Forms.Device.BeginInvokeOnMainThread(
                   new Action(
                   delegate
                   {
                       MessagingCenter.Send("", BaseViewModel.MSG_Init);
                   }));
               });
            }

            ChannelsListView.ItemSelected += ChannelsListView_ItemSelected;
        }

        public void StopPlayback()
        {
            if (_playerPage != null && _playerPage.Playing)
            {
                _playerPage.StopPlay();
                Navigation.PopModalAsync();
            }
        }

        public void OnKeyDown(string key)
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
                    Task.Run(async () => await OnKeyDown());
                    break;
                case "dpadup":
                case "buttonl1":
                case "up":
                case "w":
                    Task.Run(async () => await OnKeyUp());
                    break;
                case "dpadleft":
                case "pageup":
                case "left":
                case "a":
                    Task.Run(async () => await OnKeyLeft());
                    break;
                case "pagedown":
                case "dpadright":
                case "right":
                case "d":
                    Task.Run(async () => await OnKeyRight());
                    break;
                case "dpadcenter":
                case "space":
                case "buttonr2":
                case "mediaplaypause":
                case "enter":
                    Task.Run(async () => await _viewModel.PlayChannel());
                    break;
                case "back":
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
                case "del":
                    _viewModel.RefreshCommand.Execute(null);
                    break;
                default:
                    {
                        _loggingService.Debug($"Unbound key down: {key}");
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
                                (_numberPressed == _viewModel.SelectedChannel.Number.ToString())
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
            await _viewModel.SelectNextChannel();
        }

        private async Task OnKeyUp()
        {
            await _viewModel.SelectPreviousChannel();
        }

        private void ToolConnect_Clicked(object sender, EventArgs e)
        {
            if (!_viewModel.DriverConnected)
            {
                MessagingCenter.Send("", BaseViewModel.MSG_Init);
            } else
            {
                Task.Run( async ()=>
                {
                    await _viewModel.DisconnectDriver();                    
                }
                );
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
            _viewModel.RefreshCommand.Execute(null);
        }

        private void ChannelsListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (!_viewModel.DoNotScrollToChannel)
            {
                ChannelsListView.ScrollTo(_viewModel.SelectedChannel, ScrollToPosition.MakeVisible, false);
            }

            _viewModel.DoNotScrollToChannel = false;
        }

    }
}
