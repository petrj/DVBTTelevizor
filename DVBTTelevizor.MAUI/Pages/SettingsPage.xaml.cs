using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using LoggerService;
using System.Collections.ObjectModel;

namespace DVBTTelevizor.MAUI;

public partial class SettingsPage : ContentPage, IOnKeyDown
{
    private SettingsPageViewModel _settingsPageViewModel;

    private ILoggingService _loggingService;
    private IDriverConnector _driver;
    private IDialogService _dialogService;
    private ITVConfiguration _configuration;
    private string _publicDirectory = "";

    private KeyboardFocusableItemList _focusItems;

    public SettingsPage(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration, IDialogService dialogService, IPublicDirectoryProvider publicDirectoryProvider)
	{
        InitializeComponent();

        _loggingService = loggingService;
        _driver = driver;
        _configuration = tvConfiguration;
        _dialogService = dialogService;
        _publicDirectory = publicDirectoryProvider.GetPublicDirectoryPath();

        BindingContext = _settingsPageViewModel = new SettingsPageViewModel(loggingService, driver, tvConfiguration, dialogService, publicDirectoryProvider);

        Unloaded += SettingsPage_Unloaded;

        BuildFocusableItems();
    }

    private void SettingsPage_Unloaded(object? sender, EventArgs e)
    {

    }

    private void BuildFocusableItems()
    {
        _focusItems = new KeyboardFocusableItemList();

        _focusItems
            .AddItem(KeyboardFocusableItem.CreateFrom("ShowTVChannels", new List<View>() { ShowTVChannelsBoxView, ShowTVSwitch }))
            .AddItem(KeyboardFocusableItem.CreateFrom("ShowRadioChannels", new List<View>() { ShowRadioChannelsBoxView, ShowRadioSwitch }))
            .AddItem(KeyboardFocusableItem.CreateFrom("ShowNonFreeChannels", new List<View>() { ShowNonFreeChannelsBoxView, ShowNonFreeSwitch }))
            .AddItem(KeyboardFocusableItem.CreateFrom("ShowOtherChannels", new List<View>() { ShowOtherChannelsBoxView, ShowOtherSwitch }))

            .AddItem(KeyboardFocusableItem.CreateFrom("ClearChannels", new List<View>() { ClearChannelsButton }))
            .AddItem(KeyboardFocusableItem.CreateFrom("ExportToFile", new List<View>() { ExportToFileButton }))
            .AddItem(KeyboardFocusableItem.CreateFrom("ImportChannels", new List<View>() { ImportChannelsButton }))

            .AddItem(KeyboardFocusableItem.CreateFrom("ShowFullScreen", new List<View>() { ShowFullScreenBoxView, FullscreenSwitch }))
            .AddItem(KeyboardFocusableItem.CreateFrom("ShowPlayOnBackground", new List<View>() { ShowPlayOnBackgroundBoxView, PlayOnBackgroundSwitch }))

            .AddItem(KeyboardFocusableItem.CreateFrom("FontSize", new List<View>() { FontSizeBoxView, FontSizePicker }))
            .AddItem(KeyboardFocusableItem.CreateFrom("AutoStart", new List<View>() { AutoStartBoxView, ChannelAutoPlayedAfterStartPicker }))

            .AddItem(KeyboardFocusableItem.CreateFrom("ClearEPG", new List<View>() { ClearEPGButton }))

            .AddItem(KeyboardFocusableItem.CreateFrom("RemoteAccessEnabled", new List<View>() { RemoteAccessEnabledBoxView, RemoteAccessSwitch }))
            .AddItem(KeyboardFocusableItem.CreateFrom("RemoteAccessIP", new List<View>() { RemoteAccessIPBoxView, IPEntry }))
            .AddItem(KeyboardFocusableItem.CreateFrom("RemoteAccessPort", new List<View>() { RemoteAccessPortBoxView, PortEntry }))
            .AddItem(KeyboardFocusableItem.CreateFrom("RemoteAccessSecurityKey", new List<View>() { RemoteAccessSecurityKeyBoxView, SecurityKeyEntry }))

            .AddItem(KeyboardFocusableItem.CreateFrom("SelectDriver", new List<View>() { DriverBoxView, DriverPicker }))
            .AddItem(KeyboardFocusableItem.CreateFrom("SelectLanguage", new List<View>() { LanguageBoxView, LanguagePicker }))
            .AddItem(KeyboardFocusableItem.CreateFrom("ExportLanguage", new List<View>() { ExportLanguageButton }))

            .AddItem(KeyboardFocusableItem.CreateFrom("EnableLogging", new List<View>() { EnableLoggingBoxView, EnableLoggingSwitch }));

        //_focusItems.OnItemFocusedEvent += SettingsPage_OnItemFocusedEvent;
    }

    // bug in MAUI? SelectedLanguage is set, but index = -1
    private static void FixPickerValue(Picker picker, object selectedValue)
    {
        if (
            (picker.SelectedIndex == -1) &&
            (picker.SelectedItem != null) &&
            (!String.IsNullOrEmpty(picker.SelectedItem.ToString()))
           )
        {
            // bug in MAUI? SelectedLanguage is set, but index = -1
            for (var i = 0; i < picker.Items.Count; i++)
            {
                if (picker.Items[i].ToString() == picker.SelectedItem.ToString())
                {
                    picker.SelectedIndex = i;
                    break;
                }
            }
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _focusItems.DeFocusAll();
        MainPage.SetToolBarColors(Parent as NavigationPage, Colors.White, Color.FromArgb("#29242a"));

        if (_settingsPageViewModel != null)
        {
            _settingsPageViewModel.FillAutoPlayChannels();

            if (_settingsPageViewModel.FontSizes.Count == 0)
            {
                _settingsPageViewModel.FillFontSizes();
            }

            if (_settingsPageViewModel.DVBTDrivers.Count == 0)
            {
                _settingsPageViewModel.FillDVBTDrivers();
            }

            if (_settingsPageViewModel.Languages.Count == 0)
            {
                _settingsPageViewModel.FillLanguages();
            }

            _settingsPageViewModel.NotifyLanguageChange();

            FixPickerValue(LanguagePicker, _settingsPageViewModel.SelectedLanguage);
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
    }

    private void OnRemoteTelevizorLabelTapped(object sender, TappedEventArgs e)
    {

    }

    private void ExportLanguageButton_Clicked(object sender, EventArgs e)
    {
        var fileName = Path.Join(_publicDirectory, "en.lng");
        Lng.SaveToFile(fileName);

        WeakReferenceMessenger.Default.Send(new ToastMessage($"Language exported to {fileName}"));
    }

    public async void OnKeyDown(string key, bool longPress)
    {
        _loggingService.Debug($"Settings Page OnKeyDown {key}");

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
                        case "SelectLanguage":
                            LanguagePicker.Focus();
                            break;
                    }
                });
                break;
        }
    }

    public void OnTextSent(string text)
    {
    }

    private async void ClearChannelsButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Info("Clearing channels");

        var channels = _configuration.GetChannels();

        if (channels.Count == 0)
        {
            await _dialogService.Information("No channel found".Translated());
            return;
        }

        if (!await _dialogService.Confirm("Are you sure to delete all channels ({0})?".Translated(channels.Count.ToString()),"Confirm".Translated(),"Yes".Translated(), "No".Translated()))
        {
            return;
        }

        _configuration.SaveChannels(new ObservableCollection<Channel>());

        WeakReferenceMessenger.Default.Send(new ChannelsChangedMessage(String.Empty));
    }
}