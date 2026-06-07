using DVBTTelevizor.TV;
using LoggerService;

namespace DVBTTelevizor.MAUI;

public partial class TuningSelectDVBTPage : ContentPage, ITuningPage, IOnKeyDown
{
    private TuningSelectDVBTPageViewModel _tuningSelectDVBTViewModel;

    private ILoggingService _loggingService;
    private IDriverConnector _driver;
    private ITVConfiguration _configuration;
    private string _publicDirectory = "";

    private KeyboardFocusableItemList _focusItems;

    private string? _lastSelectedCenterItem = null;
    private IPublicDirectoryProvider _publicDirectoryProvider;

    public bool Finished { get; set; } = false;


    public TuningSelectDVBTPage(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration, IPublicDirectoryProvider publicDirectoryProvider)
    {
        InitializeComponent();

        _loggingService = loggingService;
        _driver = driver;
        _configuration = tvConfiguration;
        _publicDirectoryProvider = publicDirectoryProvider;
        _publicDirectory = publicDirectoryProvider.GetPublicDirectoryPath();

        BindingContext = _tuningSelectDVBTViewModel = new TuningSelectDVBTPageViewModel(loggingService, driver, tvConfiguration, publicDirectoryProvider);
        _tuningSelectDVBTViewModel.Initializing = false;

        _focusItems = BuildFocusableItems();
    }

    public void UpdateSettings(TuningSettings tuningSettings)
    {
        _tuningSelectDVBTViewModel.Settings = tuningSettings;
        _tuningSelectDVBTViewModel?.Update();
    }

    private KeyboardFocusableItemList BuildFocusableItems()
    {
        var focusItems = new KeyboardFocusableItemList();

        focusItems
            .AddItem(KeyboardFocusableItem.CreateFrom("DVBT", new List<View>() { DVBTBoxView, DVBTSwitch }))
            .AddItem(KeyboardFocusableItem.CreateFrom("DVBT2", new List<View>() { DVBT2BoxView, DVBT2Switch }))
            .AddItem(KeyboardFocusableItem.CreateFrom("Bandwidth", new List<View>() { BandwidthBoxView, BandwidthPicker }))
            .AddItem(KeyboardFocusableItem.CreateFrom("Back", new List<View>() { BackButton }))
            .AddItem(KeyboardFocusableItem.CreateFrom("Next", new List<View>() { NextButton }));

        focusItems.OnItemFocusedEvent += _focusItems_OnItemFocusedEvent;

        return focusItems;
    }

    private void _focusItems_OnItemFocusedEvent(KeyboardFocusableItemEventArgs _args)
    {
        if (_focusItems.FocusedItem == null)
        {
            return;
        }

        if (_focusItems.FocusedItem.Name == "DVBT")
        {
            _lastSelectedCenterItem = "DVBT";
        }
        else
        if (_focusItems.FocusedItem.Name == "DVBT2")
        {
            _lastSelectedCenterItem = "DVBT2";
        }
        else
        if (_focusItems.FocusedItem.Name == "Bandwidth")
        {
            _lastSelectedCenterItem = "Bandwidth";
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _tuningSelectDVBTViewModel.FillBandwidths();

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
            case AppDriverTypeEnum.DVBT:

                _configuration.TuneDVBTEnabled = _tuningSelectDVBTViewModel.Settings.DVBT;
                _configuration.TuneDVBT2Enabled = _tuningSelectDVBTViewModel.Settings.DVBT2;
                _configuration.TuneDVBTPreferred = _tuningSelectDVBTViewModel.Settings.TuneDVBTPreferred;

                _configuration.DVBTBandwidthKHz = _tuningSelectDVBTViewModel.Settings.BandwidthKHz;
            break;
            default:
                break;
        }
    }

    public void OnKeyDown(string key, bool longPress)
    {
        _loggingService.Debug($"TuningSelectDVBTPage Page OnKeyDown {key}");

        var keyAction = KeyboardDeterminer.GetKeyAction(key);

        switch (keyAction)
        {
            case KeyboardNavigationActionEnum.Down:
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    _focusItems.FocusNextItem();
                });
                break;

            case KeyboardNavigationActionEnum.Right:
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    Right();
                });
                break;

            case KeyboardNavigationActionEnum.Up:
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    _focusItems.FocusPreviousItem();
                });
                break;

            case KeyboardNavigationActionEnum.Left:
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    Left();
                });
                break;

            case KeyboardNavigationActionEnum.Back:
                BackButton_Clicked(this, new EventArgs());
                break;

            case KeyboardNavigationActionEnum.OK:

                if (_focusItems.FocusedItem == null)
                    return;

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    switch (_focusItems.FocusedItem.Name)
                    {
                        case "DVBT":
                            DVBTSwitch.IsToggled = !DVBTSwitch.IsToggled;
                            break;
                        case "DVBT2":
                            DVBT2Switch.IsToggled = !DVBT2Switch.IsToggled;
                            break;
                        case "Bandwidth":
                            BandwidthPicker.Focus();
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

    private void Right()
    {
        if (_focusItems.FocusedItem == null)
        {
            _focusItems.FocusItem("Next", KeyboardFocusDirection.Next);
            return;
        }

        switch (_focusItems.FocusedItem.Name)
        {
            case "DVBT":
            case "DVBT2":
            case "Bandwidth":
                _focusItems.FocusItem("Next", KeyboardFocusDirection.Next);
                break;
            case "Back":
                _focusItems.FocusItem(_lastSelectedCenterItem == null ? "DVBT" : _lastSelectedCenterItem, KeyboardFocusDirection.Next);
                break;
            case "Next":
                _focusItems.FocusItem("Back", KeyboardFocusDirection.Next);
                break;
        }
    }

    private void Left()
    {
        if (_focusItems.FocusedItem == null)
        {
            _focusItems.FocusItem("Back", KeyboardFocusDirection.Next);
            return;
        }

        switch (_focusItems.FocusedItem.Name)
        {
            case "DVBT":
            case "DVBT2":
            case "Bandwidth":
                _focusItems.FocusItem("Back", KeyboardFocusDirection.Next);
                break;
            case "Back":
                _focusItems.FocusItem("Next", KeyboardFocusDirection.Next);
                break;
            case "Next":
                _focusItems.FocusItem(_lastSelectedCenterItem == null ? "DVBT" : _lastSelectedCenterItem, KeyboardFocusDirection.Next);
                break;
        }
    }

    public void OnTextSent(string text)
    {
        _loggingService.Debug($"TuningSelectDVBTPage Page OnTextSent {text}");
    }

    private void BackButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"TuningSelectDVBTPage Page BackButton_Clicked");

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Navigation.PopAsync();
        });
    }

    public async Task ShowPage<T>() where T : Page
    {
        var page = MainPage.GetOrCreatePage<T>(_loggingService, _driver, null, _configuration, _publicDirectoryProvider);

        if (page is ITuningPage tuPage)
        {
            tuPage.UpdateSettings(_tuningSelectDVBTViewModel?.Settings);
        }

        await MainPage.ShowPage<T>(Navigation, page);
    }

    private async void NextButton_Clicked(object sender, EventArgs e)
    {
        switch (_tuningSelectDVBTViewModel.Settings.TuningMode)
        {
            case TuneModeEnum.Manual:
                await ShowPage<TuningFrequenciesPage>();
                break;
            case TuneModeEnum.Frequency:
                await ShowPage<TuningFrequencyPage>();
                break;
        }
    }
}