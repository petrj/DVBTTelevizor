using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using LoggerService;
using static System.Net.Mime.MediaTypeNames;

namespace DVBTTelevizor.MAUI;

public partial class DriverPage : ContentPage, IOnKeyDown
{
    private DriverPageViewModel _driverPageViewModel;

    private ILoggingService _loggingService;
    private IDriverConnector _driver;
    private IDialogService _dialogService;
    private ITVCConfiguration _configuration;
    private string _publicDirectory = "";

    private KeyboardFocusableItemList _focusItems;

    public DriverPage(ILoggingService loggingService, IDriverConnector driver, ITVCConfiguration tvConfiguration, IDialogService dialogService, IPublicDirectoryProvider publicDirectoryProvider)
    {
        InitializeComponent();

        _loggingService = loggingService;
        _driver = driver;
        _configuration = tvConfiguration;
        _dialogService = dialogService;
        _publicDirectory = publicDirectoryProvider.GetPublicDirectoryPath();

        BindingContext = _driverPageViewModel = new DriverPageViewModel(loggingService, driver, tvConfiguration, dialogService, publicDirectoryProvider);

        BuildFocusableItems();

    }

    private void BuildFocusableItems()
    {
        _focusItems = new KeyboardFocusableItemList();
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
        _loggingService.Debug($"DriverPage OnKeyDown {key}");
    }

    public void OnTextSent(string text)
    {
        _loggingService.Debug($"DriverPage OnTextSent {text}");
    }

    private async void InstallDriverButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"DriverPage InstallDriverButton_Clicked");

        await Browser.OpenAsync("https://play.google.com/store/apps/details?id=info.martinmarinov.dvbdriver", BrowserLaunchMode.External);
    }

    private void ConnectButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"DriverPage ConnectButton_Clicked");

        WeakReferenceMessenger.Default.Send(new ConnectMessage(String.Empty));
    }

    private void DisconnectButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"DriverPage DisconnectButton_Clicked");
    }
}