using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using LoggerService;
using Microsoft.Maui.Layouts;
using Microsoft.Maui.Platform;
using Newtonsoft.Json;
using System.Collections.ObjectModel;

namespace DVBTTelevizor.MAUI;

public partial class SettingsPage : ContentPage, IOnKeyDown
{
    public enum ImportChannelsEnum
    {
        None = 0,
        Overwrite = 1,
        Append = 2
    }

    private SettingsPageViewModel _settingsPageViewModel;

    private ILoggingService _loggingService;
    private IDriverConnector _driver;
    private IDialogService _dialogService;
    private ITVConfiguration _configuration;
    private string _publicDirectory = "";

    private KeyboardFocusableItemList _focusItems;
    private List<MenuItem> _menuItems = new List<MenuItem>();

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

        WriteToExternalDeviceSwitch.Toggled += WriteToExternalDeviceSwitch_Toggled;

        BuildFocusableItems();
    }

    private void WriteToExternalDeviceSwitch_Toggled(object? sender, ToggledEventArgs e)
    {
        _loggingService.Info("WriteToSDCardSwitch_Toggled");

        if (e.Value)
        {
            _settingsPageViewModel.RequestWriteToSDCard();
        }

        _settingsPageViewModel.NotifyConfigChange();
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

            .AddItem(KeyboardFocusableItem.CreateFrom("ExportToFile", new List<View>() { ExportToFileButton }))
            .AddItem(KeyboardFocusableItem.CreateFrom("ImportChannels", new List<View>() { ImportChannelsButton }))
            .AddItem(KeyboardFocusableItem.CreateFrom("ClearChannels", new List<View>() { ClearChannelsButton }))

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

            .AddItem(KeyboardFocusableItem.CreateFrom("WriteToExternalDevice", new List<View>() { WriteToExternalDeviceSwitchBoxView, WriteToExternalDeviceSwitch }))

            .AddItem(KeyboardFocusableItem.CreateFrom("SelectLanguage", new List<View>() { LanguageBoxView, LanguagePicker }))
            .AddItem(KeyboardFocusableItem.CreateFrom("ExportLanguage", new List<View>() { ExportLanguageButton }))

            .AddItem(KeyboardFocusableItem.CreateFrom("EnableLogging", new List<View>() { EnableLoggingBoxView, EnableLoggingSwitch }))

            .AddItem(KeyboardFocusableItem.CreateFrom("UDPIPLogging", new List<View>() { UDPIPLoggingBoxView, UDPIPEntry }));

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

    private void Menu_Tapped(object sender, EventArgs e)
    {
        if (e != null && e is TappedEventArgs tea)
        {
            Menu_Tapped(tea.Parameter.ToString());
        }
    }

    private void ShowOrHideMenu()
    {
        if (MainMenu.MenuVisible)
        {
            HideMenu();
        }
        else
        {
            ShowMenu();
        }
    }

    private void ShowMenu()
    {
        MainMenu.MenuVisible = true;
        _settingsPageViewModel.MenuVisible = true;
    }

    private void HideMenu()
    {
        MainMenu.MenuVisible = false;
        _settingsPageViewModel.MenuVisible = false;
    }

    private void DeleteChannelsMenu()
    {
        ShowOrHideMenu();

        if (MainMenu.IsVisible)
        {
            BuildConfirmDeleteChannelsMenu();
        }
    }

    private void BuildConfirmDeleteChannelsMenu()
    {
        _menuItems.Clear();

        var channels = _configuration.GetChannels();

        _menuItems.Add(MainMenu.CreateMenuItem("menuConfirm", "Delete all channels".Translated() + $" ({channels.Count})", "confirm.png"));
        _menuItems.Add(MainMenu.CreateMenuItem("menuCancel", "Cancel".Translated(), "cancel.png"));

        MainMenu.UpdateMenu("Confirmatiom".Translated(), _menuItems);
    }

    private void BuildConfirmMenu(string title, string titleYes, string titleNo, string actionConfirm)
    {
        ShowOrHideMenu();

        if (MainMenu.IsVisible)
        {
              _menuItems.Clear();

            _menuItems.Add(MainMenu.CreateMenuItem(actionConfirm, titleYes, "confirm.png"));
            _menuItems.Add(MainMenu.CreateMenuItem("menuCancel", titleNo, "cancel.png"));

            MainMenu.UpdateMenu(title, _menuItems);
        }
    }

    private void BuildChooseMenu(string title, Dictionary<string,string> items)
    {
        ShowOrHideMenu();

        if (MainMenu.IsVisible)
        {
            _menuItems.Clear();

            foreach (var kvp in items)
            {
                _menuItems.Add(MainMenu.CreateMenuItem(kvp.Key, kvp.Value, ""));
            }

            _menuItems.Add(MainMenu.CreateMenuItem("menuCancel", "Cancel".Translated(), "cancel.png"));

            MainMenu.UpdateMenu(title, _menuItems);
        }
    }

    private void BuildInfoMenu(string title, string titleOK)
    {
        ShowOrHideMenu();

        if (MainMenu.IsVisible)
        {
            _menuItems.Clear();

            _menuItems.Add(MainMenu.CreateMenuItem("menuOK", titleOK, ""));

            MainMenu.UpdateMenu(title, _menuItems);
        }
    }

    private async void Menu_Tapped(string menuId)
    {
        _loggingService.Info($"Menu tapped: {menuId}");

        HideMenu();

        switch (menuId)
        {
            case "menuConfirm":
                _configuration.SaveChannels(new ObservableCollection<Channel>());
                WeakReferenceMessenger.Default.Send(new ChannelsChangedMessage(String.Empty));
                WeakReferenceMessenger.Default.Send(new  ToastMessage("All existing channels were deleted".Translated()));
                break;
            case "menuCancel":
                break;
            case "menuConfirmOverwriteChannelsExport":
                ExportChannels(true);
                break;

            case "menuOverwriteExistingChannels":
                ImportChannels(ImportChannelsEnum.Overwrite);
                break;
            case "menuAppendExistingChannels":
                ImportChannels(ImportChannelsEnum.Append);
                break;
        }
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

    public static void ShowPicker(Picker picker)
    {
#if ANDROID
        var spinner = picker.Handler?.PlatformView as Android.Widget.Spinner;
        spinner?.PerformClick();
#elif WINDOWS
    var comboBox = picker.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.ComboBox;
    if (comboBox != null)
        comboBox.IsDropDownOpen = true;
#else
    // iOS and MacCatalyst do not allow programmatic opening
    System.Diagnostics.Debug.WriteLine("Programmatic Picker open not supported on this platform.");
#endif
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

        // if menu contains only single OK button, return ""menuOK""
        if ((_menuItems.Count == 1) && (_menuItems[0].Id == "menuOK"))
        {
            return "menuOK";
        }

        return null;
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
                    await SettingsScrollView.ScrollToAsync(view, ScrollToPosition.MakeVisible, animated: false);
                }
            }
        } catch (Exception ex)
        {
            _loggingService.Error(ex);
        }
    }

    public async void OnKeyDown(string key, bool longPress)
    {
        _loggingService.Debug($"Settings Page OnKeyDown {key}");

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
                    _focusItems.FocusNextItem();
                    ScrollToFocusedItem();
                });
                break;

            case KeyboardNavigationActionEnum.Up:
            case KeyboardNavigationActionEnum.Left:
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    _focusItems.FocusPreviousItem();
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

                        case "ClearChannels":
                            ClearChannelsButton_Clicked(this, new EventArgs());
                            break;

                        case "ExportToFile":
                            ExportToFileButton_Clicked(this, new EventArgs());
                            break;

                        case "ImportChannels":
                            ImportChannelsButton_Clicked(this, new EventArgs());
                            break;

                        case "ShowFullScreen":
                            FullscreenSwitch.IsToggled = !FullscreenSwitch.IsToggled;
                            break;

                        case "ShowPlayOnBackground":
                            PlayOnBackgroundSwitch.IsToggled = !PlayOnBackgroundSwitch.IsToggled;
                            break;

                        case "FontSize":
                            ShowPicker(FontSizePicker);
                            break;

                        case "AutoStart":
                            ShowPicker(ChannelAutoPlayedAfterStartPicker);
                            break;

                        case "ClearEPG":
                            MainThread.BeginInvokeOnMainThread(async () =>
                            {
                                ClearEPGButton_Clicked(this, new EventArgs());
                            });
                            break;

                        case "RemoteAccessEnabled":
                            RemoteAccessSwitch.IsToggled = !RemoteAccessSwitch.IsToggled;
                            BuildInfoMenu("The change will only take effect after the application is restarted".Translated(), "OK".Translated());
                            break;

                        case "RemoteAccessIP":
                            IPEntry.Focus();
                            break;

                        case "RemoteAccessPort":
                            PortEntry.Focus();
                            break;

                        case "RemoteAccessSecurityKey":
                            SecurityKeyEntry.Focus();
                            break;

                        case "SelectDriver":
                            ShowPicker(DriverPicker);
                            break;

                        case "WriteToExternalDevice":
                            WriteToExternalDeviceSwitch.IsToggled = !WriteToExternalDeviceSwitch.IsToggled;
                            break;

                        case "SelectLanguage":
                            ShowPicker(LanguagePicker);
                            break;

                        case "ExportLanguage":
                            ExportLanguageButton_Clicked(this, new EventArgs());
                            break;

                        case "EnableLogging":
                            EnableLoggingSwitch.IsToggled = !EnableLoggingSwitch.IsToggled;
                            BuildInfoMenu("The change will only take effect after the application is restarted".Translated(), "OK".Translated());
                            break;

                        case "UDPIPLogging":
                            UDPIPEntry.Focus();
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
        _loggingService.Info("ClearChannelsButton_Clicked");

        DeleteChannelsMenu();
    }

    private void ExportToFileButton_Clicked(object sender, EventArgs e)
    {
        ExportChannels();
    }

    private async void ExportChannels(bool overwrite = false)
    {
        _loggingService.Info($"ExportChannels: overwrite:{overwrite}");

        try
        {
            if (File.Exists(_settingsPageViewModel.AndroidChannelsListPath))
            {
                if (!overwrite)
                {
                    BuildConfirmMenu("File already exists. Overwrite?".Translated(), "Yes".Translated(), "No".Translated(), "menuConfirmOverwriteChannelsExport");
                    return;
                } else
                {
                    File.Delete(_settingsPageViewModel.AndroidChannelsListPath);
                }
            }

            var channels = _configuration.GetChannels();
            var json = JsonConvert.SerializeObject(channels);

            File.WriteAllText(_settingsPageViewModel.AndroidChannelsListPath, json);
        } catch (Exception ex)
        {
            _loggingService.Error(ex, "Error exporting channels list");
        }
    }

    private async void ImportChannels(ImportChannelsEnum importChannels)
    {
        _loggingService.Info($"ImportChannels: importChannelsEnum:{importChannels}");

        try
        {
            if (!File.Exists(_settingsPageViewModel.AndroidChannelsListPath))
            {
                BuildInfoMenu("File does not exist".Translated(), "OK".Translated());
                return;
            }

            var channels = _configuration.GetChannels();

            if ((channels.Count > 0) && (importChannels == ImportChannelsEnum.None))
            {
                BuildChooseMenu("Import channels", new Dictionary<string, string>()
                {
                    {"menuOverwriteExistingChannels","Overwite existing".Translated()},
                    {"menuAppendExistingChannels","Append existing".Translated()}
                });
                return;
            }

            if (importChannels == ImportChannelsEnum.Overwrite)
            {
                channels.Clear();
            }

            var channelsJSON = await File.ReadAllTextAsync(_settingsPageViewModel.AndroidChannelsListPath);
            var importedChannels = JsonConvert.DeserializeObject<ObservableCollection<Channel>>(channelsJSON);

            var importedCount = 0;

            foreach (var channel in importedChannels)
            {
                bool exists = false;
                foreach (var existingChannel in channels)
                {
                    if (existingChannel.UniqueIdentifier == channel.UniqueIdentifier)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    importedCount++;
                    channels.Add(channel.Clone());
                } else
                {
                    _loggingService.Info("Channel already exists");
                }
            }

            _configuration.SaveChannels(channels);

            WeakReferenceMessenger.Default.Send(new ChannelsChangedMessage(String.Empty));

            var msg = $"{importedCount} " + "channels imported".Translated();

            WeakReferenceMessenger.Default.Send(new ToastMessage(msg));
        }
        catch (Exception ex)
        {
            _loggingService.Error(ex, "Error exporting channels list");
        }
    }

    private void ImportChannelsButton_Clicked(object sender, EventArgs e)
    {
        ImportChannels(ImportChannelsEnum.None);
    }

    private void ClearEPGButton_Clicked(object sender, EventArgs e)
    {

    }
}