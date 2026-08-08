using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using DVBTTelevizor.TV;
using LoggerService;
using Microsoft.Maui.Layouts;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using static DVBTTelevizor.MAUI.TuningProgressPageViewModel;
using static System.Net.Mime.MediaTypeNames;

namespace DVBTTelevizor.MAUI;

public partial class TuningProgressPage : ContentPage, ITuningPage, IOnKeyDown
{
    private TuningProgressPageViewModel _viewModel;

    public bool Finished { get; set; } = false;

    private Size _lastAllocatedSize = new Size(-1, -1);
    private bool _isPortrait { get; set; } = false;
    private bool? _isPortraitPreviousValue { get; set; } = null;

    private List<MenuItem> _menuItems = new List<MenuItem>();

    private ILoggingService _loggingService;
    private ITVConfiguration _configuration;
    private string _publicDirectory = "";
    private IPublicDirectoryProvider _publicDirectoryProvider;

    private KeyboardFocusableItemList _focusItems;

    private Command _commandUpdateBitrate;
    private DriverPage _driverPage;

    private AppMenu _appMenu = null;

    private DateTime _lastSliderValuechangedActionTime = DateTime.MinValue;
    private long _sliderFreqKHz = 0;
    private ConcurrentQueue<long> _sliderFreqKHzQueue = new ConcurrentQueue<long>();

