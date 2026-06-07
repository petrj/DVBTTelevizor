using LoggerService;
using static System.Net.Mime.MediaTypeNames;

namespace DVBTTelevizor.MAUI;

public partial class TuningFrequenciesPage : ContentPage, ITuningPage, IOnKeyDown
{
    private TuningFrequenciesViewModel _tuningFrequenciesViewModel;

    private ILoggingService _loggingService;
    private IDriverConnector _driver;
    private ITVConfiguration _configuration;

    private KeyboardFocusableItemList _focusItems;

    private bool _editingFrom = false;
    private IPublicDirectoryProvider _publicDirectoryProvider;

    private bool _dissapearingRegistered = false;

    public TuningFrequenciesPage(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration, IPublicDirectoryProvider publicDirectoryProvider)
    {
        InitializeComponent();

        _loggingService = loggingService;
        _driver = driver;
        _configuration = tvConfiguration;
        _publicDirectoryProvider = publicDirectoryProvider;

        BindingContext = _tuningFrequenciesViewModel = new TuningFrequenciesViewModel(loggingService, driver, tvConfiguration, publicDirectoryProvider);

        BuildFocusableItems();
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
            .AddItem(KeyboardFocusableItem.CreateFrom("EditFreqFrom", new List<View>() { EditFreqFromButton }))
            .AddItem(KeyboardFocusableItem.CreateFrom("EditFreqTo", new List<View>() { EditFreqToButton }))
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

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _tuningFrequenciesViewModel.Settings.SaveToConfiguration(_configuration);
    }

    public void OnKeyDown(string key, bool longPress)
    {
        _loggingService.Debug($"TuningFrequenciesPage OnKeyDown {key}");


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
                        case "EditFreqFrom":
                            EditFreqFromButton_Clicked(this, new EventArgs());
                            break;
                        case "EditFreqTo":
                            EditFreqToButton_Clicked(this, new EventArgs());
                            break;
                        case "Back":
                            BackButton_Clicked(this, new EventArgs());
                            break;
                        case "Next":
                            NextButton_Clicked(this, new EventArgs());
                            break;
                    }
                });

                break;
        }
    }

    public void OnTextSent(string text)
    {
        _loggingService.Debug($"TuningFrequenciesPage Page OnTextSent {text}");
    }

    private void BackButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"TuningSelectDVBTPage BackButton_Clicked");

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Navigation.PopAsync();
        });
    }

    private async void NextButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"TuningSelectDVBTPage NextButton_Clicked");

        var page = MainPage.GetOrCreatePage<TuningProgressPage>(_loggingService, _driver, null, _configuration, _publicDirectoryProvider) as TuningProgressPage;

        if (Settings != null)
        {
            _configuration.FrequencyFromKHz = Settings.FrequencyFromKHz;
            _configuration.FrequencyToKHz = Settings.FrequencyToKHz;

            page?.UpdateSettings(Settings);
        }

        page?.ResetTune(true);
        await MainPage.ShowPage<TuningProgressPage>(Navigation, page);
    }

     private async void EditFreqToButton_Clicked(object sender, EventArgs e)
    {
        _editingFrom = false;
        await ShowFreqPage();
    }

    private void _frequencyPage_Disappearing(object? sender, EventArgs e)
    {
        if (Settings == null)
            return;

        var page = GetFreqPage();

        if (_editingFrom)
        {
            _tuningFrequenciesViewModel.FrequencyFromKHz = page.Settings.FrequencyKHz;
        }
        else
        {
            _tuningFrequenciesViewModel.FrequencyToKHz = page.Settings.FrequencyKHz;
        }
    }

    private async void EditFreqFromButton_Clicked(object sender, EventArgs e)
    {
        _editingFrom = true;
        await ShowFreqPage();
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
            var settings = Settings.Clone(_loggingService);
            settings.FrequencyKHz = _editingFrom ? Settings.FrequencyFromKHz : Settings.FrequencyToKHz;

            page.TuneFrequencyMode = _editingFrom ? TuneFrequencyModeEnum.From : TuneFrequencyModeEnum.To;

            page.UpdateSettings(Settings);
        }

        await MainPage.ShowPage<FrequencyPage>(Navigation, page);
    }

    public void UpdateSettings(TuningSettings tuningSettings)
    {
        _tuningFrequenciesViewModel.Settings = tuningSettings;
    }
}