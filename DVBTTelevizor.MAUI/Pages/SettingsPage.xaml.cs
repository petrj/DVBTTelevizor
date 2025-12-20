using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using LoggerService;
using Microsoft.Maui.Layouts;
using Microsoft.Maui.Platform;
using Newtonsoft.Json;
using SledovaniTV;
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
    private ITVConfiguration _configuration;
    private string _publicDirectory = "";
    private string _lngBefore = "";

    private SledovaniTV.SledovaniTV _iptv;

    private KeyboardFocusableItemList _focusItems;
    private List<MenuItem> _menuItems = new List<MenuItem>();

    public SettingsPage(ILoggingService loggingService, IDriverConnector driver, SledovaniTV.SledovaniTV iptv, ITVConfiguration tvConfiguration, IPublicDirectoryProvider publicDirectoryProvider)
	{
        InitializeComponent();

        _loggingService = loggingService;
        _driver = driver;
        _iptv = iptv;
        _configuration = tvConfiguration;
        _publicDirectory = publicDirectoryProvider.GetPublicDirectoryPath();

        BindingContext = _settingsPageViewModel = new SettingsPageViewModel(loggingService, driver, iptv, tvConfiguration, publicDirectoryProvider);


        Unloaded += SettingsPage_Unloaded;

        WriteToExternalDeviceSwitch.Toggled += WriteToExternalDeviceSwitch_Toggled;
        PlayOnBackgroundSwitch.Toggled += PlayOnBackgroundSwitch_Toggled;
        FullscreenSwitch.Toggled += FullscreenSwitch_Toggled;

        BuildFocusableItems();

        LanguagePicker.Focused += delegate { _lngBefore = _configuration.Language; };
        LanguagePicker.Unfocused += delegate
        {
            if (_lngBefore != _configuration.Language)
            {
                BuildInfoMenu("The change will only take effect after the application is restarted".Translated(), "OK".Translated());
            }
        };

        EnableLoggingSwitch.Toggled += delegate
        {
            BuildInfoMenu("The change will only take effect after the application is restarted".Translated(), "OK".Translated());
        };

        WeakReferenceMessenger.Default.Register<CheckBatterySettingsReplyMessage>(this, (r, m) =>
        {
            Task.Run(async () =>
            {
                await ProcessCheckBatterySettingsResult(m.Value);
            });
        });
    }

    private void FullscreenSwitch_Toggled(object? sender, ToggledEventArgs e)
    {
        _loggingService.Info("FullscreenSwitch_Toggled");

        WeakReferenceMessenger.Default.Send(new ShowFullscreenMessage(e.Value));
    }

    private void PlayOnBackgroundSwitch_Toggled(object? sender, ToggledEventArgs e)
    {
        _loggingService.Info("PlayOnBackgroundSwitch_Toggled");

        if (e.Value)
        {
            WeakReferenceMessenger.Default.Send(new CheckBatterySettingsMessage(String.Empty));
        }
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

            .AddItem(KeyboardFocusableItem.CreateFrom("RemoteAccessAppLink", new List<View>() { RemoteAccessAppLinkBoxView }))



            .AddItem(KeyboardFocusableItem.CreateFrom("SelectDriver", new List<View>() { DriverBoxView, DriverPicker }))

            .AddItem(KeyboardFocusableItem.CreateFrom("WriteToExternalDevice", new List<View>() { WriteToExternalDeviceSwitchBoxView, WriteToExternalDeviceSwitch }))

            .AddItem(KeyboardFocusableItem.CreateFrom("SelectLanguage", new List<View>() { LanguageBoxView, LanguagePicker }))

            .AddItem(KeyboardFocusableItem.CreateFrom("EnableLogging", new List<View>() { EnableLoggingBoxView, EnableLoggingSwitch }))

            .AddItem(KeyboardFocusableItem.CreateFrom("UDPIPLogging", new List<View>() { UDPIPLoggingBoxView, UDPIPEntry }))

            .AddItem(KeyboardFocusableItem.CreateFrom("SledovaniTVEnabled", new List<View>() { SledovaniTVEnabledBoxView, SledovaniTVSwitch }))
            .AddItem(KeyboardFocusableItem.CreateFrom("SledovaniTVUserName", new List<View>() { SledovaniTVUserNameBoxView, SledovaniTVUserNameEntry }))
            .AddItem(KeyboardFocusableItem.CreateFrom("SledovaniTVPassword", new List<View>() { SledovaniTVPasswordBoxView, SledovaniTVPasswordEntry }))

            .AddItem(KeyboardFocusableItem.CreateFrom("SledovaniTVPairButton", new List<View>() { SledovaniTVPairButton }))
            .AddItem(KeyboardFocusableItem.CreateFrom("SledovaniTVReloadChannelsButton", new List<View>() { SledovaniTVReloadChannelsButton }))

            .AddItem(KeyboardFocusableItem.CreateFrom("SledovaniTVShowDevice", new List<View>() { SledovaniTVShowDeviceBoxView, SledovaniTVShowPairedDeviceSwitch }))
            .AddItem(KeyboardFocusableItem.CreateFrom("SledovaniTVDeviceID", new List<View>() { SledovaniTVDeviceIDBoxView, SledovaniTVDeviceIDEntry }))
            .AddItem(KeyboardFocusableItem.CreateFrom("SledovaniTVDevicePassword", new List<View>() { SledovaniTVDevicePasswordBoxView, SledovaniTVDevicePasswordEntry }))

            .AddItem(KeyboardFocusableItem.CreateFrom("SledovaniTVShowAdultChannels", new List<View>() { SledovaniTVShowAdultChannelsBoxView, SledovaniTVShowAdultChannelsSwitch }))
            .AddItem(KeyboardFocusableItem.CreateFrom("SledovaniTVPIN", new List<View>() { SledovaniTVPINBoxView, SledovaniTVPINEntry }))

            .AddItem(KeyboardFocusableItem.CreateFrom("ExportSettingsToFile", new List<View>() { ExportSettingsToFileButton }))
            .AddItem(KeyboardFocusableItem.CreateFrom("ImportSettingsFromFile", new List<View>() { ImportSettingsFromFileButton }));
        ;


        _focusItems.OnItemFocusedEvent += _focusItems_OnItemFocusedEvent;
    }

    private void _focusItems_OnItemFocusedEvent(KeyboardFocusableItemEventArgs _args)
    {
        var item = _args.FocusedItem.Name;
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
        if (e != null &&
            e is TappedEventArgs tea &&
            tea.Parameter is MenuItem item)
        {
            Menu_Tapped(item);
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

        MainMenu.UpdateMenu((int)_configuration.AppFontSize, "Confirmatiom".Translated(), _menuItems);
    }

    private void BuildConfirmMenu(string title, string titleYes, string titleNo, string actionConfirm)
    {
        ShowOrHideMenu();

        if (MainMenu.IsVisible)
        {
              _menuItems.Clear();

            _menuItems.Add(MainMenu.CreateMenuItem(actionConfirm, titleYes, "confirm.png"));
            _menuItems.Add(MainMenu.CreateMenuItem("menuCancel", titleNo, "cancel.png"));

            MainMenu.UpdateMenu((int)_configuration.AppFontSize, title, _menuItems);
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

            MainMenu.UpdateMenu((int)_configuration.AppFontSize, title, _menuItems);
        }
    }

    private void BuildInfoMenu(string title, string titleOK)
    {
        ShowOrHideMenu();

        if (MainMenu.IsVisible)
        {
            _menuItems.Clear();

            _menuItems.Add(MainMenu.CreateMenuItem("menuOK", titleOK, ""));

            MainMenu.UpdateMenu((int)_configuration.AppFontSize, title, _menuItems);
        }
    }

    private async void Menu_Tapped(MenuItem item)
    {
        var menuId = item.Id;
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
            case "menuClearCache":
                WeakReferenceMessenger.Default.Send(new ClearCacheMessage(String.Empty));
                break;
            case "menuGoToBatteryOptimizationSettings":
                WeakReferenceMessenger.Default.Send(new OpenBatteryOptimizationSettingsMessage(String.Empty));
                break;
            case "menuSledovaniTVReloadChannels":
                await _settingsPageViewModel.SledovaniTVReloadChannels();
                break;
            case "menuConfirmOverwriteSettingsExport":
                await _settingsPageViewModel.ExportSettings();
                break;

            case "menuConfirmImportSettingsExport":
                await _settingsPageViewModel.ImportSettings();
                break;
        }
    }

    private async Task ProcessCheckBatterySettingsResult(bool ignoring)
    {
        _loggingService.Debug($"ProcessCheckBatterySettingsResult");

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (!ignoring)
            {
                // When running in the background, it is necessary to ensure that the app is not terminated due to battery optimization
                BuildConfirmMenu("When running in the background, it is necessary to ensure that the app is not terminated due to battery optimization".Translated(), "Go to settings...".Translated(), "Close".Translated(), "menuGoToBatteryOptimizationSettings");
            }
        });
    }

    private void OnRemoteTelevizorLabelTapped(object sender, TappedEventArgs e)
    {
        WeakReferenceMessenger.Default.Send(new OpenURLMessage("https://play.google.com/store/apps/details?id=net.petrjanousek.RemoteTelevizor"));
    }

    public static void ShowPicker(Picker picker)
    {
        picker.Focus();

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

    private MenuItem? GetSelectedMenuItem()
    {
        foreach (var item in _menuItems)
        {
            if (item.Selected)
            {
                return item;
            }
        }

        // if menu contains only single OK button, return ""menuOK""
        if ((_menuItems.Count == 1) && (_menuItems[0].Id == "menuOK"))
        {
            return _menuItems[0];
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
                var menu = GetSelectedMenuItem();
                if (menu != null)
                {
                    Menu_Tapped(menu);
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

                        case "RemoteAccessAppLink":
                            OnRemoteTelevizorLabelTapped(this, null);
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

                        case "EnableLogging":
                            EnableLoggingSwitch.IsToggled = !EnableLoggingSwitch.IsToggled;
                            BuildInfoMenu("The change will only take effect after the application is restarted".Translated(), "OK".Translated());
                            break;

                        case "UDPIPLogging":
                            UDPIPEntry.Focus();
                            break;

                        case "SledovaniTVEnabled":
                            SledovaniTVSwitch.IsToggled = !SledovaniTVSwitch.IsToggled;
                            break;

                        case "SledovaniTVUserName":
                            SledovaniTVUserNameEntry.Focus();
                            break;

                        case "SledovaniTVPassword":
                            SledovaniTVDevicePasswordEntry.Focus();
                            break;

                        case "SledovaniTVPairButton":
                            SledovaniTVPairButton_Clicked(this, new EventArgs());
                            break;

                        case "SledovaniTVReloadChannelsButton":
                            SledovaniTVReloadChannelsButton_Clicked(this, new EventArgs());
                            break;

                        case "SledovaniTVShowDevice":
                            SledovaniTVShowPairedDeviceSwitch.IsToggled = !SledovaniTVShowPairedDeviceSwitch.IsToggled;
                            break;

                        case "SledovaniTVDeviceID":
                            SledovaniTVDeviceIDEntry.Focus();
                            break;

                        case "SledovaniTVDevicePassword":
                            SledovaniTVDevicePasswordEntry.Focus();
                            break;

                        case "SledovaniTVShowAdultChannels":
                            SledovaniTVShowAdultChannelsSwitch.IsToggled = !SledovaniTVShowAdultChannelsSwitch.IsToggled;
                            break;

                        case "SledovaniTVPIN":
                            SledovaniTVPINEntry.Focus();
                            break;
                        case "ExportSettingsToFile":
                            ExportSettingsToFileButton_Clicked(this, new EventArgs());
                            break;
                        case "ImportSettingsFromFil":
                            ImportSettingsFromFileButton_Clicked(this, new EventArgs());
                            break;
                    }
                });
                break;
        }
    }

    public void OnTextSent(string text)
    {
        if (_focusItems.FocusedItem == null)
            return;

        switch (_focusItems.FocusedItem.Name)
        {
            case "RemoteAccessIP":
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    IPEntry.Text = text;
                });
                break;
            case "RemoteAccessPort":
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    PortEntry.Text = text;
                });
                break;
            case "RemoteAccessSecurityKey":
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    SecurityKeyEntry.Text = text;
                });
                break;

            case "SledovaniTVUserName":
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    SledovaniTVUserNameEntry.Text = text;
                });
                break;

            case "SledovaniTVPassword":
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    SledovaniTVPasswordEntry.Text = text;
                });
                break;

            case "SledovaniTVDeviceID":
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    SledovaniTVDeviceIDEntry.Text = text;
                });
                break;

            case "SledovaniTVDevicePassword":
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    SledovaniTVDevicePasswordEntry.Text = text;
                });
                break;

            case "SledovaniTVPIN":
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    SledovaniTVPINEntry.Text = text;
                });
                break;
        }
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

            WeakReferenceMessenger.Default.Send(new ToastMessage("Channels exported".Translated()));

        }
        catch (Exception ex)
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
                BuildChooseMenu("Import channels".Translated(), new Dictionary<string, string>()
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
        _loggingService.Info("ClearEPGButton_Clicked");

        BuildConfirmMenu("Clear cache?".Translated(), "Clear EPG cand channel cache".Translated(), "Cancel".Translated(), "menuClearCache");
    }

    private void SledovaniTVReloadChannelsButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Info("SledovaniTVReloadChannelsButton_Clicked");

        BuildConfirmMenu("SledovaniTV - Reloading will reset all channels changes".Translated(), "Reload all channels".Translated(), "Cancel".Translated(), "menuSledovaniTVReloadChannels");
    }

    private async void SledovaniTVPairButton_Clicked(object sender, EventArgs e)
    {
        if (String.IsNullOrWhiteSpace(_configuration.SledovaniTVUserName) &&
            String.IsNullOrWhiteSpace(_configuration.SledovaniTVUserName))
        {
            WeakReferenceMessenger.Default.Send(new ToastMessage("Empty credentials".Translated()));
            return;
        }
        await _settingsPageViewModel.SledovaniTVPair();
    }

    private void ExportSettingsToFileButton_Clicked(object sender, EventArgs e)
    {
        Task.Run(async () =>
        {
            if (File.Exists(_settingsPageViewModel.AndroidSettingsListPath))
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    BuildConfirmMenu("File already exists. Overwrite?".Translated(), "Yes".Translated(), "No".Translated(), "menuConfirmOverwriteSettingsExport");
                });
                return;
            }

            await _settingsPageViewModel.ExportSettings();
        });
    }

    private void ImportSettingsFromFileButton_Clicked(object sender, EventArgs e)
    {
        _loggingService.Info($"ImportSettingsFromFileButton_Clicked");

        try
        {
            if (!File.Exists(_settingsPageViewModel.AndroidSettingsListPath))
            {
                BuildInfoMenu("File does not exist".Translated(), "OK".Translated());
                return;
            }

            BuildConfirmMenu("Import configration? All settings will be overwritten!".Translated(), "Yes".Translated(), "No".Translated(), "menuConfirmImportSettingsExport");
        }
        catch (Exception ex)
        {
            _loggingService.Error(ex, "Error importing settings");
        }
    }
}