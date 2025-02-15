using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using LoggerService;
using Microsoft.Maui.Layouts;
using static System.Net.Mime.MediaTypeNames;

namespace DVBTTelevizor.MAUI;

public partial class TuningProgressPage : ContentPage, IOnKeyDown
{
    private TuningProgressPageViewModel _viewModel;

    public bool Finished { get; set; } = false;

    private Size _lastAllocatedSize = new Size(-1, -1);
    private bool _isPortrait { get; set; } = false;
    private bool? _isPortraitPreviousValue { get; set; } = null;

    private ILoggingService _loggingService;
    private IDriverConnector _driver;
    private IDialogService _dialogService;
    private ITVConfiguration _configuration;
    private string _publicDirectory = "";

    private KeyboardFocusableItemList _focusItems;

    public TuningProgressPage(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration, IDialogService dialogService, IPublicDirectoryProvider publicDirectoryProvider)
    {
        InitializeComponent();

        _loggingService = loggingService;
        _driver = driver;
        _configuration = tvConfiguration;
        _dialogService = dialogService;
        _publicDirectory = publicDirectoryProvider.GetPublicDirectoryPath();

        BindingContext = _viewModel = new TuningProgressPageViewModel(loggingService, driver, tvConfiguration, dialogService, publicDirectoryProvider);

        BuildFocusableItems();

        _viewModel.ChannelFound += ChannelFound;
    }

    public bool DVBTTuning
    {
        get
        {
            return _viewModel == null ? false : _viewModel.DVBTTuning;
        }
        set
        {
            if (_viewModel == null)
                return;

            _viewModel.DVBTTuning = value;
        }
    }

    public bool DVBT2Tuning
    {
        get
        {
            return _viewModel == null ? false : _viewModel.DVBT2Tuning;
        }
        set
        {
            if (_viewModel == null)
                return;

            _viewModel.DVBT2Tuning = value;
        }
    }

    private void ChannelFound(object? sender, EventArgs e)
    {
        if (e is ChannelFoundEventArgs che)
        {
            _loggingService.Info($"Adding new channel: {che.Channel.Name}");
        }
    }

    private void BuildFocusableItems()
    {
        _focusItems = new KeyboardFocusableItemList();

        //_focusItems
        //    .AddItem(KeyboardFocusableItem.CreateFrom("Auto", new List<View>() { AutoScanButton }))
        //    .AddItem(KeyboardFocusableItem.CreateFrom("Manual", new List<View>() { ManualScanButton }))
        //    .AddItem(KeyboardFocusableItem.CreateFrom("Tune", new List<View>() { TuneButton }));

        //_focusItems.OnItemFocusedEvent += Page_OnItemFocusedEvent;
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        if (_lastAllocatedSize.Width == width &&
            _lastAllocatedSize.Height == height)
        {
            // no size changed
            return;
        }

        if (width > height)
        {
            _isPortrait = false;
        }
        else
        {
            _isPortrait = true;
        }

        _lastAllocatedSize.Width = width;
        _lastAllocatedSize.Height = height;

        if (_isPortrait != _isPortraitPreviousValue)
        {
            RefreshGUI();
        }

        _isPortraitPreviousValue = _isPortrait;
    }

