using AndroidX.Lifecycle;
using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using LoggerService;
using System.Collections.ObjectModel;
using static System.Net.Mime.MediaTypeNames;

namespace DVBTTelevizor.MAUI;

public partial class ChannelPage : ContentPage, IOnKeyDown
{
    private ChannelPageViewModel _channelPageViewModel;

    private ILoggingService _loggingService;
    private IDriverConnector _driver;
    private IDialogService _dialogService;
    private ITVConfiguration _configuration;
    private string _publicDirectory = "";
    private string? _previousName = null;

    private KeyboardFocusableItemList _focusItems;

    private List<MenuItem> _subtitleMenuItems = new List<MenuItem>();
    private List<MenuItem> _audioMenuItems = new List<MenuItem>();

    public ChannelPage(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration, IDialogService dialogService, IPublicDirectoryProvider publicDirectoryProvider)
    {
        InitializeComponent();

        _loggingService = loggingService;
        _driver = driver;
        _configuration = tvConfiguration;
        _dialogService = dialogService;
        _publicDirectory = publicDirectoryProvider.GetPublicDirectoryPath();

        BindingContext = _channelPageViewModel = new ChannelPageViewModel(loggingService, driver, tvConfiguration, dialogService, publicDirectoryProvider);

        EntryName.Focused += delegate
        {
            EntryName.CursorPosition = EntryNumber.Text == null ? 0 : EntryName.Text.Length;
            _previousName = _channelPageViewModel?.Channel?.Name;
        };
        EntryName.Unfocused += EntryName_Unfocused;

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
    }

    private async Task AudioMenu()
    {
        try
        {
            if (_channelPageViewModel == null ||
                _channelPageViewModel.Channel == null ||
                _channelPageViewModel.Channel.AudioTracks == null)
            {
                return;
            }

            ShowMenu();

            _audioMenuItems.Clear();

            int index = 0;
            foreach (var track in _channelPageViewModel.Channel.AudioTracks)
            {
                index++;
                var title = track.Value;
                if (track.Key.ToString() == _channelPageViewModel.Channel.SelectedAudioTrack)
                {
                    title += " *";
                }
                _audioMenuItems.Add(MainMenu.CreateMenuItem($"setAudio:{track.Key}", title, "audio.png", index > _channelPageViewModel.AudioTracks.Count - 1));
            }

            _audioMenuItems.Add(MainMenu.CreateMenuItem("menuBack", "Back".Translated(), "back.png"));
            _audioMenuItems.Add(MainMenu.CreateMenuItem("menuClose", "Close".Translated(), "close.png"));

            MainMenu.UpdateMenu("Audio menu".Translated(), _audioMenuItems);

            ShowMenu();

        }
        catch (Exception ex)
        {
            _loggingService.Error(ex);
        }
    }


    private void EntryName_Unfocused(object? sender, FocusEventArgs e)
    {
        if (_channelPageViewModel == null || _channelPageViewModel.Channel == null)
            return;

        if (_channelPageViewModel.Channel.Name == _previousName)
        {
            return;
        }

        Task.Run(async () =>
        {
            if (string.IsNullOrEmpty(EntryName.Text))
            {
                _channelPageViewModel.Channel.Name = _previousName;

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await _dialogService.Information($"Invalid name".Translated());
                    _channelPageViewModel.NotifyChannelChange();
                });
            }

            //if (_channelPageViewModel.Channel.Name != _previousName)
            //{
            //    _channelPageViewModel.Changed = true;
            //    await _channelPageViewModel.SaveChannels();
            //}

            _previousName = null;
        });
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

            var title = "Channel".Translated();
            if (value != null && !String.IsNullOrWhiteSpace(value.Name))
            {
                title += $" - {value.Name}";
            }
            Title = title;
        }
    }

    private void BuildFocusableItems()
    {
        _focusItems = new KeyboardFocusableItemList();

        //_focusItems
        //    .AddItem(KeyboardFocusableItem.CreateFrom("Donate1", new List<View>() { Donate1Button }))
        //    .AddItem(KeyboardFocusableItem.CreateFrom("Donate2", new List<View>() { Donate2Button }))
        //    .AddItem(KeyboardFocusableItem.CreateFrom("Donate3", new List<View>() { Donate3Button }))
        //    .AddItem(KeyboardFocusableItem.CreateFrom("Donate5", new List<View>() { Donate5Button }));

        //_focusItems.OnItemFocusedEvent += Page_OnItemFocusedEvent;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _focusItems.DeFocusAll();
        MainPage.SetToolBarColors(Parent as NavigationPage, Colors.White, Color.FromArgb("#29242a"));
    }

    public void OnKeyDown(string key, bool longPress)
    {
        _loggingService.Debug($"ChannelPage Page OnKeyDown {key}");
        /*

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
        */
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
            AudioMenu();
        });
    }

    private void ButtonChangeSubtitles_Clicked(object sender, EventArgs e)
    {

    }

    private void ButtonDeleteChannel_Clicked(object sender, EventArgs e)
    {

    }

    private void ButtonUp_Clicked(object sender, EventArgs e)
    {

    }

    private void ButtonDown_Clicked(object sender, EventArgs e)
    {

    }
}