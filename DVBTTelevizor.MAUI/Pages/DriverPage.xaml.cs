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
    private bool _menuEnabled = true;

    private string _publicDirectory;

    private KeyboardFocusableItemList _focusItems;
    private List<MenuItem> _menuItems = new List<MenuItem>();

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

        DriverPicker.SelectedIndexChanged += DriverPicker_SelectedIndexChanged;

        BindingContext = _driverPageViewModel = new DriverPageViewModel(loggingService, driver, tvConfiguration, publicDirectoryProvider);

        BuildFocusableItems();
    }

    private void BuildChaneDriverMenu()
    {
        ShowOrHideMenu();

        if (MainMenu.IsVisible)
        {
            _menuItems.Clear();

            _menuItems.Add(MainMenu.CreateMenuItem("menuChangeDriver", "Change driver".Translated(), "refresh.png"));
            _menuItems.Add(MainMenu.CreateMenuItem("menuCancel", "Cancel", "cancel.png"));

            MainMenu.UpdateMenu((int)_configuration.AppFontSize,
                "Switch driver confirmation".Translated(), _menuItems);
        }
    }


    private void DriverPicker_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (!_menuEnabled)
            return;

        BuildChaneDriverMenu();

        return;

        if (_driverPageViewModel.DriverTypeIndex == 0 && (!(_driver is DVBTDriverConnector)))
        {
            // switch driver to DVBTDriverConnector

            WeakReferenceMessenger.Default.Send(new DVBTDriverChangedMessage(String.Empty));
            //WeakReferenceMessenger.Default.Send(new ConnectMessage(String.Empty));
        }

        if (_driverPageViewModel.DriverTypeIndex == 1 && (!(_driver is RTLSDRDriverConnector)))
        {
            // switch driver RTLSDRTCPIPFMDriverConnector

            WeakReferenceMessenger.Default.Send(new DVBTDriverChangedMessage(String.Empty));
            //WeakReferenceMessenger.Default.Send(new ConnectMessage(String.Empty));
        }

        Task.Run(async () => await _driverPageViewModel.CheckDriver());
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
            _menuEnabled = false;
            await _driverPageViewModel.FillDrivers();
            await _driverPageViewModel.CheckDriver();
            _menuEnabled = true;
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


    private void ShowOrHideMenu()
    {
        if (MainMenu.MenuVisible)
        {
            HideMenu();
        }
        else
        {
            ShowMenu();
        }
    }

    private void ShowMenu()
    {
        MainMenu.MenuVisible = true;
        _driverPageViewModel.MenuVisible = true;
    }

    private void HideMenu()
    {
        MainMenu.MenuVisible = false;
        _driverPageViewModel.MenuVisible = false;
    }


    private async void Menu_Tapped(string menuId)
    {
        _loggingService.Info($"Menu tapped: {menuId}");

        HideMenu();

        switch (menuId)
        {
            /*
            case "menuFromBeginning":
                _viewModel.ResetTune(true);
                await _viewModel.StartTune();
                break;

            case "menuContinue":
            case "menuRetryTune":
                await _viewModel.StartTune();
                break;

            case "menuDriver":
                var driverPage = new DriverPage(_loggingService, _driver, _configuration, _publicDirectoryProvider);
                await Navigation.PushAsync(driverPage);
                break;
            */
        }
    }
}