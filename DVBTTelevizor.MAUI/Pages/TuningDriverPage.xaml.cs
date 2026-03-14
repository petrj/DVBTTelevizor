
using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using LoggerService;
using static System.Net.Mime.MediaTypeNames;

namespace DVBTTelevizor.MAUI;

public partial class TuningDriverPage : ContentPage, IOnKeyDown
{
    private BaseViewModel _viewModel;

    private ILoggingService _loggingService;
    private IDriverConnector _driver;
    private ITVConfiguration _configuration;
    private string _publicDirectory = "";

    private TuningSettings _tuningSettings;

    private KeyboardFocusableItemList _focusItems;

    private TuningModePage _tuningModePage;

    public TuningDriverPage(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration,  IPublicDirectoryProvider publicDirectoryProvider)
    {
        InitializeComponent();

        _loggingService = loggingService;
        _driver = driver;
        _configuration = tvConfiguration;
        _publicDirectory = publicDirectoryProvider.GetPublicDirectoryPath();

        _tuningSettings = new TuningSettings(_loggingService);

        BindingContext = _viewModel = new TuningModePageViewModel(loggingService, driver, tvConfiguration, publicDirectoryProvider);

        _tuningModePage = new TuningModePage(loggingService, driver, tvConfiguration, publicDirectoryProvider);

        BuildFocusableItems();
    }

    private void BuildFocusableItems()
    {
        _focusItems = new KeyboardFocusableItemList();

        _focusItems
            .AddItem(KeyboardFocusableItem.CreateFrom("DVBT", new List<View>() { DVBTButton }))
            .AddItem(KeyboardFocusableItem.CreateFrom("FM", new List<View>() { FMButton }))
            .AddItem(KeyboardFocusableItem.CreateFrom("DAB", new List<View>() { DABButton }));

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
        //_tuningSettings.SaveToConfiguration(_configuration);
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
                        case "DVBT":
                            DVBTButton_Clicked(this, new EventArgs());
                            break;
                        case "FM":
                            FMButton_Clicked(this, new EventArgs());
                            break;
                        case "DAB":
                            DABButton_Clicked(this, new EventArgs());
                            break;
                    }
                });
                break;
        }
    }

    public void OnTextSent(string text)
    {
        _loggingService.Debug($"TuningDriverPage OnTextSent {text}");
    }

    private async void DVBTButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"TuningDriverPage: DVBTButton_Clicked");

        ShowPage(DriverTypeEnum.AndroidDVBTDriver);
    }

    private void FMButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"TuningDriverPage: FMButton_Clicked");

        ShowPage(DriverTypeEnum.RTLSDRDriverFM);
    }

    private void DABButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"TuningDriverPage:     private void DABButton_Clicked(object sender, EventArgs e)\r\n");

        ShowPage(DriverTypeEnum.RTLSDRDriverDAB);
    }

    private void ShowPage(DriverTypeEnum driverType)
    {
        if (_tuningModePage.IsLoaded)
        {
            // preventing click when the settings page is just (or yet) loaded
            return;
        }

        // update settings according to selected driver
        _tuningSettings.LoadFromConfiguration(_configuration);
        // update frequencies according to driver
        _tuningSettings.SetFrequencies(_driver);

        _tuningSettings.DVBT = false;
        _tuningSettings.DVBT2 = false;
        _tuningSettings.DAB = false;
        _tuningSettings.FM = false;

        switch (driverType)
        {
            case DriverTypeEnum.AndroidDVBTDriver:
                _tuningSettings.DVBT = true;
                _tuningSettings.DVBT2 = true;                
                break;
            case DriverTypeEnum.RTLSDRDriverFM:
                _tuningSettings.FM = true;
                break;
            case DriverTypeEnum.RTLSDRDriverDAB:
                _tuningSettings.DAB = true;
                break;
        }

        _tuningModePage.UpdateSettings( _tuningSettings );

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Navigation.PushAsync(_tuningModePage);
        });
    }
}