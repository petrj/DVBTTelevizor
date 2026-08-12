using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using DVBTTelevizor.TV;
using LoggerService;

namespace DVBTTelevizor.MAUI;

public partial class DriverPage : ContentPage, IOnKeyDown
{
    private DriverPageViewModel _driverPageViewModel;

    private ILoggingService _loggingService;
    private ITVConfiguration _configuration;

    private KeyboardFocusableItemList _focusItems;
    private IDriverConnector _driver;
    private IPublicDirectoryProvider _publicDirectoryProvider;

    private AppMenu _appMenu = null;

    public DriverPage(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration, IPublicDirectoryProvider publicDirectoryProvider)
    {
        InitializeComponent();

        _driver = driver;
        _loggingService = loggingService;
        _configuration = tvConfiguration;
        _publicDirectoryProvider = publicDirectoryProvider;

        _appMenu = new AppMenu(MainMenu);
        _appMenu.FontSize = _configuration.AppFontSize;
        _appMenu.MenuVisibleChanged += _appMenu_MenuVisibleChanged;

        BindingContext = _driverPageViewModel = new DriverPageViewModel(loggingService, driver, tvConfiguration, publicDirectoryProvider);

        BuildFocusableItems();
    }

    public AppDriverTypeEnum PageDriver
    {
        get
        {
            return _driverPageViewModel.PageDriver;
        }
        set
        {
            _driverPageViewModel?.PageDriver = value;
        }
    }

