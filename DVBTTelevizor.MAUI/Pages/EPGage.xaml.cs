using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using LoggerService;
using Microsoft.Maui.Controls.PlatformConfiguration;
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

    public EPGPage(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration, IPublicDirectoryProvider publicDirectoryProvider)
    {
        InitializeComponent();

        _loggingService = loggingService;

        BindingContext = _epgPageViewModel = new EPGPageViewModel(loggingService, driver, tvConfiguration, publicDirectoryProvider);

        BuildFocusableItems();
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
            await _epgPageViewModel.Refresh();
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
                await _epgPageViewModel.Refresh();
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

    private void EPGListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
    {
        _loggingService.Debug($"EPGListView_ItemSelected");
    }


    private void OnEventItem_Tapped(object sender, TappedEventArgs e)
    {
        _loggingService.Debug($"OnEventItem_Tapped");
        if (e.Parameter is EventItem eventItem)
        {
            _loggingService.Debug($"OnEventItem_Tapped: {eventItem.EventName}");
        }
    }

}