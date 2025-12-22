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

    private TuningSelectDVBTPage _selectDVBTPage;
    private TuningProgressPage _tuningProgressPage;
    private TuningFrequencyPage _tuningFrequencyPage;
    private TuningFrequenciesPage _tuningFrequenciesPage;

    public TuningWelcomePage(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration,  IPublicDirectoryProvider publicDirectoryProvider)
    {
        InitializeComponent();

        _loggingService = loggingService;
        _driver = driver;
        _configuration = tvConfiguration;
        _publicDirectory = publicDirectoryProvider.GetPublicDirectoryPath();

        _tuningSettings = new TuningSettings(_loggingService);

        BindingContext = _viewModel = new TuningWelcomePageViewModel(loggingService, driver, tvConfiguration, publicDirectoryProvider);

        _selectDVBTPage = new TuningSelectDVBTPage(loggingService, driver, tvConfiguration, publicDirectoryProvider);
        _tuningProgressPage = new TuningProgressPage(loggingService, driver, tvConfiguration, publicDirectoryProvider);
        _tuningFrequencyPage = new TuningFrequencyPage(loggingService, driver, tvConfiguration, publicDirectoryProvider);
        _tuningFrequenciesPage = new TuningFrequenciesPage(loggingService, driver, tvConfiguration, publicDirectoryProvider);

        BuildFocusableItems();
    }

    private void BuildFocusableItems()
    {
        _focusItems = new KeyboardFocusableItemList();

        _focusItems
            .AddItem(KeyboardFocusableItem.CreateFrom("Driver", new List<View>() { DriverTypeBoxView, DriverPicker }))
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
                        case "Driver":
                            MainThread.BeginInvokeOnMainThread(async () =>
                            {
                                DriverPicker.Focus();
                            });
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

        if (_viewModel.SelectedDriverType == DriverTypeEnum.RTLSDRDriver)
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

        if (_viewModel.SelectedDriverType == DriverTypeEnum.RTLSDRDriver)
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