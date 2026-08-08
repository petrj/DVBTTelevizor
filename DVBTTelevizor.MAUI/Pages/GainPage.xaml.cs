using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using LoggerService;
using Microsoft.Maui.Layouts;
using static System.Net.Mime.MediaTypeNames;

namespace DVBTTelevizor.MAUI;

public partial class GainPage : ContentPage, IOnKeyDown
{
    private GainPageViewModel _viewModel;

    private ILoggingService _loggingService;
    private IDriverConnector _driver;
    private ITVConfiguration _configuration;
    private string _publicDirectory = "";


    public TuneFrequencyModeEnum TuneFrequencyMode { get; set; } = TuneFrequencyModeEnum.Center;

    private KeyboardFocusableItemList _focusItems;

    public bool Confirmed { get; set; } = false;

    public GainPage(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration, IPublicDirectoryProvider publicDirectoryProvider)
    {
        InitializeComponent();

        _loggingService = loggingService;
        _driver = driver;
        _configuration = tvConfiguration;
        _publicDirectory = publicDirectoryProvider.GetPublicDirectoryPath();

        BindingContext = _viewModel = new GainPageViewModel(loggingService, driver, tvConfiguration, publicDirectoryProvider);

        BuildFocusableItems();
    }

    private void LeftButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"GainPage LeftButton_Clicked");

        if (_configuration == null)
            return;

        _configuration.GainValue -= 10;

        if (_configuration.GainValue  < _viewModel.GainMin)
        {
            _configuration.GainValue = _viewModel.GainMin;
        }

        _viewModel.NotifyChange();
    }

    private void RightButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"GainPage RightButton_Clicked");

        if (_configuration == null)
            return;

        _configuration.GainValue += 10;

        if (_configuration.GainValue > _viewModel.GainMax)
        {
            _configuration.GainValue = _viewModel.GainMax;
        }

        _viewModel.NotifyChange();
    }

    private void SliderFrequency_ValueChanged(object sender, ValueChangedEventArgs e)
    {
        _loggingService.Debug($"GainPage SliderFrequency_ValueChanged");

        //if (_configuration == null)
        //    return;

        //_configuration.GainValue = _viewModel.GainValue;

        //_viewModel.NotifyChange();
    }

    public void NotifyChange()
    {
        _viewModel?.NotifyChange();
    }

    private void BuildFocusableItems()
    {
        _focusItems = new KeyboardFocusableItemList();

        _focusItems
            .AddItem(KeyboardFocusableItem.CreateFrom("HW", new List<View>() { HWBoxView }))
            .AddItem(KeyboardFocusableItem.CreateFrom("Auto", new List<View>() { SWBoxView }))
            .AddItem(KeyboardFocusableItem.CreateFrom("Manual", new List<View>() { ManualBoxView }))
            .AddItem(KeyboardFocusableItem.CreateFrom("Left", new List<View>() { LeftButton }))
            .AddItem(KeyboardFocusableItem.CreateFrom("Right", new List<View>() { RightButton }));

        //_focusItems.OnItemFocusedEvent += Page_OnItemFocusedEvent;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _focusItems.DeFocusAll();
        MainPage.SetToolBarColors(Parent as NavigationPage, Colors.White, Color.FromArgb("#29242a"));

        NotifyChange();
    }

    public void OnKeyDown(string key, bool longPress)
    {
        _loggingService.Debug($"GainPage Page OnKeyDown {key}");

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
                        case "Back":
                            BackButton_Clicked(this, new EventArgs());
                            break;
                        case "Default":
                            DefaultButton_Clicked(this, new EventArgs());
                            break;
                        case "Confirm":
                            ConfirmButton_Clicked(this, new EventArgs());
                            break;

                        case "HW":
                            RadioButtonHW.IsChecked = !RadioButtonHW.IsChecked;
                            break;
                        case "Auto":
                            RadioButtonSW.IsChecked = !RadioButtonSW.IsChecked;
                            break;
                        case "Manual":
                            RadioButtonManual.IsChecked = !RadioButtonManual.IsChecked;
                            break;

                        case "Left":
                            LeftButton_Clicked(this, new EventArgs());
                            break;
                        case "Right":
                            RightButton_Clicked(this, new EventArgs());
                            break;
                    }
                });
                break;
        }
    }


    private void BackButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"GainPage BackButton_Clicked");

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Navigation.PopAsync();
        });
    }

    private void DefaultButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"GainPage DefaultButton_Clicked");
    }

    private void ConfirmButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"GainPage ConfirmButton_Clicked");

        Confirmed = true;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Navigation.PopAsync();
        });
    }

    public void OnTextSent(string text)
    {

    }
}