using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.DBManager;
using DVBTTelevizor.MAUI.Messages;
using LoggerService;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Layouts;
using MPEGTS;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using static System.Net.Mime.MediaTypeNames;

namespace DVBTTelevizor.MAUI;


public enum EPGPageActiveItemEnum
{
    List,
    EPG,
    LeftButton,
    RightButton
}

public partial class EPGPage : ContentPage, IOnKeyDown
{
    private EPGPageViewModel _epgPageViewModel;
    private ILoggingService _loggingService;

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

    protected override void OnAppearing()
    {
        base.OnAppearing();

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
    public EITManager? EIT
    {
        get
        {
            return _epgPageViewModel?.EIT;
        }
        set
        {
            if (_epgPageViewModel == null)
                return;

            _epgPageViewModel.EIT = value;
        }
    }

    public SledovaniTV.SledovaniTV? IPTV
    {
        get
        {
            return _epgPageViewModel?.IPTV;
        }
        set
        {
            if (_epgPageViewModel == null)
                return;

            _epgPageViewModel.IPTV = value;
        }
    }

    private void HandleListKeyDown(KeyboardNavigationActionEnum keyAction)
    {
        _loggingService.Debug($"EPGPage HandleListKeyDown {keyAction}");

        switch (keyAction)
        {
            case KeyboardNavigationActionEnum.Down:
                _epgPageViewModel?.SelectNextItem();
                break;
            case KeyboardNavigationActionEnum.Up:
                _epgPageViewModel?.SelectPreviousItem();
                break;
        }
    }

    private void HandleSwitchFocusKeyDown(KeyboardNavigationActionEnum keyAction)
    {
        switch (keyAction)
        {
            case KeyboardNavigationActionEnum.Right:

                switch (_epgPageViewModel?.ActiveItem)
                {
                    case EPGPageActiveItemEnum.List:
                        _epgPageViewModel?.ActiveItem = EPGPageActiveItemEnum.EPG;
                        _epgPageViewModel.SelectedItem?.Focused = false;
                        _epgPageViewModel.SelectedItem?.NotifyChanges();
                        //_focusItems.SetFocus("EPG");
                        break;
                    case EPGPageActiveItemEnum.EPG:
                        _epgPageViewModel?.ActiveItem = EPGPageActiveItemEnum.LeftButton;
                        //_focusItems.SetFocus("RightButton");
                        break;
                    case EPGPageActiveItemEnum.LeftButton:
                        _epgPageViewModel?.ActiveItem = EPGPageActiveItemEnum.RightButton;
                        //_focusItems.SetFocus("List");
                        break;
                    case EPGPageActiveItemEnum.RightButton:
                        _epgPageViewModel?.ActiveItem = EPGPageActiveItemEnum.List;
                        _epgPageViewModel.SelectedItem?.Focused = true;
                        _epgPageViewModel.SelectedItem?.NotifyChanges();
                        //_focusItems.SetFocus("LeftButton");
                        break;
                }

                break;
            case KeyboardNavigationActionEnum.Left:

                switch (_epgPageViewModel?.ActiveItem)
                {
                    case EPGPageActiveItemEnum.List:
                        _epgPageViewModel?.ActiveItem = EPGPageActiveItemEnum.RightButton;
                        _epgPageViewModel.SelectedItem?.Focused = false;
                        _epgPageViewModel.SelectedItem?.NotifyChanges();
                        //_focusItems.SetFocus("EPG");
                        break;
                    case EPGPageActiveItemEnum.EPG:
                        _epgPageViewModel?.ActiveItem = EPGPageActiveItemEnum.List;
                        _epgPageViewModel.SelectedItem?.Focused = true;
                        _epgPageViewModel.SelectedItem?.NotifyChanges();
                        //_focusItems.SetFocus("RightButton");
                        break;
                    case EPGPageActiveItemEnum.LeftButton:
                        _epgPageViewModel?.ActiveItem = EPGPageActiveItemEnum.EPG;
                        //_focusItems.SetFocus("List");
                        break;
                    case EPGPageActiveItemEnum.RightButton:
                        _epgPageViewModel?.ActiveItem = EPGPageActiveItemEnum.LeftButton;
                        //_focusItems.SetFocus("LeftButton");
                        break;
                }

                break;
        }
    }


    public void OnKeyDown(string key, bool longPress)
    {
        _loggingService.Debug($"EPGPage Page OnKeyDown {key}");

        var keyAction = KeyboardDeterminer.GetKeyAction(key);

        // handle
        //  - left/right key actions for switching between list/epg/buttons
        //  - back key action for going back to the previous page
        //
        switch (keyAction)
        {
            case KeyboardNavigationActionEnum.Right:
            case KeyboardNavigationActionEnum.Left:
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    HandleSwitchFocusKeyDown(keyAction);
                });
                Task.Run(async () => { await _epgPageViewModel.NotifyChange(); });
                return;
            case KeyboardNavigationActionEnum.Back:
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Navigation.PopAsync();
                });
                return;
        }

        // handle key actions based on the active item

        switch (_epgPageViewModel?.ActiveItem)
        {
            case EPGPageActiveItemEnum.List:
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    HandleListKeyDown(keyAction);
                });
                return;
        }
    }

    public void OnTextSent(string text)
    {
        _loggingService.Debug($"AboutPage Page OnTextSent {text}");
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