using LoggerService;
using static System.Net.Mime.MediaTypeNames;

namespace DVBTTelevizor.MAUI;

public partial class TuningWelcomePage : ContentPage, IOnKeyDown
{
    private TuningWelcomePageViewModel _driverPageViewModel;

    private ILoggingService _loggingService;
    private IDriverConnector _driver;
    private IDialogService _dialogService;
    private ITVCConfiguration _configuration;
    private string _publicDirectory = "";

    private KeyboardFocusableItemList _focusItems;

    private TuningSelectDVBTPage _selectDVBTPage;

    public TuningWelcomePage(ILoggingService loggingService, IDriverConnector driver, ITVCConfiguration tvConfiguration, IDialogService dialogService, IPublicDirectoryProvider publicDirectoryProvider)
    {
        InitializeComponent();

        _loggingService = loggingService;
        _driver = driver;
        _configuration = tvConfiguration;
        _dialogService = dialogService;
        _publicDirectory = publicDirectoryProvider.GetPublicDirectoryPath();

        BindingContext = _driverPageViewModel = new TuningWelcomePageViewModel(loggingService, driver, tvConfiguration, dialogService, publicDirectoryProvider);

        _selectDVBTPage = new TuningSelectDVBTPage(loggingService, driver, tvConfiguration, dialogService, publicDirectoryProvider);

        BuildFocusableItems();
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

        if (_selectDVBTPage.IsLoaded)
        {
            // preventing click when the settings page is just (or yet) loaded
            return;
        }

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Navigation.PushAsync(_selectDVBTPage);
        });
    }

    private void ManualScanButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"TuningWelcomePage: ManualScanButton_Clicked");
    }

    private void TuneButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"TuningWelcomePage: TuneButton_Clicked");
    }
}