    private void _appMenu_MenuVisibleChanged(object? sender, MenuVisibleChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            _driverPageViewModel.MenuVisible = e.IsVisible;
        });
    }

    private void BuildFocusableItems()
    {
        _focusItems = new KeyboardFocusableItemList();
        _focusItems
            .AddItem(KeyboardFocusableItem.CreateFrom("Install", new List<View>() { InstallDriverButton }))
            .AddItem(KeyboardFocusableItem.CreateFrom("Preferences", new List<View>() { DriverPreferencesButton }))
            .AddItem(KeyboardFocusableItem.CreateFrom("Connect", new List<View>() { ConnectButton }))
            .AddItem(KeyboardFocusableItem.CreateFrom("Gain", new List<View>() { GainButton }))
            .AddItem(KeyboardFocusableItem.CreateFrom("Stat", new List<View>() { StatButton }))
            .AddItem(KeyboardFocusableItem.CreateFrom("DisConnect", new List<View>() { DisconnectButton }));

        //_focusItems.OnItemFocusedEvent += Page_OnItemFocusedEvent;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        Task.Run(async () =>
        {
            WeakReferenceMessenger.Default.Send(new CheckDriversRequestMessage(null));
        });

        _focusItems.DeFocusAll();
        MainPage.SetToolBarColors(Parent as NavigationPage, Colors.White, Color.FromArgb("#29242a"));
    }

    public async void OnKeyDown(string key, bool longPress)
    {
        _loggingService.Debug($"DriverPage OnKeyDown {key}");

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
            case KeyboardNavigationActionEnum.Down:
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    _focusItems.FocusNextItem(true);
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
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Navigation.PopAsync();
                });
                break;

            case KeyboardNavigationActionEnum.OK:

                switch (_focusItems.FocusedItemName)
                {
                    case "Install":
                        MainThread.BeginInvokeOnMainThread(async () =>
                        {
                            InstallDriverButton_Clicked(this, new EventArgs());
                        });
                        break;
                    case "Connect":
                        MainThread.BeginInvokeOnMainThread(async () =>
                        {
                            ConnectButton_Clicked(this, new EventArgs());
                        });
                        break;
                    case "Preferences":
                        MainThread.BeginInvokeOnMainThread(async () =>
                        {
                            DriverPreferencesButton_Clicked(this, new EventArgs());
                        });
                        break;

                    case "DisConnect":
                        MainThread.BeginInvokeOnMainThread(async () =>
                        {
                            DisconnectButton_Clicked(this, new EventArgs());
                        });
                        break;

                    case "Stat":
                        MainThread.BeginInvokeOnMainThread(async () =>
                        {
                            StatButton_Clicked(this, new EventArgs());
                        });
                        break;
                    case "Gain":
                        MainThread.BeginInvokeOnMainThread(async () =>
                        {
                            GainButton_Clicked(this, new EventArgs());
                        });
                        break;
                }

                break;
        }
    }

    public void OnTextSent(string text)
    {
        _loggingService.Debug($"DriverPage OnTextSent {text}");
    }

    private void InstallDriverButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"DriverPage InstallDriverButton_Clicked");

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            switch (_driverPageViewModel.PageDriver)
            {
                case AppDriverTypeEnum.DVBT:
                        await Browser.OpenAsync("https://play.google.com/store/apps/details?id=info.martinmarinov.dvbdriver", BrowserLaunchMode.External);
                        break;
                    case AppDriverTypeEnum.FM:
                    case AppDriverTypeEnum.DAB:
                    await Browser.OpenAsync("https://play.google.com/store/apps/details?id=marto.rtl_tcp_andro", BrowserLaunchMode.External);
                        break;
            }
        });
    }

    private void ConnectButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"DriverPage ConnectButton_Clicked");

        if (_driverPageViewModel.PageDriver != _driverPageViewModel.Driver.DriverType)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                _appMenu.ShowConfirmChangeDriverMenu(_driverPageViewModel.Driver, _driverPageViewModel.PageDriver);
            });
            return;
        }

        if  (_driverPageViewModel.PageDriver == _driverPageViewModel.Driver.DriverType)
        {
            _driverPageViewModel.IsConnecting = true; // DVBT driver connection can lead to non-updating the state in GUI

            switch (_driverPageViewModel.PageDriver)
            {
                case AppDriverTypeEnum.DVBT:
                    WeakReferenceMessenger.Default.Send(new SendConnectDriverRequestMessage(AppDriverTypeEnum.DVBT));
                    break;
                case AppDriverTypeEnum.DAB:
                    WeakReferenceMessenger.Default.Send(new SendConnectDriverRequestMessage(AppDriverTypeEnum.DAB));
                    break;
                case AppDriverTypeEnum.FM:
                    WeakReferenceMessenger.Default.Send(new SendConnectDriverRequestMessage(AppDriverTypeEnum.FM));
                    break;
            }
        }
    }

    private void DisconnectButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"DriverPage DisconnectButton_Clicked");

        WeakReferenceMessenger.Default.Send(new DisConnectMessage(String.Empty));

        _driverPageViewModel.IsDisConnecting = true; // DVBT driver connection can lead to non-updating the state in GUI
    }

    private async void GainButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"DriverPage GaintButton_Clicked");

        await ShowPage<GainPage>();
    }

    private async void StatButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"DriverPage StatButton_Clicked");

        await ShowPage<DriverStatPage>();
    }

    public async Task ShowPage<T>() where T : Page
    {
        await MainPage.ShowPage<T>(Navigation, _loggingService, _driver, null, _configuration, _publicDirectoryProvider);
    }

    private void DriverPreferencesButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"DriverPage DriverPreferencesButton_Clicked");

        switch (_driverPageViewModel.PageDriver)
        {
            case AppDriverTypeEnum.DVBT:
                    WeakReferenceMessenger.Default.Send(new ShowDriverPrefrencesMessage("info.martinmarinov.dvbdriver"));
                 break;
            case AppDriverTypeEnum.DAB:
            case AppDriverTypeEnum.FM:
                    WeakReferenceMessenger.Default.Send(new ShowDriverPrefrencesMessage("marto.rtl_tcp_andro"));
                 break;
        }
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

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            _appMenu.HideMenu();
        });

        switch (menuId)
        {
            case "menuConfirmChangeDriver":
                WeakReferenceMessenger.Default.Send(new SendConnectDriverRequestMessage(item.DriverType));
                break;

        }
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
                _driverPageViewModel.MenuVisible = false;
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

}