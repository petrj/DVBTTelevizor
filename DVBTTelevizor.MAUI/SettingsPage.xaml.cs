using LoggerService;

namespace DVBTTelevizor.MAUI;

public partial class SettingsPage : ContentPage
{
    private SettingsPageViewModel _settingsPageViewModel;

    private ILoggingService _loggingService { get; set; }
    private IDriverConnector _driver { get; set; }
    private IDialogService _dialogService;
    private ITVCConfiguration _configuration;

    public SettingsPage(ILoggingService loggingService, IDriverConnector driver, ITVCConfiguration tvConfiguration, IDialogService dialogService, IPublicDirectoryProvider publicDirectoryProvider)
	{
        InitializeComponent();

        _loggingService = loggingService;
        _driver = driver;
        _configuration = tvConfiguration;
        _dialogService = dialogService;

        BindingContext = _settingsPageViewModel = new SettingsPageViewModel(loggingService, driver, tvConfiguration, dialogService, publicDirectoryProvider);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (Parent is NavigationPage navigationPage)
        {
            navigationPage.BarBackgroundColor = Color.FromArgb("#29242a");
            navigationPage.BarTextColor = Colors.White;
        }
    }
}