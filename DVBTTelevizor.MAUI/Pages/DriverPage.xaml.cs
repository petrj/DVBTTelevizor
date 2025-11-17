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
    private int? _ignoreDriverChangeEvent = null;
    private int? _changeToDriverIndex = null;

    private string _publicDirectory;

    private KeyboardFocusableItemList _focusItems;

    private AppMenu _appMenu = null;

    public DriverPage(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration, IPublicDirectoryProvider publicDirectoryProvider)
    {
        InitializeComponent();

//#if ANDROID
//        DriverPicker.Loaded += (s, e) =>
//        {
//            var handler = (PickerHandler)DriverPicker.Handler;
//            handler.PlatformView.Background = new ColorDrawable(Android.Graphics.Color.Transparent);
//        };
//#endif

        _loggingService = loggingService;
        _driver = driver;
        _configuration = tvConfiguration;
        _publicDirectory = publicDirectoryProvider.GetPublicDirectoryPath();

        _appMenu = new AppMenu(MainMenu);
        _appMenu.FontSize = _configuration.AppFontSize;
        _appMenu.MenuVisibleChanged += _appMenu_MenuVisibleChanged;

        DriverPicker.SelectedIndexChanged += DriverPicker_SelectedIndexChanged;

        BindingContext = _driverPageViewModel = new DriverPageViewModel(loggingService, driver, tvConfiguration, publicDirectoryProvider);

        BuildFocusableItems();
    }

    private void _appMenu_MenuVisibleChanged(object? sender, MenuVisibleChangedEventArgs e)
    {
        _driverPageViewModel.MenuVisible = e.IsVisible;
    }

    private void DriverPicker_SelectedIndexChanged(object? sender, EventArgs e)
    {

        if (_ignoreDriverChangeEvent.HasValue && _driverPageViewModel.DriverTypeIndex == _ignoreDriverChangeEvent)
        {
            _ignoreDriverChangeEvent = null;
            return;
        }

        try
        {
            if (_driver != null && _driver.Connected)
            {
                // reverse the change and show menu
                _changeToDriverIndex = _driverPageViewModel.DriverTypeIndex;
                _driverPageViewModel.DriverTypeIndex = _driverPageViewModel.PreviousSelectedDriverTypeIndex;

                // next driver change to DriverTypeIndex must be ignored now!
                _ignoreDriverChangeEvent = _driverPageViewModel.DriverTypeIndex;

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    _appMenu.BuildChangeDriverMenu(_driver, _driverPageViewModel.SelectedDriverType, _driverPageViewModel.PreviousSelectedDriverTypeIndex);
                });
            }
            else
            {
                Task.Run(async () =>
                {
                    await _driverPageViewModel.ReConnectDriver();
                });
            }
        } finally
        {

        }
    }

    private void BuildFocusableItems()
    {
        _focusItems = new KeyboardFocusableItemList();
        _focusItems
            .AddItem(KeyboardFocusableItem.CreateFrom("Driver", new List<View>() { DriverTypeBoxView, DriverPicker }))
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
            _ignoreDriverChangeEvent = _driverPageViewModel.DriverTypeIndex;
            await _driverPageViewModel.FillDrivers();
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

                    case "Driver":
                        MainThread.BeginInvokeOnMainThread(async () =>
                        {
                            DriverPicker.Focus();
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

    private async void InstallDriverButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"DriverPage InstallDriverButton_Clicked");

        await Browser.OpenAsync("https://play.google.com/store/apps/details?id=info.martinmarinov.dvbdriver", BrowserLaunchMode.External);
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

        WeakReferenceMessenger.Default.Send(new ShowDriverPrefrencesMessage(String.Empty));
    }


    private void Menu_Tapped(object sender, EventArgs e)
    {
        if (e != null && e is TappedEventArgs tea)
        {
            Menu_Tapped(tea.Parameter.ToString());
        }
    }

    private async Task ChangeDriver()
    {
        _loggingService.Info($"ChangeDriver");

        if (!_changeToDriverIndex.HasValue)
            return;

        if (_driver != null)
        {
            if (_driver.Connected)
            {
                await _driver.Stop();
                await _driver.Disconnect();
            }
        }

        _ignoreDriverChangeEvent = _changeToDriverIndex.Value;
        _driverPageViewModel.DriverTypeIndex = _changeToDriverIndex.Value;

        _changeToDriverIndex = null;

        await _driverPageViewModel.ReConnectDriver();
    }

    private async void Menu_Tapped(string menuId)
    {
        _loggingService.Info($"Menu tapped: {menuId}");

        _appMenu.HideMenu();

        switch (menuId)
        {
            case "menuChangeDriver":
                await ChangeDriver();
                break;
        }
    }
}