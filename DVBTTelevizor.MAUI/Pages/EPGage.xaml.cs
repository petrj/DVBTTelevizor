using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using LoggerService;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Layouts;
using MPEGTS;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using static System.Net.Mime.MediaTypeNames;

namespace DVBTTelevizor.MAUI;

public partial class EPGPage : ContentPage, IOnKeyDown
{
    private EPGPageViewModel _epgPageViewModel;
    private ILoggingService _loggingService;

    private KeyboardFocusableItemList _focusItems;
    private bool IsPortrait { get; set; } = false;
    private Size _lastAllocatedSize = new Size(-1, -1);

    private Rect LandscapeItemsPosition { get; } = new Rect(0.0, 1.0, 0.5, 1.0);
    private Rect LandscapeDetailPosition { get; } = new Rect(1.0, 1.0, 0.5, 1.0);

    private Rect PortraitItemsPosition { get; } = new Rect(0.0, 0.0, 1.0, 0.5);
    private Rect PortraitDetailPosition { get; } = new Rect(1.0, 1.0, 1.0, 0.5);

    public EPGPage(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration, IPublicDirectoryProvider publicDirectoryProvider)
    {
        InitializeComponent();

        _loggingService = loggingService;

        BindingContext = _epgPageViewModel = new EPGPageViewModel(loggingService, driver, tvConfiguration, publicDirectoryProvider);

        BuildFocusableItems();
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        _loggingService.Debug($"OnSizeAllocated: {width}/{height}");

        base.OnSizeAllocated(width, height);

        if (_lastAllocatedSize.Width == width &&
            _lastAllocatedSize.Height == height)
        {
            // no size changed
            return;
        }

        if (width > height)
        {
            IsPortrait = false;
        }
        else
        {
            IsPortrait = true;
        }

        _lastAllocatedSize.Width = width;
        _lastAllocatedSize.Height = height;

        WeakReferenceMessenger.Default.Send(new ChangedSizeMessage(new Size(width, height)));

        RefreshGUI();
    }

    private void RefreshGUI()
    {
        AbsoluteLayout.SetLayoutFlags(EPGRefreshView, AbsoluteLayoutFlags.All);
        AbsoluteLayout.SetLayoutFlags(EPGDetailGrid, AbsoluteLayoutFlags.All);

        if (IsPortrait)
        {
            // Refresh GUI for portrait mode
            AbsoluteLayout.SetLayoutBounds(EPGRefreshView, PortraitItemsPosition);
            AbsoluteLayout.SetLayoutBounds(EPGDetailGrid, PortraitDetailPosition);
        }
        else
        {
            // Refresh GUI for landscape mode
            AbsoluteLayout.SetLayoutBounds(EPGRefreshView, LandscapeItemsPosition);
            AbsoluteLayout.SetLayoutBounds(EPGDetailGrid, LandscapeDetailPosition);
        }

        Task.Run(async () =>
        {
            await _epgPageViewModel.NotifyChange();
        });

    }

    private void BuildFocusableItems()
    {
        _focusItems = new KeyboardFocusableItemList();

        _focusItems
            .AddItem(KeyboardFocusableItem.CreateFrom("EPGListView", new List<View>() { EPGListView }));

        //_focusItems.OnItemFocusedEvent += Page_OnItemFocusedEvent;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _focusItems.DeFocusAll();
        MainPage.SetToolBarColors(Parent as NavigationPage, Colors.White, Color.FromArgb("#29242a"));

        Task.Run(async () =>
        {
            if (_epgPageViewModel?.RefreshCommand?.CanExecute(null) == true)
            {
                await _epgPageViewModel.RefreshCommand.ExecuteAsync(null);
            }
        });

    }

    public Channel? Channel
    {
        get
        {
            return _epgPageViewModel?.Channel;
        }
        set
        {
            if (_epgPageViewModel == null)
                return;

            _epgPageViewModel.Channel = value;
            Title = _epgPageViewModel.Channel?.Name ?? "EPG";

            Task.Run(async () =>
            {
                if (_epgPageViewModel?.RefreshCommand?.CanExecute(null) == true)
                {
                    await _epgPageViewModel.RefreshCommand.ExecuteAsync(null);
                }
            });
        }
    }

    public ObservableCollection<Channel>? Channels
    {
        get
        {
            return _epgPageViewModel?.Channels;
        }
        set
        {
            if (_epgPageViewModel == null)
                return;

            _epgPageViewModel.Channels = value;
        }
    }

    public void OnKeyDown(string key, bool longPress)
    {
        _loggingService.Debug($"EPGPage Page OnKeyDown {key}");

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

        WeakReferenceMessenger.Default.Send(new OpenURLMessage("https://dvbttelevizor.petrjanousek.net/"));
    }

    private void Email_Tapped(object sender, TappedEventArgs e)
    {
        _loggingService.Debug($"Email_Tapped");
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _loggingService.Debug($"OnSelectionChanged");
        if (e.CurrentSelection.Count > 0 && e.CurrentSelection[0] is EventItem eventItem)
        {
            _loggingService.Debug($"Selected event: {eventItem.EventName}");
        }
    }


    private void OnEventItem_Tapped(object sender, TappedEventArgs e)
    {
        _loggingService.Debug($"OnEventItem_Tapped");
        if (e.Parameter is EventItem eventItem)
        {
            _loggingService.Debug($"OnEventItem_Tapped: {eventItem.EventName}");
        }
    }

    private void Icon_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"Icon_Clicked");
    }

}