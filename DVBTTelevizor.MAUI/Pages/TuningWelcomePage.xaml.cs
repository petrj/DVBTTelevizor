
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
                try
                {
                    //_viewModel.IgnoreDriverChangeEvent = true;
                    await _viewModel.ChangeDriver(item.DriverType);
                } finally
                {
                    //.IgnoreDriverChangeEvent = false;
                }

                break;
            case "menuCancelChangeDriver":
                _viewModel.UpdateActiveDriverType();
                //switch (_configuration.DVBTDriverType)
                //{
                //    case DriverTypeEnum.AndroidDVBTDriver:
                //        _viewModel.DVBTDriverActive = true;
                //        _viewModel.FMDriverActive = false;
                //        break;
                //    case DriverTypeEnum.RTLSDRDriver:
                //        _viewModel.FMDriverActive = true;
                //        _viewModel.DVBTDriverActive = false;
                //        break;
                //}
                break;
        }
    }

    private void _appMenu_MenuVisibleChanged(object? sender, MenuVisibleChangedEventArgs e)
    {
        _viewModel.MenuVisible = e.IsVisible;
    }

    private void DriverRadioButtonCheckedChanged(bool value, DriverTypeEnum driver)
    {
        if (!value)
            return;

        if (_viewModel.IgnoreDriver == driver)
        {
            _viewModel.IgnoreDriver = null;
            return;
        }

        Task.Run(async () =>
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    _appMenu.ShowConfirmChangeDriverMenu(_configuration.DVBTDriverType, driver);
                });
            }
            finally
            {

            }
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
            .AddItem(KeyboardFocusableItem.CreateFrom("DVBTDriver", new List<View>() { DVBTDriverBoxView, DVBTDriverRadioButton }))
            .AddItem(KeyboardFocusableItem.CreateFrom("FMDriver", new List<View>() { FMDriverBoxView, FMDriverRadioButton }))
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

    public void OnKeyDown(string key, bool longPress)
    {
        _loggingService.Debug($"TuningWelcomePage Page OnKeyDown {key}");

        var keyAction = KeyboardDeterminer.GetKeyAction(key);

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
                            break;
                        case "FMDriver":
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

        _tuningSettings.FM = false;
        _tuningSettings.DVBT = false;
        _tuningSettings.DVBT2 = false;

        switch (_configuration.DVBTDriverType)
        {
            case MAUI.DriverTypeEnum.RTLSDRDriver:
                _tuningSettings.FM = true;
                break;
            default:
                if (mode == TuneModeEnum.Automatic)
                {
                    _tuningSettings.DVBT = true;
                    _tuningSettings.DVBT2 = true;
                    _tuningSettings.TuneDVBTPreferred = false;
                } else
                {
                    _tuningSettings.DVBT = _configuration.TuneDVBTEnabled;
                    _tuningSettings.DVBT2 = _configuration.TuneDVBT2Enabled;
                    _tuningSettings.TuneDVBTPreferred = _configuration.TuneDVBTPreferred;
                }
                break;
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