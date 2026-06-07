using DVBTTelevizor.TV;
using LoggerService;
using static System.Net.Mime.MediaTypeNames;

namespace DVBTTelevizor.MAUI;

public partial class TuningFrequencyPage : ContentPage, ITuningPage, IOnKeyDown
{
    private TuningFrequenciesViewModel _tuningFrequenciesViewModel;

    private ILoggingService _loggingService;
    private IDriverConnector _driver;
    private ITVConfiguration _configuration;
    private IPublicDirectoryProvider _publicDirectoryProvider;

    private KeyboardFocusableItemList _focusItems;

    private bool _dissapearingRegistered = false;

    public TuningFrequencyPage(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration, IPublicDirectoryProvider publicDirectoryProvider)
    {
        InitializeComponent();

        _loggingService = loggingService;
        _driver = driver;
        _configuration = tvConfiguration;
        _publicDirectoryProvider = publicDirectoryProvider;

        BindingContext = _tuningFrequenciesViewModel = new TuningFrequenciesViewModel(loggingService, driver, tvConfiguration, publicDirectoryProvider);

        _tuningFrequenciesViewModel.Settings = new TuningSettings(_loggingService);

        BuildFocusableItems();
    }

    public async Task ShowPage<T>(Action dissapearing) where T : Page
    {
        var page = MainPage.GetOrCreatePage<T>(_loggingService, _driver, null, _configuration, _publicDirectoryProvider, dissapearing);
        await MainPage.ShowPage<T>(Navigation, page);
    }

    private void _frequencyPage_Disappearing(object? sender, EventArgs e)
    {
        if (Settings == null)
            return;

        var page = GetFreqPage();

        if (page.Confirmed && page.Settings != null &&
            page.TuneFrequencyMode == TuneFrequencyModeEnum.Center)
        {
            _tuningFrequenciesViewModel.FrequencyKHz =
                page.Settings.FrequencyKHz;
        }
    }


    public TuningSettings? Settings
    {
        get
        {
            return _tuningFrequenciesViewModel?.Settings;
        }
    }

    private void BuildFocusableItems()
    {
        _focusItems = new KeyboardFocusableItemList();

        _focusItems
            .AddItem(KeyboardFocusableItem.CreateFrom("Back", new List<View>() { BackButton }))
            .AddItem(KeyboardFocusableItem.CreateFrom("EditFreq", new List<View>() { EditFreqButton }))
            .AddItem(KeyboardFocusableItem.CreateFrom("Next", new List<View>() { NextButton }));

        //_focusItems.OnItemFocusedEvent += Page_OnItemFocusedEvent;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _focusItems.DeFocusAll();
        MainPage.SetToolBarColors(Parent as NavigationPage, Colors.White, Color.FromArgb("#29242a"));
    }
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        SaveToConfiguration();
    }

    private void SaveToConfiguration()
    {
        switch (_configuration.AppDriverType)
        {
            case AppDriverTypeEnum.FM:
                _configuration.FMFrequencyKHz = _tuningFrequenciesViewModel.Settings.FrequencyKHz;
                break;

            case AppDriverTypeEnum.DAB:
                _configuration.DABFrequencyKHz = _tuningFrequenciesViewModel.Settings.FrequencyKHz;
                break;

            case AppDriverTypeEnum.DVBT:
            default:
                _configuration.FrequencyKHz = _tuningFrequenciesViewModel.Settings.FrequencyKHz;
                break;
        }
    }

    public void OnKeyDown(string key, bool longPress)
    {
        _loggingService.Debug($"TuningFrequencyPage OnKeyDown {key}");


        var keyAction = KeyboardDeterminer.GetKeyAction(key);

        switch (keyAction)
        {
            case KeyboardNavigationActionEnum.Down:
            case KeyboardNavigationActionEnum.Right:
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    _focusItems.FocusNextItem(true);
                });
                break;

            case KeyboardNavigationActionEnum.Up:
            case KeyboardNavigationActionEnum.Left:
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

                if (_focusItems.FocusedItem == null)
                    return;

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    switch (_focusItems.FocusedItem.Name)
                    {
                        case "Back":
                            BackButton_Clicked(this, new EventArgs());
                            break;
                        case "Next":
                            NextButton_Clicked(this, new EventArgs());
                            break;
                        case "EditFreq":
                            EditFreqButton_Clicked(this, new EventArgs());
                            break;
                    }
                });

                break;
        }
    }

    public void OnTextSent(string text)
    {
        _loggingService.Debug($"TuningFrequencyPage Page OnTextSent {text}");
    }

    private void BackButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"TuningFrequencyPage BackButton_Clicked");

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Navigation.PopAsync();
        });
    }

    private async void NextButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"TuningFrequencyPage NextButton_Clicked");

        var page = MainPage.GetOrCreatePage<TuningProgressPage>(_loggingService, _driver, null, _configuration, _publicDirectoryProvider) as TuningProgressPage;

        if (Settings != null)
        {
            Settings.FrequencyFromKHz = Settings.FrequencyKHz;
            Settings.FrequencyToKHz = Settings.FrequencyKHz;

            page?.UpdateSettings(Settings);
        }

        page?.ResetTune(true);
        await MainPage.ShowPage<TuningProgressPage>(Navigation, page);
    }
    private FrequencyPage GetFreqPage()
    {
        var page = MainPage.GetOrCreatePage<FrequencyPage>(_loggingService, _driver, null, _configuration, _publicDirectoryProvider) as FrequencyPage;

        if (!_dissapearingRegistered)
        {
            page.Disappearing += _frequencyPage_Disappearing;
            _dissapearingRegistered = true;
        }

        return page;
    }

    private async Task ShowFreqPage()
    {
        var page = GetFreqPage();

        if (page.Settings != null && Settings != null)
        {
            page.UpdateSettings(Settings);
        }

        page.Confirmed = false;
        page.TuneFrequencyMode = TuneFrequencyModeEnum.Center;

        await MainPage.ShowPage<FrequencyPage>(Navigation, page);
    }

    private async void EditFreqButton_Clicked(object sender, EventArgs e)
    {
        await ShowFreqPage();
    }

    public void UpdateSettings(TuningSettings tuningSettings)
    {
        _tuningFrequenciesViewModel.Settings = tuningSettings;
    }
}