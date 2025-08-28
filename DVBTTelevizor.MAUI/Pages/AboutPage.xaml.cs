using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using LoggerService;
using Microsoft.Maui.Controls.PlatformConfiguration;
using static System.Net.Mime.MediaTypeNames;

namespace DVBTTelevizor.MAUI;

public partial class AboutPage : ContentPage, IOnKeyDown
{
    private AboutPageViewModel _aboutPageViewModel;

    private ILoggingService _loggingService;
    private IDriverConnector _driver;
    private IDialogService _dialogService;
    private ITVConfiguration _configuration;
    private string _publicDirectory = "";

    private KeyboardFocusableItemList _focusItems;

    public AboutPage(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration, IDialogService dialogService, IPublicDirectoryProvider publicDirectoryProvider)
    {
        InitializeComponent();

        _loggingService = loggingService;
        _driver = driver;
        _configuration = tvConfiguration;
        _dialogService = dialogService;
        _publicDirectory = publicDirectoryProvider.GetPublicDirectoryPath();

        BindingContext = _aboutPageViewModel = new AboutPageViewModel(loggingService, driver, tvConfiguration, dialogService, publicDirectoryProvider);

        BuildFocusableItems();
    }

    private void BuildFocusableItems()
    {
        _focusItems = new KeyboardFocusableItemList();

        _focusItems
            .AddItem(KeyboardFocusableItem.CreateFrom("GithubLabel", new List<View>() { GithubBoxView }))
            .AddItem(KeyboardFocusableItem.CreateFrom("WebLabel", new List<View>() { WebBoxView }))
            //.AddItem(KeyboardFocusableItem.CreateFrom("Email", new List<View>() { EmailBoxView }))
            .AddItem(KeyboardFocusableItem.CreateFrom("Donate1", new List<View>() { Donate1Button }));

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
        _loggingService.Debug($"AboutPage Page OnKeyDown {key}");

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
                        case "GithubLabel":
                            GithubLabel_Tapped(this, new TappedEventArgs(null));
                            break;
                        case "WebLabel":
                            Web_Tapped(this, new TappedEventArgs(null));
                            break;
                        case "Email":
                            Email_Tapped(this, new TappedEventArgs(null));
                            break;
                        case "Donate1":
                            _loggingService.Debug($"AboutPage: Donate1");
                            break;
                        case "Donate2":
                            _loggingService.Debug($"AboutPage: Donate2");
                            break;
                        case "Donate3":
                            _loggingService.Debug($"AboutPage: Donate3");
                            break;
                        case "Donate4":
                            _loggingService.Debug($"AboutPage: Donate4");
                            break;
                    }
                });
                break;
        }
    }

    public void OnTextSent(string text)
    {
        _loggingService.Debug($"AboutPage Page OnTextSent {text}");
    }

    private void GithubLabel_Tapped(object sender, TappedEventArgs e)
    {
        _loggingService.Debug($"GithubLabel_Tapped");

        WeakReferenceMessenger.Default.Send(new OpenURLMessage("https://github.com/petrj/DVBTTelevizor"));
    }

    private void Web_Tapped(object sender, TappedEventArgs e)
    {
        _loggingService.Debug($"Web_Tapped");

        WeakReferenceMessenger.Default.Send(new OpenURLMessage("https://www.dvbttelevizor.petrjanousek.net/"));
    }

    private void Email_Tapped(object sender, TappedEventArgs e)
    {
        _loggingService.Debug($"Email_Tapped");

        // not supported in Android!
        //WeakReferenceMessenger.Default.Send(new OpenMailMessage(null));
    }

    private void Donate1Button_Clicked(object sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await _aboutPageViewModel.Donate();
        });
    }
}