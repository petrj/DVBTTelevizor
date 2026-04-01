
using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using DVBTTelevizor.TV;
using LoggerService;
using static System.Net.Mime.MediaTypeNames;

namespace DVBTTelevizor.MAUI;

public partial class SelectDriverPage : ContentPage, IOnKeyDown
{
    private TuningSelectDriverPageViewModel _viewModel;

    private ILoggingService _loggingService;
    private IDriverConnector _driver;
    private ITVConfiguration _configuration;
    private string _publicDirectory = "";

    private DriverPage _driverPage = null;


    private KeyboardFocusableItemList _focusItems;

    public SelectDriverPage(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration,  IPublicDirectoryProvider publicDirectoryProvider)
    {
        InitializeComponent();

        _loggingService = loggingService;
        _driver = driver;
        _configuration = tvConfiguration;
        _publicDirectory = publicDirectoryProvider.GetPublicDirectoryPath();

        _driverPage = new DriverPage(_loggingService, _driver, _configuration, publicDirectoryProvider);

        BindingContext = _viewModel = new TuningSelectDriverPageViewModel(loggingService, driver, tvConfiguration, publicDirectoryProvider);

        BuildFocusableItems();

        WeakReferenceMessenger.Default.Register<ShowDriverPageMessage>(this, (r, m) =>
        {
            Task.Run(async () => await MainThread.InvokeOnMainThreadAsync(
                        async () =>await ShowPage(m.Value)
                    ));
        });
    }

    private void BuildFocusableItems()
    {
        _focusItems = new KeyboardFocusableItemList();

        _focusItems
            .AddItem(KeyboardFocusableItem.CreateFrom("DVBT", new List<View>() { DVBTDriverButton }))
            .AddItem(KeyboardFocusableItem.CreateFrom("FM", new List<View>() { SDRDriverButton }));

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
        _loggingService.Debug($"SelectDriverPage OnKeyDown {key}");

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
                            WeakReferenceMessenger.Default.Send(new ShowDriverPageMessage(AppDriverTypeEnum.DVBT));
                            break;
                        case "SDR":
                            WeakReferenceMessenger.Default.Send(new ShowDriverPageMessage(AppDriverTypeEnum.DAB));
                            break;
                    }
                });
                break;
        }
    }

    public void OnTextSent(string text)
    {
        _loggingService.Debug($"SelectDriverPage OnTextSent {text}");
    }

    private async Task ShowPage(AppDriverTypeEnum driverType)
    {
        if (_driverPage.IsLoaded)
        {
            // preventing click when the settings page is just (or yet) loaded
            return;
        }

        _driverPage.PageDriver = driverType;

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await Navigation.PushAsync(_driverPage);
        });
    }
}