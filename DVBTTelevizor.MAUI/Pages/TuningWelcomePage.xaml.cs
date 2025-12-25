
using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using LoggerService;
using static System.Net.Mime.MediaTypeNames;

namespace DVBTTelevizor.MAUI;

public partial class TuningWelcomePage : ContentPage, IOnKeyDown
{
    private TuningWelcomePageViewModel _viewModel;

    private ILoggingService _loggingService;
    private IDriverConnector _driver;
    private ITVConfiguration _configuration;
    private string _publicDirectory = "";


    private TuningSettings _tuningSettings;

    private KeyboardFocusableItemList _focusItems;

    private AppMenu _appMenu = null;

    private TuningSelectDVBTPage _selectDVBTPage;
    private TuningProgressPage _tuningProgressPage;
    private TuningFrequencyPage _tuningFrequencyPage;
    private TuningFrequenciesPage _tuningFrequenciesPage;

    private DriverTypeEnum? _prevDriverType;

    public TuningWelcomePage(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration,  IPublicDirectoryProvider publicDirectoryProvider)
    {
        InitializeComponent();

        _loggingService = loggingService;
        _driver = driver;
        _configuration = tvConfiguration;
        _publicDirectory = publicDirectoryProvider.GetPublicDirectoryPath();

        _tuningSettings = new TuningSettings(_loggingService);

        _appMenu = new AppMenu(MainMenu);
        _appMenu.FontSize = _configuration.AppFontSize;
        _appMenu.MenuVisibleChanged += _appMenu_MenuVisibleChanged; ;

        BindingContext = _viewModel = new TuningWelcomePageViewModel(loggingService, driver, tvConfiguration, publicDirectoryProvider);

        _selectDVBTPage = new TuningSelectDVBTPage(loggingService, driver, tvConfiguration, publicDirectoryProvider);
        _tuningProgressPage = new TuningProgressPage(loggingService, driver, tvConfiguration, publicDirectoryProvider);
        _tuningFrequencyPage = new TuningFrequencyPage(loggingService, driver, tvConfiguration, publicDirectoryProvider);
        _tuningFrequenciesPage = new TuningFrequenciesPage(loggingService, driver, tvConfiguration, publicDirectoryProvider);

        FMDriverRadioButton.CheckedChanged += FMDriverRadioButton_CheckedChanged;
        DVBTDriverRadioButton.CheckedChanged += DVBTDriverRadioButton_CheckedChanged;

        WeakReferenceMessenger.Default.Register<DriverChangedMessage>(this, (r, m) =>
        {
            _driver = m.Value;

            _viewModel.UpdateActiveDriverType();
            _tuningSettings.LoadFromConfiguration(_configuration);
            _tuningSettings.SetFrequencies(_driver);
        });

        BuildFocusableItems();
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
                await _viewModel.ChangeDriver(item.DriverType);
                break;
            case "menuCancelChangeDriver":
                _viewModel.UpdateActiveDriverType();
                break;
        }
    }

    private void _appMenu_MenuVisibleChanged(object? sender, MenuVisibleChangedEventArgs e)
    {
        _viewModel.MenuVisible = e.IsVisible;
    }

    private void DriverRadioButtonCheckedChanged(bool value, DriverTypeEnum driverType)
    {
        if (!value)
            return;

        if (_viewModel.IgnoreDriver == driverType)
        {
            _viewModel.IgnoreDriver = null;
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

    private void BuildFocusableItems()
    {
        _focusItems = new KeyboardFocusableItemList();

        _focusItems
            .AddItem(KeyboardFocusableItem.CreateFrom("DVBTDriver", new List<View>() { DVBTDriverRadioButton }))
            .AddItem(KeyboardFocusableItem.CreateFrom("FMDriver", new List<View>() { FMDriverRadioButton }))
            .AddItem(KeyboardFocusableItem.CreateFrom("Auto", new List<View>() { AutoScanButton }))
            .AddItem(KeyboardFocusableItem.CreateFrom("Manual", new List<View>() { ManualScanButton }))
            .AddItem(KeyboardFocusableItem.CreateFrom("Tune", new List<View>() { TuneButton }));

        //_focusItems.OnItemFocusedEvent += Page_OnItemFocusedEvent;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _focusItems.DeFocusAll();
        MainPage.SetToolBarColors(Parent as NavigationPage, Colors.White, Color.FromArgb("#29242a"));

        Task.Run(async () =>
        {
            await _viewModel.FillDrivers();
            _viewModel.UpdateActiveDriverType();
            _tuningSettings.LoadFromConfiguration(_configuration);
            await _tuningSettings.SetFrequencies(_driver);
            //_tuningSettings.SaveToConfiguration(_configuration);
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _tuningSettings.SaveToConfiguration(_configuration);
    }

    private void OnMenuKeyDown(KeyboardNavigationActionEnum keyAction)
    {
        switch (keyAction)
        {
            case KeyboardNavigationActionEnum.Right:
            case KeyboardNavigationActionEnum.Down:
                Task.Run(async () =>
                {
                    await MainMenu.SelectNextMenuItem(_appMenu.MenuItems, false);
                });
                break;

            case KeyboardNavigationActionEnum.Left:
            case KeyboardNavigationActionEnum.Up:
                Task.Run(async () =>
                {
                    await MainMenu.SelectNextMenuItem(_appMenu.MenuItems, true);
                });
                break;

            case KeyboardNavigationActionEnum.Back:
                _appMenu.HideMenu();
                break;

            case KeyboardNavigationActionEnum.OK:
                var item = _appMenu.GetSelectedMenuItem();
                if (item != null)
                {
                    Menu_Tapped(item);
                }
                break;
        }
    }

    public void OnKeyDown(string key, bool longPress)
    {
        _loggingService.Debug($"TuningWelcomePage Page OnKeyDown {key}");

        var keyAction = KeyboardDeterminer.GetKeyAction(key);

        if (MainMenu.MenuVisible)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                OnMenuKeyDown(keyAction);
            });
            return;
        }

        switch (keyAction)
        {
            case KeyboardNavigationActionEnum.Down:
            case KeyboardNavigationActionEnum.Right:
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    _focusItems.FocusNextItem();
                });
                break;

            case KeyboardNavigationActionEnum.Up:
            case KeyboardNavigationActionEnum.Left:
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    _focusItems.FocusPreviousItem();
                });
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
                        case "Auto":
                            AutoScanButton_Clicked(this, new EventArgs());
                            break;
                        case "Manual":
                            ManualScanButton_Clicked(this, new EventArgs());
                            break;
                        case "Tune":
                            TuneButton_Clicked(this, new EventArgs());
                            break;
                        case "DVBTDriver":
                            DVBTDriverRadioButton.IsChecked = !DVBTDriverRadioButton.IsChecked;
                            break;
                        case "FMDriver":
                            FMDriverRadioButton.IsChecked = !FMDriverRadioButton.IsChecked;
                            break;
                    }
                });
                break;
        }
    }

    public void OnTextSent(string text)
    {
        _loggingService.Debug($"TuningWelcomePage Page OnTextSent {text}");
    }

    private async void AutoScanButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"TuningWelcomePage: AutoScanButton_Clicked");

        //if (_viewModel.SelectedDriverType == DriverTypeEnum.RTLSDRDriver)
        //{
        //    _tuningSettings.SetFMSettings();
        //}

        ShowPage(_tuningProgressPage, TuneModeEnum.Automatic);
    }

    private void ManualScanButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"TuningWelcomePage: ManualScanButton_Clicked");

        if (_viewModel.FMDriverActive)
        {
            //_tuningSettings.SetFMSettings();
            ShowPage(_tuningFrequenciesPage, TuneModeEnum.Manual);
        } else
        {
            ShowPage(_selectDVBTPage, TuneModeEnum.Manual);
        }
    }

    private void TuneButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"TuningWelcomePage: TuneButton_Clicked");

        if (_viewModel.FMDriverActive)
        {
            //_tuningSettings.SetFMSettings();
            ShowPage(_tuningFrequencyPage, TuneModeEnum.Manual);
        }
        else
        {
            ShowPage(_selectDVBTPage, TuneModeEnum.Frequency);
        }
    }

    private void ShowPage(Page page, TuneModeEnum mode)
    {
        if (page.IsLoaded)
        {
            // preventing click when the settings page is just (or yet) loaded
            return;
        }

        // update settings according to selected driver
        _tuningSettings.LoadFromConfiguration(_configuration);
        // update frequencies according to driver
        _tuningSettings.SetFrequencies(_driver);
        _tuningSettings.TuningMode = mode;

        if (_viewModel.DVBTDriverActive && (mode == TuneModeEnum.Automatic))
        {
            _tuningSettings.DVBT = true;
            _tuningSettings.DVBT2 = true;
            _tuningSettings.TuneDVBTPreferred = false;
        }

        if (page is ITuningPage tuPage)
        {
            tuPage.UpdateSettings( _tuningSettings );
        }

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Navigation.PushAsync(page);
        });
    }
}