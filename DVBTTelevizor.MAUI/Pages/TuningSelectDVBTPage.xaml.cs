using LoggerService;
using Microsoft.Maui;
using static System.Net.Mime.MediaTypeNames;

namespace DVBTTelevizor.MAUI;

public partial class TuningSelectDVBTPage : ContentPage, IOnKeyDown
{
    private TuningSelectDVBTPageViewModel _driverPageViewModel;

    private ILoggingService _loggingService;
    private IDriverConnector _driver;
    private IDialogService _dialogService;
    private ITVCConfiguration _configuration;
    private string _publicDirectory = "";

    private KeyboardFocusableItemList _focusItems;

    private string? _lastSelectedCenterItem = null;

    public bool Finished { get; set; } = false;

    private NavigationPage _tuningProgressPage;

    public TuningSelectDVBTPage(ILoggingService loggingService, IDriverConnector driver, ITVCConfiguration tvConfiguration, IDialogService dialogService, IPublicDirectoryProvider publicDirectoryProvider)
    {
        InitializeComponent();

        _loggingService = loggingService;
        _driver = driver;
        _configuration = tvConfiguration;
        _dialogService = dialogService;
        _publicDirectory = publicDirectoryProvider.GetPublicDirectoryPath();

        BindingContext = _driverPageViewModel = new TuningSelectDVBTPageViewModel(loggingService, driver, tvConfiguration, dialogService, publicDirectoryProvider);

        _tuningProgressPage = new NavigationPage(new TuningProgressPage(loggingService, driver, tvConfiguration, dialogService, publicDirectoryProvider));

        _tuningProgressPage.Disappearing += delegate
            {
                _loggingService.Info($"_tuningProgressPage Disappearing");
                var nextPage = (_tuningProgressPage.RootPage as TuningProgressPage);

                if (nextPage.Finished)
                {
                    Finished = true;
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        _loggingService.Info($"Calling PopAsync");
                        await Navigation.PopAsync();
                    });
                    /*
                    Task.Run(async () =>
                    {
                        var stack = Navigation.NavigationStack;

                        var timeout = 5;
                        var actTime = 0;
                        while (actTime < timeout)
                        {
                            var pageonTop = stack[stack.Count - 1];
                            _loggingService.Info($"Page on top: {pageonTop}");

                            actTime++;
                            await Task.Delay(1000);
                        }
                        */
                }
            };

        BuildFocusableItems();
    }

    private void BuildFocusableItems()
    {
        _focusItems = new KeyboardFocusableItemList();

        _focusItems
            .AddItem(KeyboardFocusableItem.CreateFrom("DVBT", new List<View>() { DVBTBoxView, DVBTSwitch }))
            .AddItem(KeyboardFocusableItem.CreateFrom("DVBT2", new List<View>() { DVBT2BoxView, DVBT2Switch }))
            .AddItem(KeyboardFocusableItem.CreateFrom("Bandwidth", new List<View>() { BandwidthBoxView, BandwidthPicker }))
            .AddItem(KeyboardFocusableItem.CreateFrom("Back", new List<View>() { BackButton }))
            .AddItem(KeyboardFocusableItem.CreateFrom("Next", new List<View>() { NextButton }));

        _focusItems.OnItemFocusedEvent += _focusItems_OnItemFocusedEvent;
    }

    private void _focusItems_OnItemFocusedEvent(KeyboardFocusableItemEventArgs _args)
    {
        if (_focusItems.FocusedItem == null)
        {
            return;
        }

        if (_focusItems.FocusedItem.Name == "DVBT")
        {
            _lastSelectedCenterItem = "DVBT";
        }
        else
        if (_focusItems.FocusedItem.Name == "DVBT2")
        {
            _lastSelectedCenterItem = "DVBT2";
        }
        else
        if (_focusItems.FocusedItem.Name == "Bandwidth")
        {
            _lastSelectedCenterItem = "Bandwidth";
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _driverPageViewModel.FillBandwidths();

        _focusItems.DeFocusAll();
        MainPage.SetToolBarColors(Parent as NavigationPage, Colors.White, Color.FromArgb("#29242a"));
    }

    public void OnKeyDown(string key, bool longPress)
    {
        _loggingService.Debug($"TuningSelectDVBTPage Page OnKeyDown {key}");

        var keyAction = KeyboardDeterminer.GetKeyAction(key);

        switch (keyAction)
        {
            case KeyboardNavigationActionEnum.Down:
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    _focusItems.FocusNextItem();
                });
                break;

            case KeyboardNavigationActionEnum.Right:
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    Right();
                });
                break;

            case KeyboardNavigationActionEnum.Up:
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    _focusItems.FocusPreviousItem();
                });
                break;

            case KeyboardNavigationActionEnum.Left:
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    Left();
                });
                break;

            case KeyboardNavigationActionEnum.Back:
                BackButton_Clicked(this, new EventArgs());
                break;

            case KeyboardNavigationActionEnum.OK:

                if (_focusItems.FocusedItem == null)
                    return;

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    switch (_focusItems.FocusedItem.Name)
                    {
                        case "DVBT":
                            DVBTSwitch.IsToggled = !DVBTSwitch.IsToggled;
                            break;
                        case "DVBT2":
                            DVBT2Switch.IsToggled = !DVBT2Switch.IsToggled;
                            break;
                        case "Bandwidth":
                            BandwidthPicker.Focus();
                            break;
                        case "Back":
                            BackButton_Clicked(this, new EventArgs());
                            break;
                        case "Next":
                            NextButton_Clicked(this, new EventArgs());
                            break;
                    }
                });
                break;
        }
    }

    private void Right()
    {
        if (_focusItems.FocusedItem == null)
        {
            _focusItems.FocusItem("Next", KeyboardFocusDirection.Next);
            return;
        }

        switch (_focusItems.FocusedItem.Name)
        {
            case "DVBT":
            case "DVBT2":
            case "Bandwidth":
                _focusItems.FocusItem("Next", KeyboardFocusDirection.Next);
                break;
            case "Back":
                _focusItems.FocusItem(_lastSelectedCenterItem == null ? "DVBT" : _lastSelectedCenterItem, KeyboardFocusDirection.Next);
                break;
            case "Next":
                _focusItems.FocusItem("Back", KeyboardFocusDirection.Next);
                break;
        }
    }

    private void Left()
    {
        if (_focusItems.FocusedItem == null)
        {
            _focusItems.FocusItem("Back", KeyboardFocusDirection.Next);
            return;
        }

        switch (_focusItems.FocusedItem.Name)
        {
            case "DVBT":
            case "DVBT2":
            case "Bandwidth":
                _focusItems.FocusItem("Back", KeyboardFocusDirection.Next);
                break;
            case "Back":
                _focusItems.FocusItem("Next", KeyboardFocusDirection.Next);
                break;
            case "Next":
                _focusItems.FocusItem(_lastSelectedCenterItem == null ? "DVBT" : _lastSelectedCenterItem, KeyboardFocusDirection.Next);
                break;
        }
    }

    public void OnTextSent(string text)
    {
        _loggingService.Debug($"TuningSelectDVBTPage Page OnTextSent {text}");
    }

    private void BackButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"TuningSelectDVBTPage Page BackButton_Clicked");

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Navigation.PopAsync();
        });
    }

    private async void NextButton_Clicked(object sender, EventArgs e)
    {
        if (_tuningProgressPage.IsLoaded)
        {
            // preventing click when the settings page is just (or yet) loaded
            return;
        }

        await Navigation.PushAsync(_tuningProgressPage);
    }
}