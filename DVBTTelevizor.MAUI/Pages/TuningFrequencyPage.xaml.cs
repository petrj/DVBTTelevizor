using LoggerService;
using static System.Net.Mime.MediaTypeNames;

namespace DVBTTelevizor.MAUI;

public partial class TuningFrequencyPage : ContentPage, ITuningPage, IOnKeyDown
{
    private TuningFrequenciesViewModel _tuningFrequenciesViewModel;

    private ILoggingService _loggingService;
    private IDriverConnector _driver;
    private ITVConfiguration _configuration;
    private string _publicDirectory = "";

    private KeyboardFocusableItemList _focusItems;

    private FrequencyPage _frequencyPage;
    private TuningProgressPage _tuningProgressPage;

    public TuningFrequencyPage(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration, IPublicDirectoryProvider publicDirectoryProvider)
    {
        InitializeComponent();

        _loggingService = loggingService;
        _driver = driver;
        _configuration = tvConfiguration;
        _publicDirectory = publicDirectoryProvider.GetPublicDirectoryPath();

        BindingContext = _tuningFrequenciesViewModel = new TuningFrequenciesViewModel(loggingService, driver, tvConfiguration, publicDirectoryProvider);

        _tuningFrequenciesViewModel.Settings = new TuningSettings(_loggingService);

        _tuningProgressPage = new TuningProgressPage(loggingService, driver, tvConfiguration, publicDirectoryProvider);
        _frequencyPage = new FrequencyPage(loggingService, driver, tvConfiguration, publicDirectoryProvider);
        _frequencyPage.Disappearing += _frequencyPage_Disappearing;

        BuildFocusableItems();
    }

    private void _frequencyPage_Disappearing(object? sender, EventArgs e)
    {
        if (Settings == null)
            return;

        if (_frequencyPage.Confirmed)
        {
            _tuningFrequenciesViewModel.FrequencyKHz =
                _frequencyPage.Settings.FrequencyKHz;
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

        if (_tuningProgressPage.IsLoaded)
        {
            // preventing click when the settings page is just (or yet) loaded
            return;
        }

        if (Settings != null)
        {
            Settings.FrequencyFromKHz = Settings.FrequencyKHz;
            Settings.FrequencyToKHz = Settings.FrequencyKHz;

            _configuration.FrequencyKHz = Settings.FrequencyKHz;

            _tuningProgressPage.UpdateSettings(Settings);
        }

        //_tuningProgressPage.Settings = Settings;
        //_tuningProgressPage.Settings.FrequencyFromKHz = Settings.FrequencyKHz;
        //_tuningProgressPage.Settings.FrequencyToKHz = Settings.FrequencyKHz;
        //_tuningProgressPage.UpdateActualFreq();

        await Navigation.PushAsync(_tuningProgressPage);
    }

    private void ShowFreqPage()
    {
        if (_frequencyPage.IsLoaded)
        {
            // preventing click when the settings page is just (or yet) loaded
            return;
        }

        if (_frequencyPage.Settings != null && Settings != null)
        {
            _frequencyPage.UpdateSettings(Settings);
        }

        _frequencyPage.Confirmed = false;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Navigation.PushAsync(_frequencyPage);
        });
    }

    private void EditFreqButton_Clicked(object sender, EventArgs e)
    {
        ShowFreqPage();
    }

    public void UpdateSettings(TuningSettings tuningSettings)
    {
        _tuningFrequenciesViewModel.Settings = tuningSettings;
    }
}