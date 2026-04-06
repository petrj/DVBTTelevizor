
using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using DVBTTelevizor.TV;
using LoggerService;
using Microsoft.Maui.Handlers;
using RTLSDR.Common;
using static System.Net.Mime.MediaTypeNames;

namespace DVBTTelevizor.MAUI;

public partial class DriverPage : ContentPage, IOnKeyDown
{
    private DriverPageViewModel _driverPageViewModel;

    private ILoggingService _loggingService;
    private ITVConfiguration _configuration;
    private IDriverConnector _driver;

    private string _publicDirectory;

    private KeyboardFocusableItemList _focusItems;
    private GainPage _gainPage;
    private DriverStatPage _statPage;

    private AppMenu _appMenu = null;

    public DriverPage(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration, IPublicDirectoryProvider publicDirectoryProvider)
    {
        InitializeComponent();

        _loggingService = loggingService;
        _configuration = tvConfiguration;
        _publicDirectory = publicDirectoryProvider.GetPublicDirectoryPath();
        _driver = driver;

        _gainPage = new GainPage(_loggingService, _driver, _configuration, publicDirectoryProvider);
        _statPage = new DriverStatPage(_loggingService, _driver, _configuration, publicDirectoryProvider);

        _appMenu = new AppMenu(MainMenu);
        _appMenu.FontSize = _configuration.AppFontSize;
        _appMenu.MenuVisibleChanged += _appMenu_MenuVisibleChanged;

        BindingContext = _driverPageViewModel = new DriverPageViewModel(loggingService, driver, tvConfiguration, publicDirectoryProvider);

        BuildFocusableItems();
    }

    public AppDriverTypeEnum? PageDriver
    {
        get
        {
            return _driverPageViewModel?.PageDriver;
        }
        set
        {
            _driverPageViewModel?.PageDriver = value;
        }
    }

    private void _appMenu_MenuVisibleChanged(object? sender, MenuVisibleChangedEventArgs e)
    {
        _driverPageViewModel.MenuVisible = e.IsVisible;
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
            await _driverPageViewModel.CheckDriver();
        });

        _focusItems.DeFocusAll();
        MainPage.SetToolBarColors(Parent as NavigationPage, Colors.White, Color.FromArgb("#29242a"));
    }

    public void OnKeyDown(string key, bool longPress)
    {
        _loggingService.Debug($"DriverPage OnKeyDown {key}");

        var keyAction = KeyboardDeterminer.GetKeyAction(key);

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

        if (_driverPageViewModel.PageDriver != _driver.DriverType)
        {
            _appMenu.ShowConfirmChangeDriverMenu(_driver, _driverPageViewModel.PageDriver);
            return;
        }

        if  (_driverPageViewModel.PageDriver == _driver.DriverType)
        {
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
    }

    private async void GainButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"DriverPage GaintButton_Clicked");

        await ShowPage(_gainPage);
    }

    private async void StatButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"DriverPage StatButton_Clicked");

        await ShowPage(_statPage);
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
            Menu_Tapped(item);
        }
    }

    private async void Menu_Tapped(MenuItem item)
    {
        var menuId = item.Id;
        _loggingService.Info($"Menu tapped: {menuId}");

        _appMenu.HideMenu();

        switch (menuId)
        {
            case "menuConfirmChangeDriver":
                WeakReferenceMessenger.Default.Send(new SendConnectDriverRequestMessage(item.DriverType));
                break;

        }
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
}