    private void RefreshGUI()
    {
        if (_isPortrait)
        {
            AbsoluteLayout.SetLayoutBounds(FrequencyGrid, new Rect(0.5, 0.01, 0.95, 0.1));
            AbsoluteLayout.SetLayoutBounds(TuneIndicator, new Rect(0.5, 0.1, 0.25, 0.1));
            AbsoluteLayout.SetLayoutBounds(ProgressGrid, new Rect(0.5, 0.14, 0.95, 0.1));
            AbsoluteLayout.SetLayoutBounds(SignalGrid, new Rect(0.5, 0.24, 0.95, 0.1));
            AbsoluteLayout.SetLayoutBounds(SignalDetailsGrid, new Rect(0.5, 0.34, 0.9, 0.16));
            AbsoluteLayout.SetLayoutBounds(SplitterBoxView, new Rect(0.5, 0.47, 1, 0.005));
            AbsoluteLayout.SetLayoutBounds(ResultLabel, new Rect(0.5, 0.5, 0.5, 0.1));
            AbsoluteLayout.SetLayoutBounds(TuneResultDetailsGrid, new Rect(0.5, 0.58, 0.8, 0.1));
            AbsoluteLayout.SetLayoutBounds(ButtonsGrid, new Rect(0.05, 0.98, 0.95, 0.1));
            AbsoluteLayout.SetLayoutBounds(ChannelsSplitterGrid, new Rect(0.5, 0.85, 1, 0.25));
            AbsoluteLayout.SetLayoutBounds(ChannelsListView, new Rect(0.5, 0.85, 1, 0.25));
        } else
        {
            AbsoluteLayout.SetLayoutBounds(FrequencyGrid, new Rect(0.125, 0.05, 0.25, 0.1));
            AbsoluteLayout.SetLayoutBounds(TuneIndicator, new Rect(0.125, 0.15, 0.25, 0.1));
            AbsoluteLayout.SetLayoutBounds(ProgressGrid, new Rect(0.1, 0.25, 0.4, 0.1));
            AbsoluteLayout.SetLayoutBounds(SignalGrid, new Rect(0.1, 0.4, 0.4, 0.1));
            AbsoluteLayout.SetLayoutBounds(SignalDetailsGrid, new Rect(0.15, 0.7, 0.3, 0.3));
            AbsoluteLayout.SetLayoutBounds(SplitterBoxView, new Rect(0.5, 0.5, 0.005, 1));
            AbsoluteLayout.SetLayoutBounds(ResultLabel, new Rect(1.0, 0.05, 0.5, 0.1));
            AbsoluteLayout.SetLayoutBounds(TuneResultDetailsGrid, new Rect(0.9, 0.2, 0.4, 0.2));
            AbsoluteLayout.SetLayoutBounds(ButtonsGrid, new Rect(0.05, 0.95, 0.45, 0.1));
            AbsoluteLayout.SetLayoutBounds(ChannelsSplitterGrid, new Rect(1, 1, 0.5, 0.6));
            AbsoluteLayout.SetLayoutBounds(ChannelsListView, new Rect(1, 1, 0.5, 0.6));
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _focusItems.DeFocusAll();
        MainPage.SetToolBarColors(Parent as NavigationPage, Colors.White, Color.FromArgb("#29242a"));
    }

    public void OnKeyDown(string key, bool longPress)
    {
        _loggingService.Debug($"TuningProgressPage Page OnKeyDown {key}");

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
                    //await Navigation.PopAsync();
                });
                break;

            case KeyboardNavigationActionEnum.OK:

                if (_focusItems.FocusedItem == null)
                    return;

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    switch (_focusItems.FocusedItem.Name)
                    {
                        case "ABC":
                            break;
                    }
                });
                break;
        }
    }

    public void OnTextSent(string text)
    {
        _loggingService.Debug($"TuningProgressPage OnTextSent {text}");
    }

    private async void StartButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"TuningProgressPage StartButton_Clicked");

        if (_viewModel.State == TuningProgressPageViewModel.TuneStateEnum.Stopped)
        {
            if (!await _dialogService.Confirm(
                "Tuning is in progress".Translated(),
                "Start tuning".Translated(),
                "Continue".Translated(),
                "Start from beginning".Translated()))
            {
                _viewModel.RestartTune();
            }
        }

        _viewModel.StartTune();
    }

    private void StopButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"TuningProgressPage StopButton_Clicked");

        _viewModel.StopTune();
    }

    private void ContinueButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"TuningProgressPage ContinueButton_Clicked");

        _viewModel.StartTune();
    }

    private void BackButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"TuningProgressPage BackButton_Clicked");

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Navigation.PopAsync();
        });
    }

    private async void FinishButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"TuningProgressPage FinishButton_Clicked");

        WeakReferenceMessenger.Default.Send(new FinishTuningMessage(String.Empty));
    }
}