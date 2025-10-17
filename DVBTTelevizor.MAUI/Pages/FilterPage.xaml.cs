using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using LoggerService;
using Microsoft.Maui.Controls.PlatformConfiguration;
using System.Linq.Expressions;
using static System.Net.Mime.MediaTypeNames;

namespace DVBTTelevizor.MAUI;

public partial class FilterPage : ContentPage, IOnKeyDown
{
    private FilterPageViewModel _filterPageViewModel;

    private ILoggingService _loggingService;
    private IDriverConnector _driver;
    private ITVConfiguration _configuration;
    private string _publicDirectory = "";

    private KeyboardFocusableItemList _focusItems;

    public FilterPage(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration, IPublicDirectoryProvider publicDirectoryProvider)
    {
        InitializeComponent();

        _loggingService = loggingService;
        _driver = driver;
        _configuration = tvConfiguration;
        _publicDirectory = publicDirectoryProvider.GetPublicDirectoryPath();

        BindingContext = _filterPageViewModel = new FilterPageViewModel(loggingService, driver, tvConfiguration, publicDirectoryProvider);

        BuildFocusableItems();
    }

    private void BuildFocusableItems()
    {
        _focusItems = new KeyboardFocusableItemList();

        _focusItems
            .AddItem(KeyboardFocusableItem.CreateFrom("ShowTVChannels", new List<View>() { ShowTVChannelsBoxView, ShowTVSwitch }))
            .AddItem(KeyboardFocusableItem.CreateFrom("ShowRadioChannels", new List<View>() { ShowRadioChannelsBoxView, ShowRadioSwitch }))
            .AddItem(KeyboardFocusableItem.CreateFrom("ShowNonFreeChannels", new List<View>() { ShowNonFreeChannelsBoxView, ShowNonFreeSwitch }))
            .AddItem(KeyboardFocusableItem.CreateFrom("ShowOtherChannels", new List<View>() { ShowOtherChannelsBoxView, ShowOtherSwitch }))
            .AddItem(KeyboardFocusableItem.CreateFrom("Multiplexes", new List<View>() { MultiplexesStackLayout }))

            ;

        _focusItems.OnItemFocusedEvent += _focusItems_OnItemFocusedEvent;
    }

    private void _focusItems_OnItemFocusedEvent(KeyboardFocusableItemEventArgs _args)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (_focusItems.FocusedItemName == "Multiplexes")
            {
                if (_focusItems.LastFocusDirection == KeyboardFocusDirection.Previous)
                {
                    _filterPageViewModel.SelectNext(true);
                } else
                {
                    _filterPageViewModel.SelectNext();
                }
            }
            else
            {
                _filterPageViewModel.DeSelectAll(true);
            }
        });
    }

    protected override void OnDisappearing()
    {
        _filterPageViewModel.UpdateFilter();

        base.OnDisappearing();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            _filterPageViewModel.FillMultiplexes();
        });
        _focusItems.DeFocusAll();
        MainPage.SetToolBarColors(Parent as NavigationPage, Colors.White, Color.FromArgb("#29242a"));
    }

    public void OnKeyDown(string key, bool longPress)
    {
        _loggingService.Debug($"FilterPage Page OnKeyDown {key}");

        var keyAction = KeyboardDeterminer.GetKeyAction(key);

        switch (keyAction)
        {
            case KeyboardNavigationActionEnum.Down:
            case KeyboardNavigationActionEnum.Right:
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    if (_focusItems.FocusedItemName == "Multiplexes")
                    {
                        if (_filterPageViewModel.SelectedLast())
                        {
                            _focusItems.FocusNextItem(true);
                        }
                        else
                        {
                            _filterPageViewModel.SelectNext();
                        }
                    }
                    else
                    {
                        _focusItems.FocusNextItem(true);
                    }
                });
                break;

            case KeyboardNavigationActionEnum.Up:
            case KeyboardNavigationActionEnum.Left:
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    if (_focusItems.FocusedItemName == "Multiplexes")
                    {
                        if (_filterPageViewModel.SelectedFirst())
                        {
                            _focusItems.FocusPreviousItem(true);
                        }
                        else
                        {
                            _filterPageViewModel.SelectNext(true);
                        }
                    } else
                    {
                        _focusItems.FocusPreviousItem(true);
                    }
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
                        case "ShowTVChannels":
                            ShowTVSwitch.IsToggled = !ShowTVSwitch.IsToggled;
                            break;

                        case "ShowRadioChannels":
                            ShowRadioSwitch.IsToggled = !ShowRadioSwitch.IsToggled;
                            break;

                        case "ShowNonFreeChannels":
                            ShowNonFreeSwitch.IsToggled = !ShowNonFreeSwitch.IsToggled;
                            break;

                        case "ShowOtherChannels":
                            ShowOtherSwitch.IsToggled = !ShowOtherSwitch.IsToggled;
                            break;

                        case "Multiplexes":
                            OnKeyBoardMultiplexToggled();
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

        WeakReferenceMessenger.Default.Send(new OpenURLMessage("https://www.dvbttelevizor.petrjanousek.net/"));
    }

    private void Email_Tapped(object sender, TappedEventArgs e)
    {
        _loggingService.Debug($"Email_Tapped");

        // not supported in Android!
        //WeakReferenceMessenger.Default.Send(new OpenMailMessage(null));
    }

    private void Switch_Toggled(object sender, ToggledEventArgs e)
    {
        _filterPageViewModel.UpdateFilter();
    }

    public void OnKeyBoardMultiplexToggled()
    {
        _filterPageViewModel.UpdateFilter();
    }
}