    public TuningProgressPage(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration, IPublicDirectoryProvider publicDirectoryProvider)
    {
        InitializeComponent();

        _loggingService = loggingService;
        _configuration = tvConfiguration;
        _publicDirectoryProvider = publicDirectoryProvider;
        _publicDirectory = publicDirectoryProvider.GetPublicDirectoryPath();

        BindingContext = _viewModel = new TuningProgressPageViewModel(loggingService, driver, tvConfiguration, publicDirectoryProvider);

        _driverPage = new DriverPage(_loggingService, driver, _configuration, publicDirectoryProvider);

        _appMenu = new AppMenu(MainMenu);
        _appMenu.FontSize = _configuration.AppFontSize;
        _appMenu.MenuVisibleChanged += _appMenuVisibleChanged;

        BuildFocusableItems();

        _viewModel.ChannelFound += ChannelFound;

        WeakReferenceMessenger.Default.Register<TuneFailedMessage>(this, (r, m) =>
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                //BuildConfirmMenu("Tuning failed. Check USB connection".Translated(), "Retry".Translated(), "Cancel".Translated(), "menuRetryTune", "menuCancel");
                //BuildRetryTuneMenu();
                _appMenu.ShowRetryTuneMenu(_viewModel.Driver);
            });
        });

        WeakReferenceMessenger.Default.Register<StartTuneMessage>(this, (r, m) =>
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                StartButton_Clicked(this, new EventArgs());
            });
        });


        WeakReferenceMessenger.Default.Register<ShowTuningProgressDriverPageMessage>(this, (r, m) =>
        {
            DriverButton_Clicked(this, new EventArgs());
        });

        _commandUpdateBitrate = new Command(() =>
        {
            Task.Run(async () =>
            {
                await _viewModel.NotifyBitrateChange();
            });
        });

        Disappearing += TuningProgressPage_Disappearing;
        BackgroundCommandWorker.RunInBackground(_commandUpdateBitrate, 5);
    }

    private void _appMenuVisibleChanged(object? sender, MenuVisibleChangedEventArgs e)
    {
        _viewModel.MenuVisible = e.IsVisible;
    }

    private void TuningProgressPage_Disappearing(object? sender, EventArgs e)
    {
        //_viewModel.State = TuningProgressPageViewModel.TuneStateEnum.Inactive;
    }

    private void ChannelFound(object? sender, EventArgs e)
    {
        try
        {
            if (e is ChannelFoundEventArgs che)
            {
                //_loggingService.Info($"Adding new channel: {che.Channel.Name}");
                //MainThread.BeginInvokeOnMainThread(async () =>
                //{
                //    try
                //    {
                //        ChannelsListView.ScrollTo(che.Channel, ScrollToPosition.MakeVisible, false);
                //    }
                //    catch (Exception ex)
                //    {
                //        _loggingService.Error(ex);
                //    }
                //});
            }
        }
        catch (Exception ex)
        {
            _loggingService.Error(ex);
        }
    }

    private void BuildFocusableItems()
    {
        _focusItems = new KeyboardFocusableItemList();

        _focusItems
            .AddItem(KeyboardFocusableItem.CreateFrom("Left", new List<View>() { LeftButton }))
            .AddItem(KeyboardFocusableItem.CreateFrom("Right", new List<View>() { RightButton }))
            .AddItem(KeyboardFocusableItem.CreateFrom("Back", new List<View>() { BackButton }))
            .AddItem(KeyboardFocusableItem.CreateFrom("Start", new List<View>() { StartButton }))
            .AddItem(KeyboardFocusableItem.CreateFrom("Stop", new List<View>() { StopButton }))
            .AddItem(KeyboardFocusableItem.CreateFrom("Finish", new List<View>() { FinishButton }))
            .AddItem(KeyboardFocusableItem.CreateFrom("ChannelsList", new List<View>() { ChannelsListView }))
            .AddItem(KeyboardFocusableItem.CreateFrom("Driver", new List<View>() { DriverBoxView, DriverButton }));

        _focusItems.OnItemFocusedEvent += _focusItems_OnItemFocusedEvent;
    }

    private async void _focusItems_OnItemFocusedEvent(KeyboardFocusableItemEventArgs _args)
    {
        if (_args.FocusedItem.Name == "ChannelsList")
        {
            //await _viewModel.SelectChannelsListView(ChannelsListView);
            if (_viewModel.Channels.Count == 0)
            {
                _focusItems.FocusNextItem(true);
            } else
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    _viewModel.SelectFirstChannel();
                    ChannelsListView.ScrollTo(ChannelsListView.SelectedItem, ScrollToPosition.Center, animated: true);
                });
            }
        } else
        {
            if (_viewModel.SelectedChannel != null)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    _viewModel.DeselectAll();
                });
            }
        }
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        if (_lastAllocatedSize.Width == width &&
            _lastAllocatedSize.Height == height)
        {
            // no size changed
            return;
        }

        if (width > height)
        {
            _isPortrait = false;
        }
        else
        {
            _isPortrait = true;
        }

        _lastAllocatedSize.Width = width;
        _lastAllocatedSize.Height = height;

        if (_isPortrait != _isPortraitPreviousValue)
        {
            RefreshGUI();
        }

        _isPortraitPreviousValue = _isPortrait;
    }

    private void RefreshGUI()
    {
        if (_isPortrait)
        {
            AbsoluteLayout.SetLayoutBounds(FrequencyGrid, new Rect(0.5, 0.0, 0.95, 0.1));
            AbsoluteLayout.SetLayoutBounds(TuneIndicator, new Rect(0.5, 0.1, 0.25, 0.05));
            AbsoluteLayout.SetLayoutBounds(ProgressGrid, new Rect(0.5, 0.14, 0.95, 0.15));
            AbsoluteLayout.SetLayoutBounds(SignalDetailsGrid, new Rect(0.5, 0.32, 0.9, 0.15));
            AbsoluteLayout.SetLayoutBounds(TuneResultDetailsGrid, new Rect(0.5, 0.46, 0.9, 0.1));
            AbsoluteLayout.SetLayoutBounds(ButtonsGrid, new Rect(0.05, 0.98, 0.95, 0.15));
            AbsoluteLayout.SetLayoutBounds(ChannelsSplitterGrid, new Rect(0.5, 0.8, 0.95, 0.325));
            AbsoluteLayout.SetLayoutBounds(ChannelsListView, new Rect(0.5, 0.76, 0.95, 0.28));
        } else
        {
            AbsoluteLayout.SetLayoutBounds(FrequencyGrid, new Rect(0.05, 0.00, 0.45, 0.15));
            AbsoluteLayout.SetLayoutBounds(TuneIndicator, new Rect(0.125, 0.15, 0.25, 0.05));
            AbsoluteLayout.SetLayoutBounds(ProgressGrid, new Rect(0.05, 0.25, 0.45, 0.15));
            AbsoluteLayout.SetLayoutBounds(SignalDetailsGrid, new Rect(0.05, 0.5, 0.45, 0.25));
            AbsoluteLayout.SetLayoutBounds(TuneResultDetailsGrid, new Rect(0.05, 0.75, 0.45, 0.15));
            AbsoluteLayout.SetLayoutBounds(ButtonsGrid, new Rect(0.05, 0.95, 0.45, 0.15));
            AbsoluteLayout.SetLayoutBounds(ChannelsSplitterGrid, new Rect(0.5, -0.5, 0.0, 0.0));
            AbsoluteLayout.SetLayoutBounds(ChannelsListView, new Rect(1.0, 0.5, 0.5, 1));
        }
    }

    private void ContentPage_Loaded(object sender, EventArgs e)
    {
        //StartButton_Clicked(this, new EventArgs());
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        Title = "Tuning".Translated();

        _focusItems.DeFocusAll();
        MainPage.SetToolBarColors(Parent as NavigationPage, Colors.White, Color.FromArgb("#29242a"));

        Task.Run(async () =>
        {
            await ResetTuningEnvironment();

            await Task.Delay(200); // Allow UI to render first
            StartButton_Clicked(this, new EventArgs());

           await _viewModel.NotifyChange();
        });
    }

    private void ActionDown()
    {
        if (_focusItems.FocusedItemName == "ChannelsList")
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                _viewModel.SelectNextChannel();
            });
        } else
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                _focusItems.FocusNextItem(true);
            });
        }
    }

    private void ActionUp()
    {
        if (_focusItems.FocusedItemName == "ChannelsList")
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                _viewModel.SelectPreviousChannel();
            });
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                _focusItems.FocusPreviousItem(true);
            });
        }
    }

    private void ActionRight()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            _focusItems.FocusNextItem(true);
        });
    }

    private void ActionLeft()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            _focusItems.FocusPreviousItem(true);
        });
    }

    private void OnMenuKeyDown(KeyboardNavigationActionEnum keyAction)
    {
        var menuItems = _appMenu.MenuItems;

        switch (keyAction)
        {
            case KeyboardNavigationActionEnum.Right:
            case KeyboardNavigationActionEnum.Down:
                Task.Run(async () =>
                {
                    await MainMenu.SelectNextMenuItem(menuItems, false);
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
                MainMenu.MenuVisible = false;
                _viewModel.MenuVisible = false;
                break;

            case KeyboardNavigationActionEnum.OK:
                var item = GetSelectedMenuItem();
                if (item != null)
                {
                    OnMenuIsTapped(item);
                }
                break;
        }
    }

    private MenuItem? GetSelectedMenuItem()
    {
        var menuItems = _appMenu.MenuItems;

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

    public async void OnKeyDown(string key, bool longPress)
    {
        _loggingService.Debug($"TuningProgressPage Page OnKeyDown {key}");

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
            case KeyboardNavigationActionEnum.Right:
                ActionRight();
                break;

            case KeyboardNavigationActionEnum.Down:
                ActionDown();
                break;

            case KeyboardNavigationActionEnum.Up:
                ActionUp();
                break;

            case KeyboardNavigationActionEnum.Left:
                ActionLeft();
                break;

            case KeyboardNavigationActionEnum.Back:
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Navigation.PopAsync();
                });
                break;

            case KeyboardNavigationActionEnum.OK:

                if (_focusItems.FocusedItem == null)
                    return;

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    switch (_focusItems.FocusedItem.Name)
                    {
                        case "Left":
                            LeftButton_Clicked(this, new EventArgs());
                            break;
                        case "Right":
                            RightButton_Clicked(this, new EventArgs());
                            break;
                        case "Back":
                            BackButton_Clicked(this, new EventArgs());
                            break;
                        case "Stop":
                            StopButton_Clicked(this,new EventArgs());
                            break;
                        case "Start":
                            StartButton_Clicked(this, new EventArgs());
                            break;
                        case "Finish":
                            FinishButton_Clicked(this, new EventArgs());
                            break;
                        case "Driver":
                            DriverButton_Clicked(this, new EventArgs());
                            break;
                    }
                });
                break;
        }
    }

    public void OnTextSent(string text)
    {
        _loggingService.Debug($"TuningProgressPage OnTextSent {text}");
    }

    public void ResetTune(bool clearChannels)
    {
        _viewModel?.ResetTune(clearChannels, true);
    }

    private async void StartButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"TuningProgressPage StartButton_Clicked");

        if (_viewModel.Settings.TuningMode != TuneModeEnum.Frequency &&
            _viewModel.FrequencyKHz > _viewModel.FrequencyFromKHz &&
            _viewModel.FrequencyKHz < _viewModel.FrequencyToKHz)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                _appMenu.ShowConfirmMenu(
                "Confirm".Translated(),
                "Start from beginning".Translated(),
                "Continue".Translated(),
                "menuFromBeginning",
                "menuContinue");
            });

            return;
        }

        if (await ResetTuningEnvironment())
        {
            _viewModel.ResetTune(true, true);
            await _viewModel.StartTune();
        }
    }


    private async Task<bool> ResetTuningEnvironment()
    {
        _loggingService.Debug($"ResetTuningEnvironment");

        if ((_viewModel.Driver == null))
        {
            _loggingService.Error("StartButton_Clicked - no driver");
            WeakReferenceMessenger.Default.Send(new ToastMessage("Error - no driver".Translated()));
            return false;
        }

        AppDriverTypeEnum? driverToChange = null;
        if ((_viewModel.Settings.FM) && (_viewModel.Driver.DriverType != TV.AppDriverTypeEnum.FM))
        {
            // need to change driver
            driverToChange = AppDriverTypeEnum.FM;
        }
        if ((_viewModel.Settings.DAB) && (_viewModel.Driver.DriverType != TV.AppDriverTypeEnum.DAB))
        {
            // need to change driver
            driverToChange = AppDriverTypeEnum.DAB;
        }
        if ((_viewModel.Settings.DVBT || _viewModel.Settings.DVBT2) && (_viewModel.Driver.DriverType != TV.AppDriverTypeEnum.DVBT))
        {
            // need to change driver
            driverToChange = AppDriverTypeEnum.DVBT;
        }

        if (driverToChange != null)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                _appMenu.ShowConfirmChangeDriverMenu(_viewModel.Driver, driverToChange);
            });

            return false;
        }

        if (!_viewModel.Driver.Connected)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                _appMenu.ShowConnectDriverMenu(_viewModel.Driver);
            });

            return false;
        }

        return true;
    }

    private void StopButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"TuningProgressPage StopButton_Clicked");

        _viewModel.StopTune();
    }

    private void BackButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"TuningProgressPage BackButton_Clicked");

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (_viewModel.State == TuneStateEnum.InProgress)
            {
                _viewModel.State = TuneStateEnum.Stopped;
            }

            await Navigation.PopAsync();
        });
    }


    private async void DriverButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"TuningProgressPage DriverButton_Clicked");

        _driverPage.PageDriver = _viewModel.Driver.DriverType;

        await ShowPage(_driverPage);
    }

    private async Task ShowPage(ContentPage page)
    {
        if (page.IsLoaded)
        {
            // preventing click when the settings page is just (or yet) loaded
            return;
        }

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await Navigation.PushAsync(page);
        });
    }

    private async void FinishButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"TuningProgressPage FinishButton_Clicked");

        _viewModel.ResetTune(true, true);

        _viewModel.State = TuneStateEnum.Finished;

        _viewModel.NotifyChange();

        WeakReferenceMessenger.Default.Send(new FinishTuningMessage(String.Empty));
    }

    public void UpdateSettings(TuningSettings tuningSettings)
    {
        _viewModel.Settings = tuningSettings;
        _viewModel.UpdateActualFreq();
    }

    private void Menu_Tapped(object sender, EventArgs e)
    {
        if (e != null &&
            e is TappedEventArgs tea &&
            tea.Parameter is MenuItem item)
        {
            OnMenuIsTapped(item);
        }
    }

    private async void OnMenuIsTapped(MenuItem item)
    {
        var menuId = item.Id;
        _loggingService.Info($"Menu tapped: {menuId}");

        _appMenu.HideMenu();

        switch (menuId)
        {
            case "menuFromBeginning":
                _viewModel.ResetTune(true, true);
                await _viewModel.StartTune();
                break;

            case "menuContinue":
            case "menuRetryTune":
                _viewModel.ResetTune(false, false);
                await _viewModel.StartTune();
                break;

            case "menuDriver":
                var driverPage = new DriverPage(_loggingService, _viewModel.Driver, _configuration, _publicDirectoryProvider);
                await Navigation.PushAsync(driverPage);
                break;

            case "menuInstallDriver":

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    switch (_viewModel.Driver.DriverType)
                    {
                        case TV.AppDriverTypeEnum.DVBT:
                            await Browser.OpenAsync("https://play.google.com/store/apps/details?id=info.martinmarinov.dvbdriver", BrowserLaunchMode.External);
                            break;
                        case TV.AppDriverTypeEnum.FM:
                        case TV.AppDriverTypeEnum.DAB:
                            await Browser.OpenAsync("https://play.google.com/store/apps/details?id=marto.rtl_tcp_andro", BrowserLaunchMode.External);
                            break;
                    }
                });
                break;

            case "menuConfirmConnectDriver":
            case "menuConfirmChangeDriver":
                WeakReferenceMessenger.Default.Send(new SendConnectDriverRequestMessage(item.DriverType));
                break;
        }
    }

    private void SliderFrequency_ValueChanged(object sender, ValueChangedEventArgs e)
    {
        _loggingService.Debug($"TuningProgressPage SliderFrequency_ValueChanged");

        _sliderFreqKHzQueue.Enqueue((long)SliderFrequency.Value);

        Task.Run(async () =>
        {
            await Task.Delay(100);

            bool found = false;
            long value = 0;
            while (_sliderFreqKHzQueue.Count > 0)
            {
                found = true;
                _sliderFreqKHzQueue.TryDequeue(out value);
            }

            if (found)
            {
                _viewModel.FrequencyKHz = _viewModel.Settings.RoundFrequencyKHz(value);
                await _viewModel.TuneFreq(_viewModel.FrequencyKHz * 1000, _viewModel.Settings.BandwidthKHz * 1000, _viewModel.Settings.DVBT2 ? 1 : 0);

                // save to configuration
                switch (_viewModel.Config.AppDriverType)
                {
                    case TV.AppDriverTypeEnum.FM:
                        _viewModel.Config.FMFrequencyKHz = _viewModel.FrequencyKHz;
                        break;
                    case TV.AppDriverTypeEnum.DAB:
                        _viewModel.Config.DABFrequencyKHz = _viewModel.FrequencyKHz;
                        break;
                    case AppDriverTypeEnum.DVBT:
                        _viewModel.Config.FrequencyKHz = _viewModel.FrequencyKHz;
                        break;
                }
            }
        });
    }

    private void LeftButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"FrequencyPage LeftButton_Clicked");

        _viewModel.DecreaseFreq();
    }

    private void RightButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"FrequencyPage RightButton_Clicked");

        _viewModel.IncreaseFreq();
    }
}