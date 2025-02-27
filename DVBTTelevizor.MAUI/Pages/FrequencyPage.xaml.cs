using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using LoggerService;
using Microsoft.Maui.Layouts;
using static System.Net.Mime.MediaTypeNames;

namespace DVBTTelevizor.MAUI;

public partial class FrequencyPage : ContentPage, IOnKeyDown
{
    private FrequencyPageViewModel _viewModel;

    private ILoggingService _loggingService;
    private IDriverConnector _driver;
    private IDialogService _dialogService;
    private ITVConfiguration _configuration;
    private string _publicDirectory = "";

    private KeyboardFocusableItemList _focusItems;

    public FrequencyPage(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration, IDialogService dialogService, IPublicDirectoryProvider publicDirectoryProvider)
    {
        InitializeComponent();

        _loggingService = loggingService;
        _driver = driver;
        _configuration = tvConfiguration;
        _dialogService = dialogService;
        _publicDirectory = publicDirectoryProvider.GetPublicDirectoryPath();

        BindingContext = _viewModel = new FrequencyPageViewModel(loggingService, driver, tvConfiguration, dialogService, publicDirectoryProvider);

        KHZEntry.Focused += KHZEntry_Focused;
        KHZEntry.Unfocused += KHZEntry_Unfocused;

        MHZEntry.Focused += MHZEntry_Focused;
        MHZEntry.Unfocused += MHZEntry_Unfocused;

        BuildFocusableItems();
    }

    private void MHZEntry_Focused(object? sender, FocusEventArgs e)
    {
        _viewModel.NotifyEnabled = false;
    }

    private void KHZEntry_Focused(object? sender, FocusEventArgs e)
    {
        _viewModel.NotifyEnabled = false;
    }

    private async void MHZEntry_Unfocused(object? sender, FocusEventArgs e)
    {
        _viewModel.NotifyEnabled = true;

        float mhz;
        if (!float.TryParse(MHZEntry.Text, out mhz))
        {
            await _dialogService.Information($"Invalid frequency");
            _viewModel.FrequencyKHz = _viewModel.Settings.DefaultFrequencyKHz;
            return;
        }

        if (!_viewModel.ValidFrequencyMHz(mhz))
        {
            await _dialogService.Information($"Frequency \"{mhz}\" MHz is out of range {_viewModel.FrequencyMinMHz} MHz - {_viewModel.FrequencyMaxMHz} MHz");
            _viewModel.FrequencyKHz = _viewModel.Settings.DefaultFrequencyKHz;
            return;
        }

        _viewModel.FrequencyKHz = Convert.ToInt64(mhz * 1000);
    }

    private async void KHZEntry_Unfocused(object? sender, FocusEventArgs e)
    {
        _viewModel.NotifyEnabled = true;
        int khz;
        if (!int.TryParse(KHZEntry.Text, out khz))
        {
            await _dialogService.Information($"Invalid frequency");
            _viewModel.FrequencyKHz = _viewModel.Settings.DefaultFrequencyKHz;
            return;
        }

        if (!_viewModel.ValidFrequencyKHz(khz))
        {
            await _dialogService.Information($"Frequency \"{khz}\" KHz is out of range {_viewModel.FrequencyMinKHz} KHz - {_viewModel.FrequencyMaxKHz} KHz");
            _viewModel.FrequencyKHz = _viewModel.Settings.DefaultFrequencyKHz;
            return;
        }

        _viewModel.FrequencyKHz = khz;
    }

    public TuningSettings? Settings
    {
        get
        {
            return _viewModel?.Settings;
        }
        set
        {
            _viewModel.Settings = value;
        }
    }

    public void NotifyChange()
    {
        _viewModel?.NotifyChange();
    }

    private void BuildFocusableItems()
    {
        _focusItems = new KeyboardFocusableItemList();

        /*
        _focusItems
            .AddItem(KeyboardFocusableItem.CreateFrom("Back", new List<View>() { BackButton }))
            .AddItem(KeyboardFocusableItem.CreateFrom("Start", new List<View>() { StartButton }))
            .AddItem(KeyboardFocusableItem.CreateFrom("Stop", new List<View>() { StopButton }))
            .AddItem(KeyboardFocusableItem.CreateFrom("Finish", new List<View>() { FinishButton }));
        */

        //_focusItems.OnItemFocusedEvent += Page_OnItemFocusedEvent;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        Title = "Frequency".Translated();

        _focusItems.DeFocusAll();
        MainPage.SetToolBarColors(Parent as NavigationPage, Colors.White, Color.FromArgb("#29242a"));
    }

    public void OnKeyDown(string key, bool longPress)
    {
        _loggingService.Debug($"FrequencyPage Page OnKeyDown {key}");

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
                            MainThread.BeginInvokeOnMainThread(async () =>
                            {
                                await Navigation.PopAsync();
                            });
                            break;
                    }
                });
                break;
        }
    }

    public void OnTextSent(string text)
    {
        _loggingService.Debug($"FrequencyPage OnTextSent {text}");
    }

    private void BackButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"FrequencyPage BackButton_Clicked");

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Navigation.PopAsync();
        });
    }

    private void SliderFrequency_ValueChanged(object sender, ValueChangedEventArgs e)
    {
        _loggingService.Debug($"FrequencyPage SliderFrequency_ValueChanged");
    }

    private void LeftButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"FrequencyPage LeftButton_Clicked");
    }

    private void RightButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"FrequencyPage RightButton_Clicked");
    }
}