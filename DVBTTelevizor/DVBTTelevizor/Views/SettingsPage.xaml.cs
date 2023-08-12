using LoggerService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace DVBTTelevizor
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SettingsPage : ContentPage, IOnKeyDown
    {
        private SettingsPageViewModel _viewModel;
        protected ILoggingService _loggingService;
        protected IDialogService _dialogService;
        private DVBTTelevizorConfiguration _config;

        private KeyboardFocusableItemList _focusItems;

        public SettingsPage(ILoggingService loggingService, IDialogService dialogService, DVBTTelevizorConfiguration config, ChannelService channelService)
        {
            InitializeComponent();

            _loggingService = loggingService;
            _dialogService = dialogService;
            _config = config;

            BindingContext = _viewModel = new SettingsPageViewModel(_loggingService, _dialogService, config, channelService);

            PlayOnBackgroundSwitch.Toggled += PlayOnBackgroundSwitch_Toggled;

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_RequestBatterySettings, async (sender) =>
            {
                if (await _dialogService.Confirm("You should manually turn battery optimization off for DVBT Televizor. Open settings?"))
                {
                    MessagingCenter.Send<SettingsPage>(this, BaseViewModel.MSG_SetBatterySettings);
                }
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_SettingsPageForceLayout, async (value) =>
            {
                Device.BeginInvokeOnMainThread(delegate
                {
                    // workaround for overlayed Logging and Donate stacklayout after change visibility of Remote access stacklayout
                    StackLayoutLogging.BackgroundColor = Color.Transparent;
                    StackLayoutDonate.BackgroundColor = Color.Transparent;
                });
            });

            BuildFocusableItems();

            IPEntry.Focused += Entry_Focused;
            PortEntry.Focused += Entry_Focused;
            SecurityKeyEntry.Focused += Entry_Focused;
        }

        private void Entry_Focused(object sender, FocusEventArgs e)
        {
            if (sender is Entry entry)
            {
                // move cursor to end
                if (entry.Text != null)
                    entry.CursorPosition = entry.Text.Length;
            }
        }

        public async Task AcknowledgePurchases()
        {
            await _viewModel.AcknowledgePurchases();
        }

        private void PlayOnBackgroundSwitch_Toggled(object sender, ToggledEventArgs e)
        {
            if (_viewModel.Config.PlayOnBackground)
            {
                MessagingCenter.Send<SettingsPage>(this, BaseViewModel.MSG_CheckBatterySettings);
            }
        }

        protected override void OnAppearing()
        {
            _focusItems.DeFocusAll();

            if (_viewModel != null)
                _viewModel.FillAutoPlayChannels();

            base.OnAppearing();
        }

        private void BuildFocusableItems()
        {
            _focusItems = new KeyboardFocusableItemList();

            _focusItems
                .AddItem(KeyboardFocusableItem.CreateFrom("ShowTVChannels", new List<View>() { ShowTVChannelsBoxView, ShowTVSwitch }))
                .AddItem(KeyboardFocusableItem.CreateFrom("ShowRadioChannels", new List<View>() { ShowRadioChannelsBoxView, ShowRadioSwitch }))
                .AddItem(KeyboardFocusableItem.CreateFrom("ShowOtherChannels", new List<View>() { ShowOtherChannelsBoxView, ShowOtherSwitch }))

                .AddItem(KeyboardFocusableItem.CreateFrom("ClearChannels", new List<View>() { ClearChannelsButton }))
                .AddItem(KeyboardFocusableItem.CreateFrom("ShareChannels", new List<View>() { ShareChannelsButton }))
                .AddItem(KeyboardFocusableItem.CreateFrom("ExportToFile", new List<View>() { ExportToFileButton }))
                .AddItem(KeyboardFocusableItem.CreateFrom("ImportChannels", new List<View>() { ImportChannelsButton }))

                .AddItem(KeyboardFocusableItem.CreateFrom("ShowFullScreen", new List<View>() { ShowFullScreenBoxView, FullscreenSwitch }))
                .AddItem(KeyboardFocusableItem.CreateFrom("ShowPlayOnBackground", new List<View>() { ShowPlayOnBackgroundBoxView, PlayOnBackgroundSwitch }))

                .AddItem(KeyboardFocusableItem.CreateFrom("FontSize", new List<View>() { FontSizeBoxView, FontSizePicker }))
                .AddItem(KeyboardFocusableItem.CreateFrom("AutoStart", new List<View>() { AutoStartBoxView, ChannelAutoPlayedAfterStartPicker }))

                .AddItem(KeyboardFocusableItem.CreateFrom("RemoteAccessEnabled", new List<View>() { RemoteAccessEnabledBoxView, RemoteAccessSwitch }))
                .AddItem(KeyboardFocusableItem.CreateFrom("RemoteAccessIP", new List<View>() { RemoteAccessIPBoxView, IPEntry }))
                .AddItem(KeyboardFocusableItem.CreateFrom("RemoteAccessPort", new List<View>() { RemoteAccessPortBoxView, PortEntry }))
                .AddItem(KeyboardFocusableItem.CreateFrom("RemoteAccessSecurityKey", new List<View>() { RemoteAccessSecurityKeyBoxView, SecurityKeyEntry }))

                .AddItem(KeyboardFocusableItem.CreateFrom("EnableLogging", new List<View>() { EnableLoggingBoxView, EnableLoggingSwitch }))

                .AddItem(KeyboardFocusableItem.CreateFrom("Donate1", new List<View>() { Donate1Button }))
                .AddItem(KeyboardFocusableItem.CreateFrom("Donate2", new List<View>() { Donate2Button }))
                .AddItem(KeyboardFocusableItem.CreateFrom("Donate5", new List<View>() { Donate5Button }))
                .AddItem(KeyboardFocusableItem.CreateFrom("Donate10", new List<View>() { Donate10Button }));

            _focusItems.OnItemFocusedEvent += SettingsPage_OnItemFocusedEvent;
        }

        private void SettingsPage_OnItemFocusedEvent(KeyboardFocusableItemEventArgs args)
        {
            // scroll to element

            if (args.FocusedItem == null)
                return;

            Device.BeginInvokeOnMainThread(async () =>
            {
                // scroll to element
                await SettingsScrollView.ScrollToAsync(0, args.FocusedItem.MaxYPosition - args.FocusedItem.Height, false);
            });

            Action action = null;

            switch (_focusItems.LastFocusDirection)
            {
                case KeyboardFocusDirection.Next: action = delegate { _focusItems.FocusNextItem(); }; break;
                case KeyboardFocusDirection.Previous: action = delegate { _focusItems.FocusPreviousItem(); }; break;
                default: action = delegate { args.FocusedItem.DeFocus(); }; break;
            }

            if (
                    (args.FocusedItem.Name == "RemoteAccessIP" ||
                    args.FocusedItem.Name == "RemoteAccessPort" ||
                    args.FocusedItem.Name == "RemoteAccessSecurityKey")
                    && (!_config.AllowRemoteAccessService)
               )
            {
                action();
                return;
            }
        }

        public async void OnKeyDown(string key, bool longPress)
        {
            _loggingService.Debug($"Settings Page OnKeyDown {key}");

            var keyAction = KeyboardDeterminer.GetKeyAction(key);

            switch (keyAction)
            {
                case KeyboardNavigationActionEnum.Down:
                    _focusItems.FocusNextItem();
                    break;

                case KeyboardNavigationActionEnum.Up:
                    _focusItems.FocusPreviousItem();
                    break;

                case KeyboardNavigationActionEnum.Back:
                    await Navigation.PopAsync();
                    break;

                case KeyboardNavigationActionEnum.OK:

                    switch (_focusItems.FocusedItemName)
                    {
                        case "ShowTVChannels":
                            ShowTVSwitch.IsToggled = !ShowTVSwitch.IsToggled;
                            break;

                        case "ShowRadioChannels":
                            ShowRadioSwitch.IsToggled = !ShowRadioSwitch.IsToggled;
                            break;

                        case "ShowOtherChannels":
                            ShowOtherSwitch.IsToggled = !ShowOtherSwitch.IsToggled;
                            break;

                        case "ClearChannels":
                            _viewModel.ClearChannelsCommand.Execute(null);
                            break;

                        case "ShareChannels":
                            _viewModel.ShareChannelsCommand.Execute(null);
                            break;

                        case "ExportToFile":
                            _viewModel.ExportChannelsCommand.Execute(null);
                            break;

                        case "ImportChannels":
                            _viewModel.ImportChannelsCommand.Execute(null);
                            break;

                        case "ShowFullScreen":
                            FullscreenSwitch.IsToggled = !FullscreenSwitch.IsToggled;
                            break;

                        case "ShowPlayOnBackground":
                            PlayOnBackgroundSwitch.IsToggled = !PlayOnBackgroundSwitch.IsToggled;
                            break;

                        case "FontSize":
                            FontSizePicker.Focus();
                            break;

                        case "AutoStart":
                            ChannelAutoPlayedAfterStartPicker.Focus();
                            break;

                        case "EnableLogging":
                            EnableLoggingSwitch.IsToggled = !EnableLoggingSwitch.IsToggled;
                            break;

                        case "Donate1":
                            _viewModel.Donate1command.Execute(null);
                            break;

                        case "Donate2":
                            _viewModel.Donate2Command.Execute(null);
                            break;

                        case "Donate5":
                            _viewModel.Donate5command.Execute(null);
                            break;

                        case "Donate10":
                            _viewModel.Donate10command.Execute(null);
                            break;

                        case "RemoteAccessEnabled":
                            RemoteAccessSwitch.IsToggled = !RemoteAccessSwitch.IsToggled;
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
                    }

                    break;
            }
        }

        public void OnTextSent(string text)
        {
            switch (_focusItems.FocusedItemName)
            {
                case "RemoteAccessIP":
                    IPEntry.Text = text;
                    break;
                case "RemoteAccessPort":
                    PortEntry.Text = text;
                    break;
                case "RemoteAccessSecurityKey":
                    SecurityKeyEntry.Text = text;
                    break;
            }
        }

        private void OnRemoteTelevizorLabelTapped(object sender, EventArgs e)
        {
            Task.Run(async () => await Launcher.OpenAsync("https://play.google.com/store/apps/details?id=net.petrjanousek.RemoteTelevizor"));
        }
    }
}