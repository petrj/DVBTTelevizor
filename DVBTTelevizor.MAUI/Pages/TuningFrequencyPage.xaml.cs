using LoggerService;
using static System.Net.Mime.MediaTypeNames;

namespace DVBTTelevizor.MAUI;

public partial class TuningFrequencyPage : ContentPage, IOnKeyDown
{
    private TuningFrequenciesViewModel _tuningFrequenciesViewModel;

    private ILoggingService _loggingService;
    private IDriverConnector _driver;
    private IDialogService _dialogService;
    private ITVConfiguration _configuration;
    private string _publicDirectory = "";

    private KeyboardFocusableItemList _focusItems;

    public TuningSettings _tuneSettings { get; set; }

    private FrequencyPage _frequencyPage;
    private TuningProgressPage _tuningProgressPage;

    public TuningFrequencyPage(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration, IDialogService dialogService, IPublicDirectoryProvider publicDirectoryProvider)
    {
        InitializeComponent();

        _loggingService = loggingService;
        _driver = driver;
        _configuration = tvConfiguration;
        _dialogService = dialogService;
        _publicDirectory = publicDirectoryProvider.GetPublicDirectoryPath();

        _tuneSettings = new TuningSettings();

        BindingContext = _tuningFrequenciesViewModel = new TuningFrequenciesViewModel(loggingService, driver, tvConfiguration, dialogService, publicDirectoryProvider);

        _tuningProgressPage = new TuningProgressPage(loggingService, driver, tvConfiguration, dialogService, publicDirectoryProvider);
        _frequencyPage = new FrequencyPage(loggingService, driver, tvConfiguration, dialogService, publicDirectoryProvider);
        _frequencyPage.Disappearing += _frequencyPage_Disappearing;

        BuildFocusableItems();
    }

    public TuningSettings? Settings
    {
        get
        {
            return _tuningFrequenciesViewModel?.Settings;
        }
        set
        {
            if (value == null)
            {
                return;
            }
            _tuningFrequenciesViewModel.Settings = value;
        }
    }

    private void BuildFocusableItems()
    {
        _focusItems = new KeyboardFocusableItemList();

        _focusItems
            .AddItem(KeyboardFocusableItem.CreateFrom("EditFreq", new List<View>() { EditFreqButton }))
            .AddItem(KeyboardFocusableItem.CreateFrom("Back", new List<View>() { BackButton }))
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

                /*
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
                    }
                });
                */
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

        _tuningProgressPage.Settings = _tuningFrequenciesViewModel.Settings;
        _tuningProgressPage.Settings.FrequencyFromKHz = _tuningFrequenciesViewModel.FrequencyKHz;
        _tuningProgressPage.Settings.FrequencyToKHz = _tuningFrequenciesViewModel.FrequencyKHz;
        _tuningProgressPage.UpdateActualFreq();

        await Navigation.PushAsync(_tuningProgressPage);
    }

    private void _frequencyPage_Disappearing(object? sender, EventArgs e)
    {
        if (Settings == null)
            return;

        _tuningFrequenciesViewModel.FrequencyKHz = _frequencyPage.Settings.FrequencyKHz;
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
            _frequencyPage.Settings.BandwidthKHz = Settings.BandwidthKHz;
            _frequencyPage.Settings.FrequencyKHz = Settings.FrequencyKHz;
            _frequencyPage.NotifyChange();
        }

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Navigation.PushAsync(_frequencyPage);
        });
    }

    private void EditFreqButton_Clicked(object sender, EventArgs e)
    {
        ShowFreqPage();
    }
}