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

        if (sender is FrequencyPage page)
        {
            if (page.Confirmed)
            {
                _tuningFrequenciesViewModel.FrequencyKHz =
                    page.Settings.FrequencyKHz;
            }
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
        Settings?.SaveToConfiguration(_configuration);
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

            _configuration.FrequencyKHz = Settings.FrequencyKHz;

            page?.UpdateSettings(Settings);
        }

        await MainPage.ShowPage<TuningProgressPage>(Navigation, page);
    }

    private async Task ShowFreqPage()
    {
        var page = MainPage.GetOrCreatePage<FrequencyPage>(_loggingService, _driver, null, _configuration, _publicDirectoryProvider,
            ()=> { _frequencyPage_Disappearing(this, new EventArgs()); } ) as FrequencyPage;


        if (page.Settings != null && Settings != null)
        {
            page.UpdateSettings(Settings);
        }

        page.Confirmed = false;

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