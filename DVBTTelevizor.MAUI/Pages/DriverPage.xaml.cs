
using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using DVBTTelevizor.TV;
using LoggerService;
using Microsoft.Maui.Handlers;
using static System.Net.Mime.MediaTypeNames;

namespace DVBTTelevizor.MAUI;

public partial class DriverPage : ContentPage, IOnKeyDown
{
    private DriverPageViewModel _driverPageViewModel;

    private ILoggingService _loggingService;
    private IDriverConnector _driver;
    private ITVConfiguration _configuration;

    private string _publicDirectory;

    private KeyboardFocusableItemList _focusItems;

    private AppMenu _appMenu = null;

    public DriverPage(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration, IPublicDirectoryProvider publicDirectoryProvider)
    {
        InitializeComponent();

        _loggingService = loggingService;
        _driver = driver;
        _configuration = tvConfiguration;
        _publicDirectory = publicDirectoryProvider.GetPublicDirectoryPath();

        _appMenu = new AppMenu(MainMenu);
        _appMenu.FontSize = _configuration.AppFontSize;
        _appMenu.MenuVisibleChanged += _appMenu_MenuVisibleChanged;

        FMDriverRadioButton.CheckedChanged += FMDriverRadioButton_CheckedChanged;
        DVBTDriverRadioButton.CheckedChanged += DVBTDriverRadioButton_CheckedChanged;

        BindingContext = _driverPageViewModel = new DriverPageViewModel(loggingService, driver, tvConfiguration, publicDirectoryProvider);

        WeakReferenceMessenger.Default.Register<DriverChangedMessage>(this, (r, m) =>
        {
            _driver = m.Value;
            _driverPageViewModel.UpdateActiveDriverType();
        });

        BuildFocusableItems();
    }

    private void DriverRadioButtonCheckedChanged(bool value, DriverTypeEnum driverType)
    {
        if (!value)
            return;

        if (_driverPageViewModel.IgnoreDriver == driverType)
        {
            _driverPageViewModel.IgnoreDriver = null;
            return;
        }

        Task.Run(async () =>
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                _appMenu.ShowConfirmChangeDriverMenu(_driver, _configuration.DVBTDriverType, driverType);

            });
        });
    }

    private void DVBTDriverRadioButton_CheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        DriverRadioButtonCheckedChanged(e.Value, DriverTypeEnum.AndroidDVBTDriver);
    }

    private void FMDriverRadioButton_CheckedChanged(object? sender, CheckedChangedEventArgs e)
    {
        DriverRadioButtonCheckedChanged(e.Value, DriverTypeEnum.RTLSDRDriver);
    }

    private void _appMenu_MenuVisibleChanged(object? sender, MenuVisibleChangedEventArgs e)
    {
        _driverPageViewModel.MenuVisible = e.IsVisible;
    }
    private void BuildFocusableItems()
    {
        _focusItems = new KeyboardFocusableItemList();
        _focusItems
            .AddItem(KeyboardFocusableItem.CreateFrom("DVBTDriver", new List<View>() { DVBTDriverRadioButton }))
            .AddItem(KeyboardFocusableItem.CreateFrom("FMDriver", new List<View>() { FMDriverRadioButton }))
            .AddItem(KeyboardFocusableItem.CreateFrom("Install", new List<View>() { InstallDriverButton }))
            .AddItem(KeyboardFocusableItem.CreateFrom("Preferences", new List<View>() { DriverPreferencesButton }))
            .AddItem(KeyboardFocusableItem.CreateFrom("Connect", new List<View>() { ConnectButton }))
            .AddItem(KeyboardFocusableItem.CreateFrom("DisConnect", new List<View>() { DisconnectButton }));

        //_focusItems.OnItemFocusedEvent += Page_OnItemFocusedEvent;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        Task.Run(async () =>
        {
            _driverPageViewModel.UpdateActiveDriverType();
            await _driverPageViewModel.CheckDriver();
            _driverPageViewModel.NotifyChange();
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

                    case "DVBTDriver":
                        DVBTDriverRadioButton.IsChecked = !DVBTDriverRadioButton.IsChecked;
                        break;
                    case "FMDriver":
                        FMDriverRadioButton.IsChecked = !FMDriverRadioButton.IsChecked;
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
            if (_driverPageViewModel.DVBTDriverActive)
            {
                await Browser.OpenAsync("https://play.google.com/store/apps/details?id=info.martinmarinov.dvbdriver", BrowserLaunchMode.External);
            }
            else
            {
                await Browser.OpenAsync("https://play.google.com/store/apps/details?id=marto.rtl_tcp_andro", BrowserLaunchMode.External);
            }
        });
    }

    private void ConnectButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"DriverPage ConnectButton_Clicked");

        WeakReferenceMessenger.Default.Send(new ConnectMessage(String.Empty));
    }

    private void DisconnectButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"DriverPage DisconnectButton_Clicked");

        WeakReferenceMessenger.Default.Send(new DisConnectMessage(String.Empty));
    }

    private void DriverPreferencesButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"DriverPage DriverPreferencesButton_Clicked");

        if (_driverPageViewModel.DVBTDriverActive)
        {
            WeakReferenceMessenger.Default.Send(new ShowDriverPrefrencesMessage("info.martinmarinov.dvbdriver"));
        }
        else
        {
            WeakReferenceMessenger.Default.Send(new ShowDriverPrefrencesMessage("marto.rtl_tcp_andro"));
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
            case "menuChangeDriver":
                await _driverPageViewModel.ChangeDriver(item.DriverType);
                break;
            case "menuCancelChangeDriver":
                _driverPageViewModel.UpdateActiveDriverType();
                break;
            case "menuConnectDriver":
                ConnectButton_Clicked(this, null);
                break;
        }
    }
}