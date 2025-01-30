using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using LoggerService;

namespace DVBTTelevizor.MAUI;

public partial class SettingsPage : ContentPage
{
    private SettingsPageViewModel _settingsPageViewModel;

    private ILoggingService _loggingService { get; set; }
    private IDriverConnector _driver { get; set; }
    private IDialogService _dialogService;
    private ITVCConfiguration _configuration;

    private string _publicDirectory = "";

    public SettingsPage(ILoggingService loggingService, IDriverConnector driver, ITVCConfiguration tvConfiguration, IDialogService dialogService, IPublicDirectoryProvider publicDirectoryProvider)
	{
        InitializeComponent();

        _loggingService = loggingService;
        _driver = driver;
        _configuration = tvConfiguration;
        _dialogService = dialogService;
        _publicDirectory = publicDirectoryProvider.GetPublicDirectoryPath();

        BindingContext = _settingsPageViewModel = new SettingsPageViewModel(loggingService, driver, tvConfiguration, dialogService, publicDirectoryProvider);

        this.Unloaded += SettingsPage_Unloaded;
    }

    private void SettingsPage_Unloaded(object? sender, EventArgs e)
    {

    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (Parent is NavigationPage navigationPage)
        {
            navigationPage.BarBackgroundColor = Color.FromArgb("#29242a");
            navigationPage.BarTextColor = Colors.White;
        }

        if (_settingsPageViewModel != null)
        {
            _settingsPageViewModel.FillAutoPlayChannels();

            if (_settingsPageViewModel.FontSizes.Count == 0)
            {
                _settingsPageViewModel.FillFontSizes();
            }

            if (_settingsPageViewModel.DVBTDrivers.Count == 0)
            {
                _settingsPageViewModel.FillDVBTDrivers();
            }
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
    }

    private void OnRemoteTelevizorLabelTapped(object sender, TappedEventArgs e)
    {

    }

    private void ExportLanguageButton_Clicked(object sender, EventArgs e)
    {
        var fileName = Path.Join(_publicDirectory, "en.lng");
        Lng.SaveToFile(fileName);

        WeakReferenceMessenger.Default.Send(new ToastMessage($"Language exported to {fileName}"));
    }
}