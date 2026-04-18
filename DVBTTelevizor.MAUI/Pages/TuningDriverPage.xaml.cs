
using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using DVBTTelevizor.TV;
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
    private TuningFrequencyPage _tuningFrequencyPage;

    public TuningDriverPage(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration,  IPublicDirectoryProvider publicDirectoryProvider)
    {
        InitializeComponent();

        _loggingService = loggingService;
        _driver = driver;
        _configuration = tvConfiguration;
        _publicDirectory = publicDirectoryProvider.GetPublicDirectoryPath();

        _tuningSettings = new TuningSettings(_loggingService);

        BindingContext = _viewModel = new TuningSelectDriverPageViewModel(loggingService, driver, tvConfiguration, publicDirectoryProvider);

        _tuningModePage = new TuningModePage(loggingService, driver, tvConfiguration, publicDirectoryProvider);
        _tuningFrequencyPage = new TuningFrequencyPage(loggingService, driver, tvConfiguration, publicDirectoryProvider);


        WeakReferenceMessenger.Default.Register<ShowDriverPageMessage>(this, (r, m) =>
        {
            switch(m.Value)
            {
                case AppDriverTypeEnum.DVBT:
                    DVBTButton_Clicked(this, new EventArgs());
                    break;
                case AppDriverTypeEnum.FM:
                    FMButton_Clicked(this, new EventArgs());
                    break;
                case AppDriverTypeEnum.DAB:
                    DABButton_Clicked(this, new EventArgs());
                    break;
            }

        });


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

        //Task.Run(async () =>
        //{
        //    _viewModel.UpdateActiveDriverType();
        //    _viewModel?.Settings.LoadFromConfiguration(_configuration);
        //    await _viewModel?.Settings.SetFrequencies(_driver);
        //    //_tuningSettings.SaveToConfiguration(_configuration);
        //});
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

        await ShowPage(_tuningModePage, AppDriverTypeEnum.DVBT);
    }

    private async void FMButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"TuningDriverPage: FMButton_Clicked");

        await ShowPage(_tuningFrequencyPage, AppDriverTypeEnum.FM);
    }

    private async void DABButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"TuningDriverPage:     private void DABButton_Clicked(object sender, EventArgs e)\r\n");

        await ShowPage(_tuningFrequencyPage, AppDriverTypeEnum.DAB);
    }

    private async Task ShowPage(Page page, AppDriverTypeEnum driverType)
    {
        if (page.IsLoaded)
        {
            // preventing click when the settings page is just (or yet) loaded
            return;
        }

        _configuration.AppDriverType = driverType;
        // update settings according to selected driver
        _tuningSettings.LoadFromConfiguration(_configuration);

        // update frequencies according to driver
        await _tuningSettings.SetFrequencies(_driver);

        _tuningSettings.DVBT = false;
        _tuningSettings.DVBT2 = false;
        _tuningSettings.DAB = false;
        _tuningSettings.FM = false;

        switch (driverType)
        {
            case AppDriverTypeEnum.DVBT:
                _tuningSettings.DVBT = true;
                _tuningSettings.DVBT2 = true;
                break;
            case AppDriverTypeEnum.FM:
                _tuningSettings.FM = true;
                _tuningSettings.TuningMode = TuneModeEnum.Frequency;
                break;
            case AppDriverTypeEnum.DAB:
                _tuningSettings.DAB = true;
                _tuningSettings.TuningMode = TuneModeEnum.Frequency;
                break;
        }

        if (page is ITuningPage tuPage)
        {
            tuPage.UpdateSettings(_tuningSettings);
        }

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await Navigation.PushAsync(page);
        });
    }
}