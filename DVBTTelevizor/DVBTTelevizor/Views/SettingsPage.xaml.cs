using LoggerService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        private KeyboardFocusableItemList _focusItems;

        public SettingsPage(ILoggingService loggingService, IDialogService dialogService, DVBTTelevizorConfiguration config, ChannelService channelService)
        {
            InitializeComponent();

            _loggingService = loggingService;
            _dialogService = dialogService;

            BindingContext = _viewModel = new SettingsPageViewModel(_loggingService, _dialogService, config, channelService);

            PlayOnBackgroundSwitch.Toggled += PlayOnBackgroundSwitch_Toggled;

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_RequestBatterySettings, async (sender) =>
            {
                if (await _dialogService.Confirm("You should manually turn battery optimization off for DVBT Televizor. Open settings?"))
                {
                    MessagingCenter.Send<SettingsPage>(this, BaseViewModel.MSG_SetBatterySettings);
                }
            });

            BuildFocusableItems();
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

                .AddItem(KeyboardFocusableItem.CreateFrom("ShowServiceMenuBoxView", new List<View>() { ShowServiceMenuBoxView, ShowServiceMenuSwitch }))
                .AddItem(KeyboardFocusableItem.CreateFrom("ScanEPGBeforePlay", new List<View>() { ScanEPGBeforePlayBoxView, ScanEPGSwitch }))
                .AddItem(KeyboardFocusableItem.CreateFrom("ShowFullScreen", new List<View>() { ShowFullScreenBoxView, FullscreenSwitch }))
                .AddItem(KeyboardFocusableItem.CreateFrom("ShowPlayOnBackground", new List<View>() { ShowPlayOnBackgroundBoxView, PlayOnBackgroundSwitch }))

                .AddItem(KeyboardFocusableItem.CreateFrom("FontSize", new List<View>() { FontSizeBoxView, FontSizePicker }))

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

            // TODO: scroll only if necessary (  use SettingsScrollView.ScrollY, sccreen Height, ..... )
            SettingsScrollView.ScrollToAsync(0, args.FocusedItem.MaxYPosition - Height/2, false);
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

                        case "ShowServiceMenuBoxView":
                            ShowServiceMenuSwitch.IsToggled = !ShowServiceMenuSwitch.IsToggled;
                            break;

                        case "ScanEPGBeforePlay":
                            ScanEPGSwitch.IsToggled = !ScanEPGSwitch.IsToggled;
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
                    }

                    break;
            }
        }
    }
}