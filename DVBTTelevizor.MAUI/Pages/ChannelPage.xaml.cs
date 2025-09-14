using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using LoggerService;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using static System.Net.Mime.MediaTypeNames;

namespace DVBTTelevizor.MAUI;

public partial class ChannelPage : ContentPage, IOnKeyDown
{
    private ChannelPageViewModel _channelPageViewModel;

    private ILoggingService _loggingService;
    private IDriverConnector _driver;
    private ITVConfiguration _configuration;
    private string _publicDirectory = "";
    private string? _previousName = null;
    private string? _previousNumber = null;

    private List<MenuItem> _menuItems = new List<MenuItem>();
    private KeyboardFocusableItemList _focusItems;

    public ChannelPage(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration, IPublicDirectoryProvider publicDirectoryProvider)
    {
        InitializeComponent();

        _loggingService = loggingService;
        _driver = driver;
        _configuration = tvConfiguration;
        _publicDirectory = publicDirectoryProvider.GetPublicDirectoryPath();

        BindingContext = _channelPageViewModel = new ChannelPageViewModel(loggingService, driver, tvConfiguration, publicDirectoryProvider);

        EntryName.Focused += delegate
        {
            EntryName.CursorPosition = EntryNumber.Text == null ? 0 : EntryName.Text.Length;
            _previousName = Channel?.Name;
        };
        EntryNumber.Focused += delegate
        {
            EntryNumber.CursorPosition = EntryNumber.Text == null ? 0 : EntryNumber.Text.Length;
            _previousNumber = Channel?.Number;
        };

        EntryName.Unfocused += EntryName_Unfocused;
        EntryNumber.Unfocused += EntryNumber_Unfocused;

        BuildFocusableItems();
    }

    private void ShowMenu()
    {
        MainMenu.MenuVisible =
        _channelPageViewModel.MenuVisible = true;
    }

    private void HideMenu()
    {
        MainMenu.MenuVisible =
        _channelPageViewModel.MenuVisible = false;
    }

    public void Menu_Tapped(object sender, EventArgs e)
    {
        if (e != null && e is TappedEventArgs tea)
        {
            Menu_Tapped(tea.Parameter.ToString());
        }
    }

    private async void Menu_Tapped(string menuId)
    {
        _loggingService.Info($"Menu tapped: {menuId}");

        HideMenu();

        var ch = _channelPageViewModel.Channel;
        if (ch == null)
        {
            return;
        }

        if (menuId.StartsWith("setAudio"))
        {
            WeakReferenceMessenger.Default.Send(new SetAudioTrackMessage(menuId));

            ch.SelectedAudioTrack = menuId.Substring(9);
        } else
        if (menuId.StartsWith("setSubtitles"))
        {
            WeakReferenceMessenger.Default.Send(new SetSubtitlesMessage(menuId));
            ch.SelectedSubtitle = menuId.Substring(13);
            return;
        }

        if (menuId == "menuConfirmDeleteChannel")
        {
            await Task.Run(async () => await RemoveChannelAndClose());
        }

        _channelPageViewModel.UpdateAutioAndSubtitles();
    }

