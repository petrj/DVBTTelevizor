using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using LoggerService;
using Microsoft.Maui.Layouts;
using static System.Net.Mime.MediaTypeNames;

namespace DVBTTelevizor.MAUI;

public partial class FrequencyPage : ContentPage, ITuningPage, IOnKeyDown
{
    private FrequencyPageViewModel _viewModel;

    private ILoggingService _loggingService;
    private IDriverConnector _driver;
    private ITVConfiguration _configuration;
    private string _publicDirectory = "";

    public TuneFrequencyModeEnum TuneFrequencyMode { get; set; } = TuneFrequencyModeEnum.Center;

    private KeyboardFocusableItemList _focusItems;

    public bool Confirmed { get; set; } = false;

    public FrequencyPage(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration, IPublicDirectoryProvider publicDirectoryProvider)
    {
        InitializeComponent();

        _loggingService = loggingService;
        _driver = driver;
        _configuration = tvConfiguration;
        _publicDirectory = publicDirectoryProvider.GetPublicDirectoryPath();

        BindingContext = _viewModel = new FrequencyPageViewModel(loggingService, driver, tvConfiguration, publicDirectoryProvider);

        KHZEntry.Focused += KHZEntry_Focused;
        KHZEntry.Unfocused += KHZEntry_Unfocused;

        MHZEntry.Focused += MHZEntry_Focused;
        MHZEntry.Unfocused += MHZEntry_Unfocused;

        SliderFrequency.DragCompleted += SliderFrequency_DragCompleted;

        BuildFocusableItems();
    }

    private void SliderFrequency_DragCompleted(object? sender, EventArgs e)
    {
        _viewModel.RoundFrequency();
        _viewModel.NotifyChange();
    }

    private void SliderFrequency_Unfocused(object? sender, FocusEventArgs e)
    {

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
            WeakReferenceMessenger.Default.Send(new ToastMessage("Invalid frequency".Translated()));
            _viewModel.Settings.FrequencyKHz = _viewModel.Settings.DeviceFrequencyMinKHz;
            return;
        }

        if (!_viewModel.Settings.ValidFrequency(Convert.ToInt64(mhz*1000.0), true))
        {
            WeakReferenceMessenger.Default.Send(new ToastMessage($"Frequency \"{0}\" MHz is out of range {1} MHz - {2} MHz".Translated(mhz.ToString(), _viewModel.FrequencyMinMHz.ToString(), _viewModel.FrequencyMaxMHz.ToString())));
            _viewModel.Settings.FrequencyKHz = _viewModel.Settings.DeviceFrequencyMinKHz;
            return;
        }

        _viewModel.Settings.FrequencyKHz = Convert.ToInt64(mhz * 1000);
    }

    private async void KHZEntry_Unfocused(object? sender, FocusEventArgs e)
    {
        _viewModel.NotifyEnabled = true;
        int khz;
        if (!int.TryParse(KHZEntry.Text, out khz))
        {
            WeakReferenceMessenger.Default.Send(new ToastMessage("Invalid frequency".Translated()));
            _viewModel.Settings.FrequencyKHz = _viewModel.Settings.DeviceFrequencyMinKHz;
            return;
        }

        if (!_viewModel.Settings.ValidFrequency(khz, true))
        {
            WeakReferenceMessenger.Default.Send(new ToastMessage($"Frequency \"{0}\" MHz is out of range {1} MHz - {2} MHz".Translated(khz.ToString(), _viewModel.FrequencyMinKHz.ToString(), _viewModel.FrequencyMaxKHz.ToString())));
            _viewModel.Settings.FrequencyKHz = _viewModel.Settings.DeviceFrequencyMinKHz;
            return;
        }

        _viewModel.Settings.FrequencyKHz = khz;
    }

    public TuningSettings? Settings
    {
        get
        {
            return _viewModel?.Settings;
        }
    }

    public void NotifyChange()
    {
        _viewModel?.NotifyChange();
    }

    private void BuildFocusableItems()
    {
        _focusItems = new KeyboardFocusableItemList();

        _focusItems
            .AddItem(KeyboardFocusableItem.CreateFrom("Left", new List<View>() { LeftButton }))
            .AddItem(KeyboardFocusableItem.CreateFrom("Right", new List<View>() { RightButton }))
            .AddItem(KeyboardFocusableItem.CreateFrom("KHz", new List<View>() { KHzEntryBoxView, KHZEntry }))
            .AddItem(KeyboardFocusableItem.CreateFrom("MHz", new List<View>() { MHzEntryBoxView , MHZEntry}))
            .AddItem(KeyboardFocusableItem.CreateFrom("Back", new List<View>() { BackButton }))
            .AddItem(KeyboardFocusableItem.CreateFrom("Default", new List<View>() { DefaultButton }))
            .AddItem(KeyboardFocusableItem.CreateFrom("Confirm", new List<View>() { ConfirmButton }));

        //_focusItems.OnItemFocusedEvent += Page_OnItemFocusedEvent;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        var title = "Frequency".Translated();
        switch (TuneFrequencyMode)
        {
            case TuneFrequencyModeEnum.From:
                title += " " + "from".Translated();
                break;
            case TuneFrequencyModeEnum.To:
                title += " " + "to".Translated();
                break;
        }

        Title = title;

        _focusItems.DeFocusAll();
        MainPage.SetToolBarColors(Parent as NavigationPage, Colors.White, Color.FromArgb("#29242a"));

        NotifyChange();
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
                        case "Left":
                            LeftButton_Clicked(this, new EventArgs());
                            break;
                        case "Right":
                            RightButton_Clicked(this, new EventArgs());
                            break;
                        case "KHz":
                            KHZEntry.Focus();
                            break;
                        case "MHz":
                            MHZEntry.Focus();
                            break;
                        case "Back":
                            BackButton_Clicked(this, new EventArgs());
                            break;
                        case "Default":
                            DefaultButton_Clicked(this, new EventArgs());
                            break;
                        case "Confirm":
                            ConfirmButton_Clicked(this, new EventArgs());
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

    private void SliderFrequency_ValueChanged(object sender, ValueChangedEventArgs e)
    {
        _loggingService.Debug($"FrequencyPage SliderFrequency_ValueChanged");
    }

    private void BackButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"FrequencyPage BackButton_Clicked");

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Navigation.PopAsync();
        });
    }

    private void LeftButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"FrequencyPage LeftButton_Clicked");

        _viewModel.DecreaseFreq();
        _viewModel.RoundFrequency();
    }

    private void RightButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"FrequencyPage RightButton_Clicked");

        _viewModel.IncreaseFreq();
        _viewModel.RoundFrequency();
    }

    private void DefaultButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"FrequencyPage DefaultButton_Clicked");

        _viewModel.SetDefaultFrequency(TuneFrequencyMode);
    }

    public void UpdateSettings(TuningSettings tuningSettings)
    {
        _viewModel.Settings = tuningSettings;
    }

    private void ConfirmButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"FrequencyPage ConfirmButton_Clicked");

        Confirmed = true;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Navigation.PopAsync();
        });
    }
}