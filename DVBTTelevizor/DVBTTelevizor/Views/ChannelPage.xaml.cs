using Android.Widget;
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
    public partial class ChannelPage : ContentPage, IOnKeyDown
    {
        private ChannelPageViewModel _viewModel;
        protected ILoggingService _loggingService;
        protected IDialogService _dialogService;
        private KeyboardFocusableItemList _focusItems;

        public ChannelPage(ILoggingService loggingService, IDialogService dialogService, IDVBTDriverManager driver, DVBTTelevizorConfiguration config)
        {
            InitializeComponent();

            _loggingService = loggingService;
            _dialogService = dialogService;

            BindingContext = _viewModel = new ChannelPageViewModel(_loggingService, _dialogService, driver, config);

            BuildFocusableItems();

            Appearing += ChannelPage_Appearing;
        }

        private void BuildFocusableItems()
        {
            _focusItems = new KeyboardFocusableItemList();

            _focusItems
                .AddItem(KeyboardFocusableItem.CreateFrom("Number", new List<View>() { NumberBoxView, EntryName }))
                .AddItem(KeyboardFocusableItem.CreateFrom("Name", new List<View>() { NameBoxView, EntryNumber }));
                /*.AddItem(KeyboardFocusableItem.CreateFrom("Provider", new List<View>() { ProviderBoxView }))
                .AddItem(KeyboardFocusableItem.CreateFrom("Channel", new List<View>() { ChannelBoxView }))
                .AddItem(KeyboardFocusableItem.CreateFrom("Frequency", new List<View>() { FrequencyBoxView }))
                .AddItem(KeyboardFocusableItem.CreateFrom("DVBT", new List<View>() { DVBTTypeView }))
                .AddItem(KeyboardFocusableItem.CreateFrom("Bandwith", new List<View>() { BandwithBoxView }))
                .AddItem(KeyboardFocusableItem.CreateFrom("Type", new List<View>() { TypeBoxView }))
                .AddItem(KeyboardFocusableItem.CreateFrom("MapPID", new List<View>() { MapPIDBoxView }))
                .AddItem(KeyboardFocusableItem.CreateFrom("PIDs", new List<View>() { PIDsBoxView }));
                .AddItem(KeyboardFocusableItem.CreateFrom("Back", new List<View>() { BackButton }));*/

            _focusItems.OnItemFocusedEvent += ChannelPage_OnItemFocusedEvent;
        }

        private void ChannelPage_OnItemFocusedEvent(KeyboardFocusableItemEventArgs args)
        {
            // scroll to item
            ChannelPageScrollView.ScrollToAsync(0, args.FocusedItem.MaxYPosition - Height / 2, false);
        }

        private void ChannelPage_Appearing(object sender, EventArgs e)
        {
            _viewModel.NotifyFontSizeChange();
        }

        public DVBTChannel Channel
        {
            get
            {
                return _viewModel.Channel;
            }
            set
            {
                _viewModel.Channel = value;
                Title = _viewModel.Channel.Name;
            }
        }

        public async void OnKeyDown(string key, bool longPress)
        {
            _loggingService.Debug($"ChannelPage OnKeyDown {key}");

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
                            case "Number":
                                EntryNumber.Focus();
                                break;

                            case "Name":
                                EntryName.Focus();
                                break;

                            case "Back":
                                BackButton_Clicked(this, null);
                            break;
                    }
                    break;
            }
        }

        private void BackButton_Clicked(object sender, EventArgs e)
        {
            Navigation.PopAsync();
        }
    }
}