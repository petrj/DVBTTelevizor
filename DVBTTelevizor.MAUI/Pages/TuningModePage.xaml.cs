
using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using LoggerService;
using static System.Net.Mime.MediaTypeNames;

namespace DVBTTelevizor.MAUI;

public partial class TuningModePage : ContentPage, IOnKeyDown, ITuningPage
{
    private TuningModePageViewModel _viewModel;

    private ILoggingService _loggingService;
    private IDriverConnector _driver;
    private ITVConfiguration _configuration;
    private string _publicDirectory = "";

    private KeyboardFocusableItemList _focusItems;

    private TuningSelectDVBTPage _selectDVBTPage;
    private TuningProgressPage _tuningProgressPage;
    private TuningFrequencyPage _tuningFrequencyPage;
    private TuningFrequenciesPage _tuningFrequenciesPage;

    public TuningModePage(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration,  IPublicDirectoryProvider publicDirectoryProvider)
    {
        InitializeComponent();

        _loggingService = loggingService;
        _driver = driver;
        _configuration = tvConfiguration;
        _publicDirectory = publicDirectoryProvider.GetPublicDirectoryPath();

        BindingContext = _viewModel = new TuningModePageViewModel(loggingService, driver, tvConfiguration, publicDirectoryProvider);

        _viewModel.Settings = new TuningSettings(_loggingService);

        _selectDVBTPage = new TuningSelectDVBTPage(loggingService, driver, tvConfiguration, publicDirectoryProvider);
        _tuningProgressPage = new TuningProgressPage(loggingService, driver, tvConfiguration, publicDirectoryProvider);
        _tuningFrequencyPage = new TuningFrequencyPage(loggingService, driver, tvConfiguration, publicDirectoryProvider);
        _tuningFrequenciesPage = new TuningFrequenciesPage(loggingService, driver, tvConfiguration, publicDirectoryProvider);

        BuildFocusableItems();
    }

    public void UpdateSettings(TuningSettings tuningSettings)
    {
        _viewModel.Settings = tuningSettings;
    }

    private void BuildFocusableItems()
    {
        _focusItems = new KeyboardFocusableItemList();

        _focusItems
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
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
    }


    public void OnKeyDown(string key, bool longPress)
    {
        _loggingService.Debug($"TuningModePage Page OnKeyDown {key}");

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
                    }
                });
                break;
        }
    }

    public void OnTextSent(string text)
    {
        _loggingService.Debug($"TuningModePage Page OnTextSent {text}");
    }

    private async void AutoScanButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"TuningModePage: AutoScanButton_Clicked");

        ShowPage(_tuningProgressPage, TuneModeEnum.Automatic);
    }

    private void ManualScanButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"TuningModePage: ManualScanButton_Clicked");

        if (_viewModel.Settings.DVBT || _viewModel.Settings.DVBT2)
        {
            ShowPage(_selectDVBTPage, TuneModeEnum.Manual);
        } else
        {
            ShowPage(_tuningFrequenciesPage, TuneModeEnum.Manual);
        }
    }

    private void TuneButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"TuningModePage: TuneButton_Clicked");

        if (_viewModel.Settings.DVBT || _viewModel.Settings.DVBT2)
        {
            ShowPage(_selectDVBTPage, TuneModeEnum.Frequency);
        }
        else
        {
            ShowPage(_tuningFrequencyPage, TuneModeEnum.Frequency);
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
        _viewModel?.Settings.LoadFromConfiguration(_configuration);
        // update frequencies according to driver
        _viewModel?.Settings.SetFrequencies(_driver);
        _viewModel?.Settings.TuningMode = mode;

        if (_viewModel.DVBTDriverActive && (mode == TuneModeEnum.Automatic))
        {
            _viewModel?.Settings.DVBT = true;
            _viewModel?.Settings.DVBT2 = true;
            _viewModel?.Settings.TuneDVBTPreferred = false;
        }

        if (page is ITuningPage tuPage)
        {
            tuPage.UpdateSettings(_viewModel?.Settings);
        }

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Navigation.PushAsync(page);
        });
    }
}