    private async Task RemoveChannelAndClose()
    {
        if (Channel != null && Channels != null)
        {
            _channelPageViewModel.Channels = new ObservableCollection<Channel>(Channels.Where(x => x.UniqueIdentifier != Channel.UniqueIdentifier).ToList());
        }

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            ReloadChannels(null);

            await Navigation.PopAsync();
        });
    }

    private async Task SubtitlesMenu()
    {
        try
        {
            if (Channel == null ||
                Channel.Subtitles == null)
            {
                return;
            }

            ShowMenu();

           _menuItems = new List<MenuItem>();

            int index = 0;
            foreach (var sub in Channel.Subtitles)
            {
                var title = sub.Value;
                if (sub.Key.ToString() == Channel.SelectedAudioTrack)
                {
                    title += " *";
                }
                _menuItems.Add(MainMenu.CreateMenuItem($"setSubtitles:{sub.Key}", title, "audio.png", index > _channelPageViewModel.Subtitles.Count -1));
                index++;
            }

            _menuItems.Add(MainMenu.CreateMenuItem("menuBack", "Back".Translated(), "back.png"));
            _menuItems.Add(MainMenu.CreateMenuItem("menuClose", "Close".Translated(), "close.png"));

            MainMenu.UpdateMenu("Subtitles menu".Translated(), _menuItems);

            ShowMenu();

        }
        catch (Exception ex)
        {
            _loggingService.Error(ex);
        }
    }

    private async Task AudioMenu()
    {
        try
        {
            if (Channel == null ||
                Channel.AudioTracks == null)
            {
                return;
            }

            ShowMenu();

            _menuItems = new List<MenuItem>();

            int index = 0;
            foreach (var track in Channel.AudioTracks)
            {
                var title = track.Value;
                if (track.Key.ToString() == Channel.SelectedAudioTrack)
                {
                    title += " *";
                }
                _menuItems.Add(MainMenu.CreateMenuItem($"setAudio:{track.Key}", title, "audio.png", index > _channelPageViewModel.AudioTracks.Count-1));
                index++;
            }

            _menuItems.Add(MainMenu.CreateMenuItem("menuBack", "Back".Translated(), "back.png"));
            _menuItems.Add(MainMenu.CreateMenuItem("menuClose", "Close".Translated(), "close.png"));

            MainMenu.UpdateMenu("Audio menu".Translated(), _menuItems);

            ShowMenu();

        }
        catch (Exception ex)
        {
            _loggingService.Error(ex);
        }
    }

    private async Task DeleteMenu()
    {
        try
        {
            if (Channel == null)
            {
                return;
            }

            ShowMenu();

            _menuItems = new List<MenuItem>();

            _menuItems.Add(MainMenu.CreateMenuItem("menuConfirmDeleteChannel", "Yes".Translated(), "confirm.png"));
            _menuItems.Add(MainMenu.CreateMenuItem("menuNo", "No".Translated(), "cancel.png"));

            MainMenu.UpdateMenu("Are you sure to delete channel".Translated() + $" {Channel.Name}?", _menuItems);

            ShowMenu();
        }
        catch (Exception ex)
        {
            _loggingService.Error(ex);
        }
    }


    private void EntryName_Unfocused(object? sender, FocusEventArgs e)
    {
        if (Channel == null)
            return;

        if (Channel.Name == _previousName)
        {
            return;
        }

        ReloadChannels(Channel?.UniqueIdentifier);
        UpdateTitle();
    }

    private void EntryNumber_Unfocused(object sender, FocusEventArgs e)
    {
        if (Channel == null)
            return;

        if (Channel?.Number == _previousNumber)
        {
            return;
        }

        Task.Run(async () =>
        {
            try
            {
                int num;
                if (!int.TryParse(EntryNumber.Text, out num) || (num < 1) || (num > 32000))
                {
                    Channel.Number = _previousNumber;

                    WeakReferenceMessenger.Default.Send(new ToastMessage("Invalid number".Translated()));
                    _channelPageViewModel.NotifyChannelChange();

                    return;
                }

                if ((num < 1) || (num > 9999))
                {
                    Channel.Number = _previousNumber;

                    WeakReferenceMessenger.Default.Send(new ToastMessage("Number out of range".Translated()));
                    _channelPageViewModel.NotifyChannelChange();

                    return;
                }

                if (!Channel.IsNumberUnique(num.ToString(), Channels))
                {
                    Channel.Number = _previousNumber;

                    WeakReferenceMessenger.Default.Send(new ToastMessage("Number is already used".Translated()));
                    _channelPageViewModel.NotifyChannelChange();

                    return;
                }

                ReloadChannels(Channel?.UniqueIdentifier);
            }
            finally
            {
                _previousNumber = null;
            }
        });
    }

    private void UpdateTitle()
    {
        var title = "Channel".Translated();
        if (_channelPageViewModel.Channel != null && !String.IsNullOrWhiteSpace(_channelPageViewModel.Channel.Name))
        {
            title += $" - {_channelPageViewModel.Channel.Name}";
        }
        Title = title;
    }

    public Channel? Channel
    {
        get
        {
            return _channelPageViewModel?.Channel;
        }
        set
        {
            if (_channelPageViewModel == null)
                return;

            _channelPageViewModel.Channel = value;
            UpdateTitle();
        }
    }

    public ObservableCollection<Channel>? Channels
    {
        get
        {
            return _channelPageViewModel?.Channels;
        }
        set
        {
            if (_channelPageViewModel == null)
                return;

            _channelPageViewModel.Channels = value;
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _focusItems.DeFocusAll();
        MainPage.SetToolBarColors(Parent as NavigationPage, Colors.White, Color.FromArgb("#29242a"));
    }

    public async void ScrollToFocusedItem()
    {
        try
        {
            var focusedItem = _focusItems.FocusedItem;
            if (focusedItem != null)
            {
                var view = focusedItem.GetFirstView();
                if (view != null)
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await ChannelScrollView.ScrollToAsync(view, ScrollToPosition.Start, animated: false);
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _loggingService.Error(ex);
        }
    }

    private void BuildFocusableItems()
    {
        _focusItems = new KeyboardFocusableItemList();

        _focusItems
            .AddItem(KeyboardFocusableItem.CreateFrom("Name", new List<View>() { NameBoxView, EntryName }))
            .AddItem(KeyboardFocusableItem.CreateFrom("Number", new List<View>() { NumberBoxView, EntryNumber }))
            .AddItem(KeyboardFocusableItem.CreateFrom("Up", new List<View>() { ButtonUp }))
            .AddItem(KeyboardFocusableItem.CreateFrom("Down", new List<View>() { ButtonDown }))
            .AddItem(KeyboardFocusableItem.CreateFrom("MapPID", new List<View>() { MapPIDBoxView }))
            .AddItem(KeyboardFocusableItem.CreateFrom("Audio", new List<View>() { ButtonChangeAudio }))
            .AddItem(KeyboardFocusableItem.CreateFrom("Subtitles", new List<View>() { ButtonChangeSubtitles }))
            .AddItem(KeyboardFocusableItem.CreateFrom("Delete", new List<View>() { ButtonDeleteChannel }));

        //_focusItems.OnItemFocusedEvent += Page_OnItemFocusedEvent;
    }

    private void OnMenuKeyDown(KeyboardNavigationActionEnum keyAction)
    {
        switch (keyAction)
        {
            case KeyboardNavigationActionEnum.Right:
            case KeyboardNavigationActionEnum.Down:
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await MainMenu.SelectNextMenuItem(_menuItems, false);
                });
                break;

            case KeyboardNavigationActionEnum.Left:
            case KeyboardNavigationActionEnum.Up:
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await MainMenu.SelectNextMenuItem(_menuItems, true);
                });
                break;

            case KeyboardNavigationActionEnum.Back:
                HideMenu();
                break;

            case KeyboardNavigationActionEnum.OK:
                var id = GetSelectedMenuId();
                if (id != null)
                {
                    Menu_Tapped(id);
                }
                break;
        }
    }

    private string GetSelectedMenuId()
    {
        foreach (var item in _menuItems)
        {
            if (item.Selected)
            {
                return item.Id;
            }
        }

        return null;
    }

    public void OnKeyDown(string key, bool longPress)
    {
        _loggingService.Debug($"ChannelPage Page OnKeyDown {key}");

        var keyAction = KeyboardDeterminer.GetKeyAction(key);

        if (MainMenu.MenuVisible)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                OnMenuKeyDown(keyAction);
            });
            return;
        }

        switch (keyAction)
        {
            case KeyboardNavigationActionEnum.Down:
            case KeyboardNavigationActionEnum.Right:
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    _focusItems.FocusNextItem(true);
                    ScrollToFocusedItem();
                });
                break;

            case KeyboardNavigationActionEnum.Up:
            case KeyboardNavigationActionEnum.Left:
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    _focusItems.FocusPreviousItem(true);
                    ScrollToFocusedItem();
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
                        case "Name":
                            EntryName.Focus();
                            break;
                        case "Number":
                            EntryNumber.Focus();
                            break;
                        case "Up":
                            ButtonUp_Clicked(this, new EventArgs());
                            break;
                        case "Down":
                            ButtonDown_Clicked(this, new EventArgs());
                            break;
                        case "Audio":
                            ButtonChangeAudio_Clicked(this, new EventArgs());
                            break;
                        case "Subtitles":
                            ButtonChangeSubtitles_Clicked(this, new EventArgs());
                            break;
                        case "Delete":
                            ButtonDeleteChannel_Clicked(this, new EventArgs());
                            break;
                    }
                });
                break;
        }

    }

    public void OnTextSent(string text)
    {
        _loggingService.Debug($"ChannelPage Page OnTextSent {text}");
    }

    private void ButtonChangeAudio_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"ButtonChangeAudio_Clicked");

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await AudioMenu();
        });
    }

    private void ButtonChangeSubtitles_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"ButtonChangeSubtitles_Clicked");

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await SubtitlesMenu();
        });
    }

    private void ButtonDeleteChannel_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"ButtonDeleteChannel_Clicked");

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await DeleteMenu();
        });
    }

    private void ReloadChannels(string? uniqueIdentifier)
    {
        _configuration.SaveChannels(_channelPageViewModel.Channels);
        _channelPageViewModel.Channels = _configuration.GetChannels();
        if (uniqueIdentifier != null)
        {
            _channelPageViewModel.Channel = Channel.GetChannelByUniqueId(uniqueIdentifier, _channelPageViewModel.Channels);
        }
        _channelPageViewModel.NotifyChannelChange();
    }

    private void ButtonUp_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"ButtonDeleteChannel_Clicked");

        var prev = Channel.GetPreviousChannel(_channelPageViewModel.Channel, _channelPageViewModel.Channels);
        if (prev != null)
        {
            // swap numbers
            var num = prev.Number;
            prev.Number = Channel.Number;
            Channel.Number = num;

            ReloadChannels(Channel.UniqueIdentifier);
        }
    }

    private void ButtonDown_Clicked(object sender, EventArgs e)
    {
        _loggingService.Debug($"ButtonDown_Clicked");

        var next = Channel.GetNextChannel(_channelPageViewModel.Channel, _channelPageViewModel.Channels);
        if (next != null)
        {
            // swap numbers
            var num = next.Number;
            next.Number = Channel.Number;
            Channel.Number = num;

            ReloadChannels(Channel.UniqueIdentifier);
        }
    }
}