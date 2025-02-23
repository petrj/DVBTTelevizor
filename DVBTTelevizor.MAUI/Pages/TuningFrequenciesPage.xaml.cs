using LoggerService;
using static System.Net.Mime.MediaTypeNames;

namespace DVBTTelevizor.MAUI;

public partial class TuningFrequenciesPage : ContentPage, IOnKeyDown
{
    private TuningFrequenciesViewModel _tuningFrequenciesViewModel;

    private ILoggingService _loggingService;
    private IDriverConnector _driver;
    private IDialogService _dialogService;
    private ITVConfiguration _configuration;
    private string _publicDirectory = "";

    private KeyboardFocusableItemList _focusItems;

    private TuningProgressPage _tuningProgressPage;

    public TuningFrequenciesPage(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration, IDialogService dialogService, IPublicDirectoryProvider publicDirectoryProvider)
    {
        InitializeComponent();

        _loggingService = loggingService;
        _driver = driver;
        _configuration = tvConfiguration;
        _dialogService = dialogService;
        _publicDirectory = publicDirectoryProvider.GetPublicDirectoryPath();

        BindingContext = _tuningFrequenciesViewModel = new TuningFrequenciesViewModel(loggingService, driver, tvConfiguration, dialogService, publicDirectoryProvider);

        _tuningProgressPage = new TuningProgressPage(loggingService, driver, tvConfiguration, dialogService, publicDirectoryProvider);

        BuildFocusableItems();
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

        if (_tuningProgressPage.IsLoaded)
        {
            // preventing click when the settings page is just (or yet) loaded
            return;
        }

        _tuningProgressPage.DVBTTuning = _tuningFrequenciesViewModel.DVBT;
        _tuningProgressPage.DVBT2Tuning = _tuningFrequenciesViewModel.DVBT2;
        _tuningProgressPage.TuneBandWidthKHz = _tuningFrequenciesViewModel.TuneBandWidthKHz;

        await Navigation.PushAsync(_tuningProgressPage);
    }

    public long TuneBandWidthKHz
    {
        get
        {
            return _tuningFrequenciesViewModel == null ? 8000 : _tuningFrequenciesViewModel.TuneBandWidthKHz;
        }
        set
        {
            if (_tuningFrequenciesViewModel == null)
            {
                return;
            }

            _tuningFrequenciesViewModel.TuneBandWidthKHz = value;
        }
    }

    public bool DVBT
    {
        get
        {
            return _tuningFrequenciesViewModel == null ? true : _tuningFrequenciesViewModel.DVBT;
        }
        set
        {
            if (_tuningFrequenciesViewModel == null)
            {
                return;
            }

            _tuningFrequenciesViewModel.DVBT = value;
        }
    }

    public bool DVBT2
    {
        get
        {
            return _tuningFrequenciesViewModel == null ? true : _tuningFrequenciesViewModel.DVBT2;
        }
        set
        {
            if (_tuningFrequenciesViewModel == null)
            {
                return;
            }

            _tuningFrequenciesViewModel.DVBT2 = value;
        }
    }

    private void EditFreqToButton_Clicked(object sender, EventArgs e)
    {

    }

    private void EditFreqFromButton_Clicked(object sender, EventArgs e)
    {

    }
}