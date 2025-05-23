using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using LoggerService;
using Microsoft.Maui.Layouts;
using static System.Net.Mime.MediaTypeNames;

namespace DVBTTelevizor.MAUI;

public partial class TuningProgressPage : ContentPage, ITuningPage, IOnKeyDown
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

        _focusItems
            .AddItem(KeyboardFocusableItem.CreateFrom("Back", new List<View>() { BackButton }))
            .AddItem(KeyboardFocusableItem.CreateFrom("Start", new List<View>() { StartButton }))
            .AddItem(KeyboardFocusableItem.CreateFrom("Stop", new List<View>() { StopButton }))
            .AddItem(KeyboardFocusableItem.CreateFrom("Finish", new List<View>() { FinishButton }))
            .AddItem(KeyboardFocusableItem.CreateFrom("ChannelsList", new List<View>() { ChannelsListView }));

        _focusItems.OnItemFocusedEvent += _focusItems_OnItemFocusedEvent;
    }

    private async void _focusItems_OnItemFocusedEvent(KeyboardFocusableItemEventArgs _args)
    {
        if (_args.FocusedItem.Name == "ChannelsList")
        {
            //await _viewModel.SelectChannelsListView(ChannelsListView);
            if (_viewModel.Channels.Count == 0)
            {
                _focusItems.FocusNextItem(true);
            } else
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    _viewModel.SelectFirstChannel();
                    ChannelsListView.ScrollTo(ChannelsListView.SelectedItem, ScrollToPosition.Center, animated: true);
                });
            }
        } else
        {
            if (_viewModel.SelectedChannel != null)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    _viewModel.DeselectAll();
                });
            }
        }
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
            AbsoluteLayout.SetLayoutBounds(FrequencyGrid, new Rect(0.5, 0.0, 0.95, 0.1));
            AbsoluteLayout.SetLayoutBounds(TuneIndicator, new Rect(0.5, 0.1, 0.25, 0.1));
            AbsoluteLayout.SetLayoutBounds(ProgressGrid, new Rect(0.5, 0.15, 0.95, 0.1));
            AbsoluteLayout.SetLayoutBounds(SignalDetailsGrid, new Rect(0.9, 0.275, 0.95, 0.15));
            AbsoluteLayout.SetLayoutBounds(SplitterBoxView, new Rect(0.5, 0.4, 1, 0.005));
            AbsoluteLayout.SetLayoutBounds(TuneResultDetailsGrid, new Rect(0.5, 0.46, 0.95, 0.14));
            AbsoluteLayout.SetLayoutBounds(ButtonsGrid, new Rect(0.05, 0.98, 0.95, 0.1));
            AbsoluteLayout.SetLayoutBounds(ChannelsSplitterGrid, new Rect(0.5, 0.815, 0.95, 0.325));
            AbsoluteLayout.SetLayoutBounds(ChannelsListView, new Rect(0.5, 0.815, 0.95, 0.325));
        } else
        {
            AbsoluteLayout.SetLayoutBounds(FrequencyGrid, new Rect(0.05, 0.00, 0.45, 0.1));
            AbsoluteLayout.SetLayoutBounds(TuneIndicator, new Rect(0.125, 0.15, 0.25, 0.1));
            AbsoluteLayout.SetLayoutBounds(ProgressGrid, new Rect(0.05, 0.25, 0.45, 0.1));
            AbsoluteLayout.SetLayoutBounds(SignalDetailsGrid, new Rect(0.05, 0.4, 0.45, 0.15));
            AbsoluteLayout.SetLayoutBounds(SplitterBoxView, new Rect(0.5, 0.5, 0.005, 1));
            AbsoluteLayout.SetLayoutBounds(TuneResultDetailsGrid, new Rect(0.05, 0.7, 0.45, 0.2));
            AbsoluteLayout.SetLayoutBounds(ButtonsGrid, new Rect(0.05, 0.95, 0.45, 0.1));
            AbsoluteLayout.SetLayoutBounds(ChannelsSplitterGrid, new Rect(1, 0.5, 0.5, 1));
            AbsoluteLayout.SetLayoutBounds(ChannelsListView, new Rect(1.0, 0.5, 0.5, 1));
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        Title = "Tuning".Translated();

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            StartButton_Clicked(this, new EventArgs());
        });

        _focusItems.DeFocusAll();
        MainPage.SetToolBarColors(Parent as NavigationPage, Colors.White, Color.FromArgb("#29242a"));
    }

    private void ActionDown()
    {
        if (_focusItems.FocusedItemName == "ChannelsList")
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                _viewModel.SelectNextChannel();
            });
        } else
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                _focusItems.FocusNextItem(true);
            });
        }
    }

    private void ActionUp()
    {
        if (_focusItems.FocusedItemName == "ChannelsList")
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                _viewModel.SelectPreviousChannel();
            });
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                _focusItems.FocusPreviousItem(true);
            });
        }
    }

    private void ActionRight()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            _focusItems.FocusNextItem(true);
        });
    }

    private void ActionLeft()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            _focusItems.FocusPreviousItem(true);
        });
    }

    public void OnKeyDown(string key, bool longPress)
    {
        _loggingService.Debug($"TuningProgressPage Page OnKeyDown {key}");

        var keyAction = KeyboardDeterminer.GetKeyAction(key);

        switch (keyAction)
        {
            case KeyboardNavigationActionEnum.Right:
                ActionRight();
                break;

            case KeyboardNavigationActionEnum.Down:
                ActionDown();
                break;

            case KeyboardNavigationActionEnum.Up:
                ActionUp();
                break;

            case KeyboardNavigationActionEnum.Left:
                ActionLeft();
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
                        case "Stop":
                            StopButton_Clicked(this,new EventArgs());
                            break;
                        case "Start":
                            StartButton_Clicked(this, new EventArgs());
                            break;
                        case "Finish":
                            FinishButton_Clicked(this, new EventArgs());
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
                _viewModel.ResetTune(true);
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

        _viewModel.ResetTune(true);

        WeakReferenceMessenger.Default.Send(new FinishTuningMessage(String.Empty));
    }

    public void UpdateSettings(TuningSettings tuningSettings)
    {
        _viewModel.Settings = tuningSettings;
        _viewModel.UpdateActualFreq();
    }
}